namespace Naxapa.Web.Entities;

public sealed class EstabelecimentoEntity
{
    public long EstabelecimentoId { get; set; }
    public string? Codigo { get; set; }
    public string NomeFantasia { get; set; } = string.Empty;
    public string? RazaoSocial { get; set; }
    public string? Documento { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? UF { get; set; }
    public string? CEP { get; set; }
    public string? LogoUrl { get; set; }
    public TimeSpan? HoraAbertura { get; set; }
    public TimeSpan? HoraFechamento { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}

public sealed class UsuarioEntity
{
    public long UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string NormalizedUserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public bool EmailConfirmado { get; set; }
    public string SenhaHash { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public bool TelefoneConfirmado { get; set; }
    public bool AutenticacaoDoisFatores { get; set; }
    public bool BloqueioHabilitado { get; set; }
    public string? SecurityStamp { get; set; }
    public string? ConcurrencyStamp { get; set; }
    public bool Ativo { get; set; }
    public DateTimeOffset? BloqueioFim { get; set; }
    public int TentativasAcessoFalho { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public DateTime? UltimoAcessoEm { get; set; }
}

public sealed class RefreshTokenEntity
{
    public long RefreshTokenId { get; set; }
    public long UsuarioId { get; set; }
    public long? EstabelecimentoId { get; set; }
    public byte[] TokenHash { get; set; } = [];
    public DateTime CriadoEm { get; set; }
    public DateTime ExpiraEm { get; set; }
    public DateTime? RevogadoEm { get; set; }
    public string? EnderecoIP { get; set; }
    public string? UserAgent { get; set; }
}

public sealed class PerfilEntity
{
    public long PerfilId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Escopo { get; set; } = "ESTABELECIMENTO";
    public string? Descricao { get; set; }
    public bool Ativo { get; set; }
}

public sealed class PermissaoEntity
{
    public long PermissaoId { get; set; }
    public string Chave { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public bool Ativo { get; set; }
}

public sealed class PerfilPermissaoEntity
{
    public long PerfilId { get; set; }
    public long PermissaoId { get; set; }
}

public sealed class UsuarioPerfilGlobalEntity
{
    public long UsuarioId { get; set; }
    public long PerfilId { get; set; }
}

public sealed class UsuarioEstabelecimentoEntity
{
    public long UsuarioEstabelecimentoId { get; set; }
    public long UsuarioId { get; set; }
    public long EstabelecimentoId { get; set; }
    public bool Principal { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}

public sealed class UsuarioEstabelecimentoPerfilEntity
{
    public long UsuarioEstabelecimentoId { get; set; }
    public long PerfilId { get; set; }
}

public sealed class CategoriaEntity
{
    public long CategoriaId { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? ImagemUrl { get; set; }
    public int OrdemExibicao { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}

public sealed class SetorProducaoEntity
{
    public long SetorProducaoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}

public sealed class ProdutoEntity
{
    public long ProdutoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public long CategoriaId { get; set; }
    public long? SetorProducaoId { get; set; }
    public string? CodigoInterno { get; set; }
    public string? CodigoBarras { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public decimal PrecoVenda { get; set; }
    public decimal? Custo { get; set; }
    public string UnidadeMedida { get; set; } = "UN";
    public bool DisponivelCardapio { get; set; }
    public bool ControlaEstoque { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}

public sealed class ProdutoImagemEntity
{
    public long ProdutoImagemId { get; set; }
    public long ProdutoId { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool Principal { get; set; }
    public int OrdemExibicao { get; set; }
}

public sealed class ClienteEntity
{
    public long ClienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public string? CodigoInterno { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Documento { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? UF { get; set; }
    public string? CEP { get; set; }
    public string? Observacao { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}

public sealed class GrupoOpcaoEntity
{
    public long GrupoOpcaoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string TipoSelecao { get; set; } = "U";
    public int QuantidadeMinima { get; set; }
    public int QuantidadeMaxima { get; set; }
    public bool Obrigatorio { get; set; }
    public bool Ativo { get; set; }
}

public sealed class OpcaoProdutoEntity
{
    public long OpcaoProdutoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public long GrupoOpcaoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal ValorAdicional { get; set; }
    public int OrdemExibicao { get; set; }
    public bool Ativo { get; set; }
}

public sealed class ProdutoGrupoOpcaoEntity
{
    public long EstabelecimentoId { get; set; }
    public long ProdutoId { get; set; }
    public long GrupoOpcaoId { get; set; }
    public int OrdemExibicao { get; set; }
}

public sealed class CardapioEntity
{
    public long CardapioId { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public bool Publicado { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}

public sealed class CardapioProdutoEntity
{
    public long EstabelecimentoId { get; set; }
    public long CardapioId { get; set; }
    public long ProdutoId { get; set; }
    public int OrdemExibicao { get; set; }
    public bool Disponivel { get; set; }
}

public sealed class ImpressoraEntity
{
    public long ImpressoraId { get; set; }
    public long EstabelecimentoId { get; set; }
    public long? SetorProducaoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Modelo { get; set; }
    public string TipoConexao { get; set; } = "WINDOWS";
    public string? EnderecoIP { get; set; }
    public int? Porta { get; set; }
    public string? NomeWindows { get; set; }
    public int? LarguraPapel { get; set; }
    public bool Padrao { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}


public sealed class ConfiguracaoImpressaoEntity
{
    public long ConfiguracaoImpressaoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public long ImpressoraId { get; set; }
    public long? SetorProducaoId { get; set; }
    public string TipoDocumento { get; set; } = "PRODUCAO";
    public int QuantidadeCopias { get; set; }
    public bool Ativo { get; set; }
}

public sealed class TrabalhoImpressaoEntity
{
    public long TrabalhoImpressaoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public long? PedidoId { get; set; }
    public long ImpressoraId { get; set; }
    public long? SetorProducaoId { get; set; }
    public string TipoDocumento { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Tentativas { get; set; }
    public Guid HashIdempotencia { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public string? UltimoErro { get; set; }
    public DateTime SolicitadoEm { get; set; }
    public DateTime? ProcessadoEm { get; set; }
}

public sealed class PedidoEntity
{
    public long PedidoId { get; set; }
    public long Numero { get; set; }
    public long EstabelecimentoId { get; set; }
    public long? ClienteId { get; set; }
    public long? MesaId { get; set; }
    public long? ComandaId { get; set; }
    public string TipoAtendimento { get; set; } = string.Empty;
    public string StatusOperacional { get; set; } = string.Empty;
    public string StatusFinanceiro { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Desconto { get; set; }
    public decimal Acrescimo { get; set; }
    public decimal Total { get; private set; }
    public string? Observacao { get; set; }
    public long CriadoPorUsuarioId { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public DateTime? FinalizadoEm { get; set; }
    public DateTime? CanceladoEm { get; set; }
}

public sealed class PedidoItemEntity
{
    public long PedidoItemId { get; set; }
    public long EstabelecimentoId { get; set; }
    public long PedidoId { get; set; }
    public long ProdutoId { get; set; }
    public string NomeProduto { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal Desconto { get; set; }
    public decimal Acrescimo { get; set; }
    public decimal ValorOpcoes { get; set; }
    public decimal TotalItem { get; private set; }
    public string? Observacao { get; set; }
}

public sealed class PedidoItemOpcaoEntity
{
    public long PedidoItemOpcaoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public long PedidoItemId { get; set; }
    public long OpcaoProdutoId { get; set; }
    public string NomeOpcao { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal ValorUnitarioAdicional { get; set; }
    public decimal TotalOpcao { get; private set; }
}

public sealed class HistoricoStatusPedidoEntity
{
    public long HistoricoStatusPedidoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public long PedidoId { get; set; }
    public string? StatusAnterior { get; set; }
    public string StatusNovo { get; set; } = string.Empty;
    public long UsuarioId { get; set; }
    public string? Observacao { get; set; }
    public DateTime CriadoEm { get; set; }
}

public sealed class FormaPagamentoEntity
{
    public long FormaPagamentoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public bool PermiteTroco { get; set; }
    public bool Ativo { get; set; }
}

public sealed class PagamentoEntity
{
    public long PagamentoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public long PedidoId { get; set; }
    public long FormaPagamentoId { get; set; }
    public long? CaixaSessaoId { get; set; }
    public decimal Valor { get; set; }
    public decimal? ValorRecebido { get; set; }
    public decimal? Troco { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ReferenciaTransacao { get; set; }
    public long CriadoPorUsuarioId { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}

public sealed class CaixaEntity
{
    public long CaixaId { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}

public sealed class CaixaSessaoEntity
{
    public long CaixaSessaoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public long CaixaId { get; set; }
    public long UsuarioAberturaId { get; set; }
    public long? UsuarioFechamentoId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal ValorAbertura { get; set; }
    public DateTime AbertoEm { get; set; }
    public DateTime? FechadoEm { get; set; }
    public decimal? ValorFechamentoCalculado { get; set; }
    public decimal? ValorFechamentoInformado { get; set; }
    public decimal? Diferenca { get; private set; }
    public string? ObservacaoAbertura { get; set; }
    public string? ObservacaoFechamento { get; set; }
}

public sealed class MovimentoCaixaEntity
{
    public long MovimentoCaixaId { get; set; }
    public long EstabelecimentoId { get; set; }
    public long CaixaSessaoId { get; set; }
    public long? PagamentoId { get; set; }
    public string TipoMovimento { get; set; } = string.Empty;
    public string Natureza { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string? Descricao { get; set; }
    public long UsuarioId { get; set; }
    public DateTime CriadoEm { get; set; }
}

public sealed class InsumoEntity
{
    public long InsumoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public string? CodigoInterno { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string UnidadeMedida { get; set; } = "UN";
    public decimal? CustoMedio { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}

public sealed class ReceitaEntity
{
    public long ReceitaId { get; set; }
    public long EstabelecimentoId { get; set; }
    public long ProdutoId { get; set; }
    public decimal Rendimento { get; set; }
    public string? Observacao { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}

public sealed class ReceitaItemEntity
{
    public long ReceitaItemId { get; set; }
    public long EstabelecimentoId { get; set; }
    public long ReceitaId { get; set; }
    public long InsumoId { get; set; }
    public decimal Quantidade { get; set; }
    public string? Observacao { get; set; }
}

public sealed class CaixaResumoEntity
{
    public long CaixaSessaoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public long CaixaId { get; set; }
    public string CodigoCaixa { get; set; } = string.Empty;
    public string Caixa { get; set; } = string.Empty;
    public long UsuarioAberturaId { get; set; }
    public string UsuarioAbertura { get; set; } = string.Empty;
    public long? UsuarioFechamentoId { get; set; }
    public string? UsuarioFechamento { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime AbertoEm { get; set; }
    public DateTime? FechadoEm { get; set; }
    public decimal ValorAbertura { get; set; }
    public decimal EntradasFisicas { get; set; }
    public decimal SaidasFisicas { get; set; }
    public decimal SaldoFisicoEsperado { get; set; }
    public decimal PagamentosDinheiro { get; set; }
    public decimal PagamentosCredito { get; set; }
    public decimal PagamentosDebito { get; set; }
    public decimal PagamentosPix { get; set; }
    public decimal PagamentosOutros { get; set; }
    public decimal TotalPagamentos { get; set; }
    public decimal Suprimentos { get; set; }
    public decimal Sangrias { get; set; }
    public decimal AjustesEntrada { get; set; }
    public decimal AjustesSaida { get; set; }
    public decimal EstornosDinheiro { get; set; }
    public decimal? ValorFechamentoCalculado { get; set; }
    public decimal? ValorFechamentoInformado { get; set; }
    public decimal? Diferenca { get; set; }
}


public sealed class MesaEntity
{
    public long MesaId { get; set; }
    public long EstabelecimentoId { get; set; }
    public int Numero { get; set; }
    public string? Nome { get; set; }
    public int? Capacidade { get; set; }
    public string Status { get; set; } = "LIVRE";
    public bool Ativo { get; set; }
}

public sealed class ComandaEntity
{
    public long ComandaId { get; set; }
    public long EstabelecimentoId { get; set; }
    public long? ClienteId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Status { get; set; } = "LIVRE";
    public DateTime? AbertaEm { get; set; }
    public DateTime? FechadaEm { get; set; }
    public string? Observacao { get; set; }
}
