using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Entities;

namespace Naxapa.Web.Data;

public sealed class NaxapaDbContext(DbContextOptions<NaxapaDbContext> options) : DbContext(options)
{
    public DbSet<EstabelecimentoEntity> Estabelecimentos => Set<EstabelecimentoEntity>();
    public DbSet<UsuarioEntity> Usuarios => Set<UsuarioEntity>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();
    public DbSet<PerfilEntity> Perfis => Set<PerfilEntity>();
    public DbSet<PermissaoEntity> Permissoes => Set<PermissaoEntity>();
    public DbSet<PerfilPermissaoEntity> PerfilPermissoes => Set<PerfilPermissaoEntity>();
    public DbSet<UsuarioPerfilGlobalEntity> UsuarioPerfisGlobais => Set<UsuarioPerfilGlobalEntity>();
    public DbSet<UsuarioEstabelecimentoEntity> UsuarioEstabelecimentos => Set<UsuarioEstabelecimentoEntity>();
    public DbSet<UsuarioEstabelecimentoPerfilEntity> UsuarioEstabelecimentoPerfis => Set<UsuarioEstabelecimentoPerfilEntity>();

    public DbSet<ClienteEntity> Clientes => Set<ClienteEntity>();
    public DbSet<CategoriaEntity> Categorias => Set<CategoriaEntity>();
    public DbSet<SetorProducaoEntity> SetoresProducao => Set<SetorProducaoEntity>();
    public DbSet<ProdutoEntity> Produtos => Set<ProdutoEntity>();
    public DbSet<ProdutoImagemEntity> ProdutoImagens => Set<ProdutoImagemEntity>();
    public DbSet<GrupoOpcaoEntity> GruposOpcao => Set<GrupoOpcaoEntity>();
    public DbSet<OpcaoProdutoEntity> OpcoesProduto => Set<OpcaoProdutoEntity>();
    public DbSet<ProdutoGrupoOpcaoEntity> ProdutoGruposOpcao => Set<ProdutoGrupoOpcaoEntity>();
    public DbSet<CardapioEntity> Cardapios => Set<CardapioEntity>();
    public DbSet<CardapioProdutoEntity> CardapioProdutos => Set<CardapioProdutoEntity>();
    public DbSet<InsumoEntity> Insumos => Set<InsumoEntity>();
    public DbSet<ReceitaEntity> Receitas => Set<ReceitaEntity>();
    public DbSet<ReceitaItemEntity> ReceitaItens => Set<ReceitaItemEntity>();

    public DbSet<ImpressoraEntity> Impressoras => Set<ImpressoraEntity>();
    public DbSet<ConfiguracaoImpressaoEntity> ConfiguracoesImpressao => Set<ConfiguracaoImpressaoEntity>();
    public DbSet<TrabalhoImpressaoEntity> TrabalhosImpressao => Set<TrabalhoImpressaoEntity>();

    public DbSet<MesaEntity> Mesas => Set<MesaEntity>();
    public DbSet<ComandaEntity> Comandas => Set<ComandaEntity>();
    public DbSet<PedidoEntity> Pedidos => Set<PedidoEntity>();
    public DbSet<PedidoItemEntity> PedidoItens => Set<PedidoItemEntity>();
    public DbSet<PedidoItemOpcaoEntity> PedidoItemOpcoes => Set<PedidoItemOpcaoEntity>();
    public DbSet<HistoricoStatusPedidoEntity> HistoricosStatusPedido => Set<HistoricoStatusPedidoEntity>();

