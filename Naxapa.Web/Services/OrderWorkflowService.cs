using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;

namespace Naxapa.Web.Services;

public sealed class OrderWorkflowService(NaxapaDbContext db, ILogger<OrderWorkflowService> logger)
{
    public async Task<int> ChangeStatusAsync(long establishmentId, long orderId, string newStatus, long userId, string? notes, CancellationToken ct = default)
    {
        newStatus = newStatus.Trim().ToUpperInvariant();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($@"
EXEC op.sp_Pedido_AlterarStatus
    @EstabelecimentoId={establishmentId},
    @PedidoId={orderId},
    @NovoStatus={newStatus},
    @UsuarioId={userId},
    @Observacao={notes};", ct);

        var enqueued = 0;
        if (newStatus == "PENDENTE")
            enqueued += await EnqueueProductionAsync(establishmentId, orderId, ct);
        else if (newStatus == "FINALIZADO")
        {
            await MoveControlledStockAsync(establishmentId, orderId, userId, ct);
            enqueued += await EnqueueReceiptAsync(establishmentId, orderId, ct);
        }

        await tx.CommitAsync(ct);
        return enqueued;
    }

    public async Task<int> EnqueueProductionAsync(long establishmentId, long orderId, CancellationToken ct = default)
    {
        var order = await db.Pedidos.AsNoTracking()
            .Where(x => x.PedidoId == orderId && x.EstabelecimentoId == establishmentId)
            .Select(x => new { x.PedidoId, x.Numero, x.TipoAtendimento, x.Observacao, x.CriadoEm })
            .FirstOrDefaultAsync(ct) ?? throw new InvalidOperationException("Pedido não encontrado.");

        var items = await (
            from i in db.PedidoItens.AsNoTracking()
            join p in db.Produtos.AsNoTracking() on i.ProdutoId equals p.ProdutoId
            where i.PedidoId == orderId && i.EstabelecimentoId == establishmentId
            select new { i.PedidoItemId, i.NomeProduto, i.Quantidade, i.Observacao, p.SetorProducaoId })
            .ToListAsync(ct);

        if (items.Count == 0) return 0;
        var itemIds = items.Select(x => x.PedidoItemId).ToArray();
        var options = await db.PedidoItemOpcoes.AsNoTracking()
            .Where(x => x.EstabelecimentoId == establishmentId && itemIds.Contains(x.PedidoItemId))
            .Select(x => new { x.PedidoItemId, x.NomeOpcao, x.Quantidade })
            .ToListAsync(ct);

        var configs = await (
            from c in db.ConfiguracoesImpressao.AsNoTracking()
            join p in db.Impressoras.AsNoTracking() on c.ImpressoraId equals p.ImpressoraId
            where c.EstabelecimentoId == establishmentId && c.Ativo && p.Ativo && c.TipoDocumento == "PRODUCAO"
            select new { c.ImpressoraId, c.SetorProducaoId, c.QuantidadeCopias, PrinterName = p.Nome })
            .ToListAsync(ct);

        var enqueued = 0;
        foreach (var group in items.Where(x => x.SetorProducaoId != null).GroupBy(x => x.SetorProducaoId!.Value))
        {
            var destinations = configs.Where(x => x.SetorProducaoId == group.Key).ToList();
            if (destinations.Count == 0)
            {
                logger.LogWarning("Pedido {OrderId}: setor {SectorId} não possui configuração de impressão ativa.", orderId, group.Key);
                continue;
            }

            foreach (var destination in destinations)
            {
                var payload = JsonSerializer.Serialize(new
                {
                    tipo = "PRODUCAO",
                    pedidoId = order.PedidoId,
                    numero = order.Numero,
                    atendimento = order.TipoAtendimento,
                    observacao = order.Observacao,
                    criadoEmUtc = order.CriadoEm,
                    setorId = group.Key,
                    copias = destination.QuantidadeCopias,
                    itens = group.Select(i => new
                    {
                        i.PedidoItemId,
                        produto = i.NomeProduto,
                        quantidade = i.Quantidade,
                        observacao = i.Observacao,
                        opcoes = options.Where(o => o.PedidoItemId == i.PedidoItemId)
                            .Select(o => new { nome = o.NomeOpcao, quantidade = o.Quantidade })
                    })
                });
                var idempotency = DeterministicGuid($"production:{establishmentId}:{orderId}:{group.Key}:{destination.ImpressoraId}");
                const string documentType = "PRODUCAO";
                await db.Database.ExecuteSqlInterpolatedAsync($@"
EXEC imp.sp_TrabalhoImpressao_Enfileirar
    @EstabelecimentoId={establishmentId},
    @PedidoId={orderId},
    @ImpressoraId={destination.ImpressoraId},
    @SetorProducaoId={group.Key},
    @TipoDocumento={documentType},
    @PayloadJson={payload},
    @HashIdempotencia={idempotency};", ct);
                enqueued++;
            }
        }
        return enqueued;
    }



    private async Task MoveControlledStockAsync(long establishmentId, long orderId, long userId, CancellationToken ct)
    {
        var lines = await (
            from i in db.PedidoItens.AsNoTracking()
            join p in db.Produtos.AsNoTracking() on i.ProdutoId equals p.ProdutoId
            where i.PedidoId == orderId && i.EstabelecimentoId == establishmentId && p.ControlaEstoque
            select new { i.PedidoItemId, i.ProdutoId, i.Quantidade, i.NomeProduto })
            .ToListAsync(ct);

        foreach (var line in lines)
        {
            const string type = "SAIDA";
            const string origin = "PEDIDO";
            var notes = $"Baixa automática do pedido {orderId} - {line.NomeProduto}";
            await db.Database.ExecuteSqlInterpolatedAsync($@"
EXEC est.sp_Estoque_Movimentar
    @EstabelecimentoId={establishmentId},
    @ProdutoId={line.ProdutoId},
    @TipoMovimento={type},
    @Quantidade={line.Quantidade},
    @UsuarioId={userId},
    @Origem={origin},
    @Observacao={notes},
    @PedidoItemId={line.PedidoItemId};", ct);
        }
    }

    public async Task<int> EnqueueReceiptAsync(long establishmentId, long orderId, CancellationToken ct = default)
    {
        var order = await db.Pedidos.AsNoTracking()
            .Where(x => x.PedidoId == orderId && x.EstabelecimentoId == establishmentId)
            .Select(x => new
            {
                x.PedidoId, x.Numero, x.TipoAtendimento, x.Subtotal, x.Desconto, x.Acrescimo, x.Total,
                x.CriadoEm, x.FinalizadoEm, x.CriadoPorUsuarioId
            })
            .FirstOrDefaultAsync(ct) ?? throw new InvalidOperationException("Pedido não encontrado.");

        var establishment = await db.Estabelecimentos.AsNoTracking()
            .Where(x => x.EstabelecimentoId == establishmentId)
            .Select(x => new { x.NomeFantasia, x.Documento, x.Telefone, x.Cidade, x.UF })
            .FirstAsync(ct);

        var operatorName = await db.Usuarios.AsNoTracking()
            .Where(x => x.UsuarioId == order.CriadoPorUsuarioId)
            .Select(x => x.Nome)
            .FirstOrDefaultAsync(ct) ?? $"Usuário {order.CriadoPorUsuarioId}";

        var items = await db.PedidoItens.AsNoTracking()
            .Where(x => x.PedidoId == orderId && x.EstabelecimentoId == establishmentId)
            .Select(x => new { x.PedidoItemId, x.NomeProduto, x.Quantidade, x.ValorUnitario, x.ValorOpcoes, x.TotalItem, x.Observacao })
            .ToListAsync(ct);
        var itemIds = items.Select(x => x.PedidoItemId).ToArray();
        var options = await db.PedidoItemOpcoes.AsNoTracking()
            .Where(x => x.EstabelecimentoId == establishmentId && itemIds.Contains(x.PedidoItemId))
            .Select(x => new { x.PedidoItemId, x.NomeOpcao, x.Quantidade, x.ValorUnitarioAdicional, x.TotalOpcao })
            .ToListAsync(ct);

        var payments = await (
            from p in db.Pagamentos.AsNoTracking()
            join f in db.FormasPagamento.AsNoTracking() on p.FormaPagamentoId equals f.FormaPagamentoId
            where p.PedidoId == orderId && p.EstabelecimentoId == establishmentId && p.Status == "CONFIRMADO"
            select new { forma = f.Nome, tipo = f.Tipo, p.Valor, p.ValorRecebido, p.Troco })
            .ToListAsync(ct);

        var configuredPrinterIds = await db.ConfiguracoesImpressao.AsNoTracking()
            .Where(x => x.EstabelecimentoId == establishmentId && x.Ativo && x.TipoDocumento == "COMPROVANTE")
            .OrderBy(x => x.ConfiguracaoImpressaoId)
            .Select(x => x.ImpressoraId)
            .ToListAsync(ct);

        var printers = await db.Impressoras.AsNoTracking()
            .Where(x => x.EstabelecimentoId == establishmentId && x.Ativo && (configuredPrinterIds.Contains(x.ImpressoraId) || x.Padrao))
            .OrderByDescending(x => configuredPrinterIds.Contains(x.ImpressoraId))
            .ThenByDescending(x => x.Padrao)
            .Take(1)
            .Select(x => new { x.ImpressoraId, x.Nome })
            .ToListAsync(ct);

        if (printers.Count == 0)
        {
            logger.LogWarning("Pedido {OrderId}: não existe impressora ativa/padrão para comprovante.", orderId);
            return 0;
        }

        var printer = printers[0];
        var payload = JsonSerializer.Serialize(new
        {
            tipo = "COMPROVANTE",
            estabelecimento = new
            {
                nome = establishment.NomeFantasia,
                documento = establishment.Documento,
                telefone = establishment.Telefone,
                cidade = establishment.Cidade,
                uf = establishment.UF
            },
            pedidoId = order.PedidoId,
            numero = order.Numero,
            atendimento = order.TipoAtendimento,
            operador = operatorName,
            criadoEmUtc = order.CriadoEm,
            finalizadoEmUtc = order.FinalizadoEm,
            itens = items.Select(i => new
            {
                produto = i.NomeProduto,
                quantidade = i.Quantidade,
                valorUnitario = i.ValorUnitario,
                valorOpcoes = i.ValorOpcoes,
                total = i.TotalItem,
                observacao = i.Observacao,
                opcoes = options.Where(o => o.PedidoItemId == i.PedidoItemId).Select(o => new
                {
                    nome = o.NomeOpcao, quantidade = o.Quantidade, valorUnitario = o.ValorUnitarioAdicional, total = o.TotalOpcao
                })
            }),
            subtotal = order.Subtotal,
            desconto = order.Desconto,
            acrescimo = order.Acrescimo,
            total = order.Total,
            pagamentos = payments,
            aviso = "DOCUMENTO NÃO FISCAL"
        });

        var idempotency = DeterministicGuid($"receipt:{establishmentId}:{orderId}:{printer.ImpressoraId}");
        const string documentType = "COMPROVANTE";
        long? productionSectorId = null;
        await db.Database.ExecuteSqlInterpolatedAsync($@"
EXEC imp.sp_TrabalhoImpressao_Enfileirar
    @EstabelecimentoId={establishmentId},
    @PedidoId={orderId},
    @ImpressoraId={printer.ImpressoraId},
    @SetorProducaoId={productionSectorId},
    @TipoDocumento={documentType},
    @PayloadJson={payload},
    @HashIdempotencia={idempotency};", ct);
        return 1;
    }

    private static Guid DeterministicGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        Span<byte> bytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(bytes);
        return new Guid(bytes);
    }
}