    public DbSet<FormaPagamentoEntity> FormasPagamento => Set<FormaPagamentoEntity>();
    public DbSet<PagamentoEntity> Pagamentos => Set<PagamentoEntity>();
    public DbSet<CaixaEntity> Caixas => Set<CaixaEntity>();
    public DbSet<CaixaSessaoEntity> CaixaSessoes => Set<CaixaSessaoEntity>();
    public DbSet<MovimentoCaixaEntity> MovimentosCaixa => Set<MovimentoCaixaEntity>();
    public DbSet<CaixaResumoEntity> CaixaResumo => Set<CaixaResumoEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureSecurity(modelBuilder);
        ConfigureCadastros(modelBuilder);
        ConfigureOperacao(modelBuilder);
        ConfigureFinanceiro(modelBuilder);
    }

    private static void ConfigureSecurity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UsuarioEntity>(e =>
        {
            e.ToTable("Usuario", "seg");
            e.HasKey(x => x.UsuarioId);
            e.Property(x => x.Nome).HasMaxLength(150);
            e.Property(x => x.UserName).HasMaxLength(100);
            e.Property(x => x.NormalizedUserName).HasMaxLength(100);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.NormalizedEmail).HasMaxLength(256);
            e.Property(x => x.SenhaHash).HasMaxLength(500);
            e.Property(x => x.Telefone).HasMaxLength(20);
            e.Property(x => x.SecurityStamp).HasMaxLength(100);
            e.Property(x => x.ConcurrencyStamp).HasMaxLength(100);
            e.Property(x => x.Ativo).HasDefaultValue(true);
            e.Property(x => x.BloqueioHabilitado).HasDefaultValue(true);
            e.Property(x => x.TentativasAcessoFalho).HasDefaultValue(0);
            e.Property(x => x.CriadoEm).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<RefreshTokenEntity>(e =>
        {
            e.ToTable("RefreshToken", "seg");
            e.HasKey(x => x.RefreshTokenId);
            e.Property(x => x.TokenHash).HasMaxLength(64);
            e.Property(x => x.CriadoEm).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.EnderecoIP).HasMaxLength(45);
            e.Property(x => x.UserAgent).HasMaxLength(300);
        });

        modelBuilder.Entity<PerfilEntity>(e =>
        {
            e.ToTable("Perfil", "seg");
            e.HasKey(x => x.PerfilId);
            e.Property(x => x.Nome).HasMaxLength(100);
            e.Property(x => x.Escopo).HasMaxLength(20);
            e.Property(x => x.Descricao).HasMaxLength(300);
        });

        modelBuilder.Entity<PermissaoEntity>(e =>
        {
            e.ToTable("Permissao", "seg");
            e.HasKey(x => x.PermissaoId);
            e.Property(x => x.Chave).HasMaxLength(150);
            e.Property(x => x.Modulo).HasMaxLength(100);
            e.Property(x => x.Descricao).HasMaxLength(300);
        });

        modelBuilder.Entity<PerfilPermissaoEntity>(e =>
        {
            e.ToTable("PerfilPermissao", "seg");
            e.HasKey(x => new { x.PerfilId, x.PermissaoId });
        });

        modelBuilder.Entity<UsuarioPerfilGlobalEntity>(e =>
        {
            e.ToTable("UsuarioPerfilGlobal", "seg");
            e.HasKey(x => new { x.UsuarioId, x.PerfilId });
        });

        modelBuilder.Entity<UsuarioEstabelecimentoEntity>(e =>
        {
            e.ToTable("UsuarioEstabelecimento", "seg");
            e.HasKey(x => x.UsuarioEstabelecimentoId);
            e.Property(x => x.Ativo).HasDefaultValue(true);
            e.Property(x => x.CriadoEm).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<UsuarioEstabelecimentoPerfilEntity>(e =>
        {
            e.ToTable("UsuarioEstabelecimentoPerfil", "seg");
            e.HasKey(x => new { x.UsuarioEstabelecimentoId, x.PerfilId });
        });
    }

    private static void ConfigureCadastros(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EstabelecimentoEntity>(e =>
        {
            e.ToTable("Estabelecimento", "cad");
            e.HasKey(x => x.EstabelecimentoId);
            e.Property(x => x.Codigo).HasMaxLength(30);
            e.Property(x => x.NomeFantasia).HasMaxLength(150);
            e.Property(x => x.RazaoSocial).HasMaxLength(150);
            e.Property(x => x.Documento).HasMaxLength(14);
            e.Property(x => x.InscricaoEstadual).HasMaxLength(20);
            e.Property(x => x.Telefone).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Logradouro).HasMaxLength(150);
            e.Property(x => x.Numero).HasMaxLength(20);
            e.Property(x => x.Complemento).HasMaxLength(100);
            e.Property(x => x.Bairro).HasMaxLength(100);
            e.Property(x => x.Cidade).HasMaxLength(100);
            e.Property(x => x.UF).HasMaxLength(2).IsFixedLength();
            e.Property(x => x.CEP).HasMaxLength(8);
            e.Property(x => x.LogoUrl).HasMaxLength(500);
            e.Property(x => x.Ativo).HasDefaultValue(true);
            e.Property(x => x.CriadoEm).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<ClienteEntity>(e =>
        {
            e.ToTable("Cliente", "cad");
            e.HasKey(x => x.ClienteId);
            e.Property(x => x.CodigoInterno).HasMaxLength(30);
            e.Property(x => x.Nome).HasMaxLength(150);
            e.Property(x => x.Documento).HasMaxLength(14);
            e.Property(x => x.Telefone).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Logradouro).HasMaxLength(150);
            e.Property(x => x.Numero).HasMaxLength(20);
            e.Property(x => x.Complemento).HasMaxLength(100);
            e.Property(x => x.Bairro).HasMaxLength(100);
            e.Property(x => x.Cidade).HasMaxLength(100);
            e.Property(x => x.UF).HasMaxLength(2).IsFixedLength();
            e.Property(x => x.CEP).HasMaxLength(8);
            e.Property(x => x.Observacao).HasMaxLength(500);
            e.Property(x => x.CriadoEm).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.Ativo).HasDefaultValue(true);
        });

        modelBuilder.Entity<CategoriaEntity>(e =>
        {
            e.ToTable("Categoria", "cad");
            e.HasKey(x => x.CategoriaId);
            e.Property(x => x.Nome).HasMaxLength(100);
            e.Property(x => x.Descricao).HasMaxLength(300);
            e.Property(x => x.ImagemUrl).HasMaxLength(500);
            e.Property(x => x.OrdemExibicao).HasDefaultValue(0);
            e.Property(x => x.Ativo).HasDefaultValue(true);
            e.Property(x => x.CriadoEm).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<SetorProducaoEntity>(e =>
        {
            e.ToTable("SetorProducao", "cad");
            e.HasKey(x => x.SetorProducaoId);
            e.Property(x => x.Nome).HasMaxLength(100);
            e.Property(x => x.Descricao).HasMaxLength(300);
            e.Property(x => x.Ativo).HasDefaultValue(true);
            e.Property(x => x.CriadoEm).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<ProdutoEntity>(e =>
        {
            e.ToTable("Produto", "cad", tb => tb.UseSqlOutputClause(false));
            e.HasKey(x => x.ProdutoId);
            e.Property(x => x.Nome).HasMaxLength(150);
            e.Property(x => x.Descricao).HasMaxLength(500);
            e.Property(x => x.CodigoInterno).HasMaxLength(30);
            e.Property(x => x.CodigoBarras).HasMaxLength(30);
            e.Property(x => x.PrecoVenda).HasPrecision(18, 2);
            e.Property(x => x.Custo).HasPrecision(18, 2);
            e.Property(x => x.UnidadeMedida).HasMaxLength(10);
            e.Property(x => x.CriadoEm).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.Ativo).HasDefaultValue(true);
            e.Property(x => x.DisponivelCardapio).HasDefaultValue(true);
            e.Property(x => x.ControlaEstoque).HasDefaultValue(false);
        });

        modelBuilder.Entity<ProdutoImagemEntity>(e =>
        {
            e.ToTable("ProdutoImagem", "cad");
            e.HasKey(x => x.ProdutoImagemId);
            e.Property(x => x.Url).HasMaxLength(500);
        });

        modelBuilder.Entity<GrupoOpcaoEntity>(e =>
        {
            e.ToTable("GrupoOpcao", "cad");
            e.HasKey(x => x.GrupoOpcaoId);
            e.Property(x => x.Nome).HasMaxLength(120);
            e.Property(x => x.Descricao).HasMaxLength(300);
            e.Property(x => x.TipoSelecao).HasMaxLength(1).IsFixedLength();
        });

        modelBuilder.Entity<OpcaoProdutoEntity>(e =>
        {
            e.ToTable("OpcaoProduto", "cad");
            e.HasKey(x => x.OpcaoProdutoId);
            e.Property(x => x.Nome).HasMaxLength(120);
            e.Property(x => x.ValorAdicional).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ProdutoGrupoOpcaoEntity>(e =>
        {
            e.ToTable("ProdutoGrupoOpcao", "cad");
            e.HasKey(x => new { x.ProdutoId, x.GrupoOpcaoId });
        });

        modelBuilder.Entity<CardapioEntity>(e =>
        {
            e.ToTable("Cardapio", "cad");
            e.HasKey(x => x.CardapioId);
            e.Property(x => x.Nome).HasMaxLength(120);
            e.Property(x => x.Descricao).HasMaxLength(300);
            e.Property(x => x.CriadoEm).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<CardapioProdutoEntity>(e =>
        {
            e.ToTable("CardapioProduto", "cad");
            e.HasKey(x => new { x.CardapioId, x.ProdutoId });
        });

        modelBuilder.Entity<InsumoEntity>(e =>
        {
            e.ToTable("Insumo", "cad");
            e.HasKey(x => x.InsumoId);
            e.Property(x => x.CodigoInterno).HasMaxLength(30);
            e.Property(x => x.Nome).HasMaxLength(150);
            e.Property(x => x.UnidadeMedida).HasMaxLength(10);
            e.Property(x => x.CustoMedio).HasPrecision(18, 4);
            e.Property(x => x.CriadoEm).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<ReceitaEntity>(e =>
        {
            e.ToTable("Receita", "cad");
            e.HasKey(x => x.ReceitaId);
            e.Property(x => x.Rendimento).HasPrecision(18, 3);
            e.Property(x => x.Observacao).HasMaxLength(500);
            e.Property(x => x.CriadoEm).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<ReceitaItemEntity>(e =>
        {
            e.ToTable("ReceitaItem", "cad");
            e.HasKey(x => x.ReceitaItemId);
            e.Property(x => x.Quantidade).HasPrecision(18, 4);
            e.Property(x => x.Observacao).HasMaxLength(300);
        });

        modelBuilder.Entity<ImpressoraEntity>(e =>
        {
            e.ToTable("Impressora", "cfg");
            e.HasKey(x => x.ImpressoraId);
            e.Property(x => x.Nome).HasMaxLength(120);
            e.Property(x => x.Modelo).HasMaxLength(120);
            e.Property(x => x.TipoConexao).HasMaxLength(20);
            e.Property(x => x.EnderecoIP).HasMaxLength(45);
            e.Property(x => x.NomeWindows).HasMaxLength(200);
            e.Property(x => x.CriadoEm).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<ConfiguracaoImpressaoEntity>(e =>
        {
            e.ToTable("ConfiguracaoImpressao", "cfg");
            e.HasKey(x => x.ConfiguracaoImpressaoId);
            e.Property(x => x.TipoDocumento).HasMaxLength(30);
        });

        modelBuilder.Entity<TrabalhoImpressaoEntity>(e =>
        {
            e.ToTable("TrabalhoImpressao", "imp");
            e.HasKey(x => x.TrabalhoImpressaoId);
            e.Property(x => x.TipoDocumento).HasMaxLength(30);
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.UltimoErro).HasMaxLength(1000);
            e.Property(x => x.SolicitadoEm).HasDefaultValueSql("SYSUTCDATETIME()");
        });
    }

    private static void ConfigureOperacao(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MesaEntity>(e =>
        {
            e.ToTable("Mesa", "op");
            e.HasKey(x => x.MesaId);
            e.Property(x => x.Nome).HasMaxLength(100);
            e.Property(x => x.Status).HasMaxLength(15);
        });

        modelBuilder.Entity<ComandaEntity>(e =>
        {
            e.ToTable("Comanda", "op");
            e.HasKey(x => x.ComandaId);
            e.Property(x => x.Codigo).HasMaxLength(50);
            e.Property(x => x.Status).HasMaxLength(15);
            e.Property(x => x.Observacao).HasMaxLength(300);
        });

        modelBuilder.Entity<PedidoEntity>(e =>
        {
            e.ToTable("Pedido", "op");
            e.HasKey(x => x.PedidoId);
            e.Property(x => x.Numero).HasDefaultValueSql("NEXT VALUE FOR op.SeqNumeroPedido").ValueGeneratedOnAdd();
            e.Property(x => x.TipoAtendimento).HasMaxLength(20);
            e.Property(x => x.StatusOperacional).HasMaxLength(20);
            e.Property(x => x.StatusFinanceiro).HasMaxLength(20);
            e.Property(x => x.Subtotal).HasPrecision(18, 2);
            e.Property(x => x.Desconto).HasPrecision(18, 2);
            e.Property(x => x.Acrescimo).HasPrecision(18, 2);
            e.Property(x => x.Total)
                .HasPrecision(18, 2)
                .HasComputedColumnSql("CONVERT(decimal(18,2), [Subtotal]-[Desconto]+[Acrescimo])", stored: true);
            e.Property(x => x.Observacao).HasMaxLength(500);
        });

        modelBuilder.Entity<PedidoItemEntity>(e =>
        {
            e.ToTable("PedidoItem", "op");
            e.HasKey(x => x.PedidoItemId);
            e.Property(x => x.NomeProduto).HasMaxLength(150);
            e.Property(x => x.Quantidade).HasPrecision(18, 3);
            e.Property(x => x.ValorUnitario).HasPrecision(18, 2);
            e.Property(x => x.Desconto).HasPrecision(18, 2);
            e.Property(x => x.Acrescimo).HasPrecision(18, 2);
            e.Property(x => x.ValorOpcoes).HasPrecision(18, 2);
            e.Property(x => x.TotalItem)
                .HasPrecision(18, 2)
                .HasComputedColumnSql("CONVERT(decimal(18,2), ([Quantidade]*[ValorUnitario])-[Desconto]+[Acrescimo]+[ValorOpcoes])", stored: true);
            e.Property(x => x.Observacao).HasMaxLength(500);
        });

        modelBuilder.Entity<PedidoItemOpcaoEntity>(e =>
        {
            e.ToTable("PedidoItemOpcao", "op");
            e.HasKey(x => x.PedidoItemOpcaoId);
            e.Property(x => x.NomeOpcao).HasMaxLength(120);
            e.Property(x => x.ValorUnitarioAdicional).HasPrecision(18, 2);
            e.Property(x => x.TotalOpcao).HasPrecision(18, 2).HasComputedColumnSql("CONVERT(decimal(18,2), [Quantidade]*[ValorUnitarioAdicional])", stored: true);
        });

        modelBuilder.Entity<HistoricoStatusPedidoEntity>(e =>
        {
            e.ToTable("HistoricoStatusPedido", "op");
            e.HasKey(x => x.HistoricoStatusPedidoId);
            e.Property(x => x.StatusAnterior).HasMaxLength(20);
            e.Property(x => x.StatusNovo).HasMaxLength(20);
            e.Property(x => x.Observacao).HasMaxLength(500);
            e.Property(x => x.CriadoEm).HasDefaultValueSql("SYSUTCDATETIME()");
        });
    }

    private static void ConfigureFinanceiro(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FormaPagamentoEntity>(e =>
        {
            e.ToTable("FormaPagamento", "fin");
            e.HasKey(x => x.FormaPagamentoId);
            e.Property(x => x.Codigo).HasMaxLength(30);
            e.Property(x => x.Nome).HasMaxLength(100);
            e.Property(x => x.Tipo).HasMaxLength(20);
        });

        modelBuilder.Entity<PagamentoEntity>(e =>
        {
            e.ToTable("Pagamento", "fin");
            e.HasKey(x => x.PagamentoId);
            e.Property(x => x.Valor).HasPrecision(18, 2);
            e.Property(x => x.ValorRecebido).HasPrecision(18, 2);
            e.Property(x => x.Troco).HasPrecision(18, 2);
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.ReferenciaTransacao).HasMaxLength(150);
            e.Property(x => x.CriadoEm).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<CaixaEntity>(e =>
        {
            e.ToTable("Caixa", "fin");
            e.HasKey(x => x.CaixaId);
            e.Property(x => x.Codigo).HasMaxLength(30);
            e.Property(x => x.Nome).HasMaxLength(100);
            e.Property(x => x.Descricao).HasMaxLength(300);
            e.Property(x => x.CriadoEm).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<CaixaSessaoEntity>(e =>
        {
            e.ToTable("CaixaSessao", "fin");
            e.HasKey(x => x.CaixaSessaoId);
            e.Property(x => x.Status).HasMaxLength(10);
            e.Property(x => x.ValorAbertura).HasPrecision(18, 2);
            e.Property(x => x.ValorFechamentoCalculado).HasPrecision(18, 2);
            e.Property(x => x.ValorFechamentoInformado).HasPrecision(18, 2);
            e.Property(x => x.Diferenca)
                .HasPrecision(18, 2)
                .HasComputedColumnSql("CASE WHEN [ValorFechamentoCalculado] IS NULL OR [ValorFechamentoInformado] IS NULL THEN NULL ELSE CONVERT(decimal(18,2), [ValorFechamentoInformado]-[ValorFechamentoCalculado]) END", stored: true);
            e.Property(x => x.ObservacaoAbertura).HasMaxLength(500);
            e.Property(x => x.ObservacaoFechamento).HasMaxLength(500);
        });

        modelBuilder.Entity<MovimentoCaixaEntity>(e =>
        {
            e.ToTable("MovimentoCaixa", "fin");
            e.HasKey(x => x.MovimentoCaixaId);
            e.Property(x => x.TipoMovimento).HasMaxLength(30);
            e.Property(x => x.Natureza).HasMaxLength(1).IsFixedLength();
            e.Property(x => x.Valor).HasPrecision(18, 2);
            e.Property(x => x.Descricao).HasMaxLength(500);
        });

        modelBuilder.Entity<CaixaResumoEntity>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_CaixaResumo", "rel");
            e.Property(x => x.ValorAbertura).HasPrecision(18, 2);
            e.Property(x => x.EntradasFisicas).HasPrecision(18, 2);
            e.Property(x => x.SaidasFisicas).HasPrecision(18, 2);
            e.Property(x => x.SaldoFisicoEsperado).HasPrecision(18, 2);
            e.Property(x => x.PagamentosDinheiro).HasPrecision(18, 2);
            e.Property(x => x.PagamentosCredito).HasPrecision(18, 2);
            e.Property(x => x.PagamentosDebito).HasPrecision(18, 2);
            e.Property(x => x.PagamentosPix).HasPrecision(18, 2);
            e.Property(x => x.PagamentosOutros).HasPrecision(18, 2);
            e.Property(x => x.TotalPagamentos).HasPrecision(18, 2);
            e.Property(x => x.Suprimentos).HasPrecision(18, 2);
            e.Property(x => x.Sangrias).HasPrecision(18, 2);
            e.Property(x => x.AjustesEntrada).HasPrecision(18, 2);
            e.Property(x => x.AjustesSaida).HasPrecision(18, 2);
            e.Property(x => x.EstornosDinheiro).HasPrecision(18, 2);
            e.Property(x => x.ValorFechamentoCalculado).HasPrecision(18, 2);
            e.Property(x => x.ValorFechamentoInformado).HasPrecision(18, 2);
            e.Property(x => x.Diferenca).HasPrecision(18, 2);
        });
    }
}
