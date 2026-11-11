/* ============================================================
   NAXAPA - NaxapaDB
   Banco de dados completo - Versão 1.0
   SQL Server
   ============================================================

   Observações:
   - Script pensado para instalação limpa/inicial.
   - O banco é multiestabelecimento.
   - Senhas NUNCA são armazenadas em texto puro.
   - O backend/API será responsável pela autenticação/autorização.
   - Procedures e triggers foram usados apenas onde há justificativa.
   ============================================================ */

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

USE master;
GO

IF DB_ID(N'NaxapaDB') IS NULL
BEGIN
    CREATE DATABASE NaxapaDB;
END
GO

USE NaxapaDB;
GO

/* ============================================================
   1. SCHEMAS
   ============================================================ */

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'seg')
    EXEC('CREATE SCHEMA seg');
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'cad')
    EXEC('CREATE SCHEMA cad');
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'cfg')
    EXEC('CREATE SCHEMA cfg');
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'op')
    EXEC('CREATE SCHEMA op');
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'fin')
    EXEC('CREATE SCHEMA fin');
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'est')
    EXEC('CREATE SCHEMA est');
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'imp')
    EXEC('CREATE SCHEMA imp');
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'aud')
    EXEC('CREATE SCHEMA aud');
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'rel')
    EXEC('CREATE SCHEMA rel');
GO

/* ============================================================
   2. SEQUENCES
   ============================================================ */

IF OBJECT_ID(N'op.SeqNumeroPedido', N'SO') IS NULL
BEGIN
    EXEC('CREATE SEQUENCE op.SeqNumeroPedido
          AS BIGINT
          START WITH 1
          INCREMENT BY 1
          MINVALUE 1
          NO CYCLE
          CACHE 50;');
END
GO

/* ============================================================
   3. ESTABELECIMENTO
   ============================================================ */

CREATE TABLE cad.Estabelecimento
(
    EstabelecimentoId BIGINT IDENTITY(1,1) NOT NULL,
    NomeFantasia NVARCHAR(150) NOT NULL,
    RazaoSocial NVARCHAR(150) NULL,
    Documento VARCHAR(14) NULL,
    InscricaoEstadual VARCHAR(20) NULL,

    Telefone VARCHAR(20) NULL,
    Email NVARCHAR(256) NULL,

    Logradouro NVARCHAR(150) NULL,
    Numero NVARCHAR(20) NULL,
    Complemento NVARCHAR(100) NULL,
    Bairro NVARCHAR(100) NULL,
    Cidade NVARCHAR(100) NULL,
    UF CHAR(2) NULL,
    CEP VARCHAR(8) NULL,

    LogoUrl NVARCHAR(500) NULL,
    HoraAbertura TIME(0) NULL,
    HoraFechamento TIME(0) NULL,

    Ativo BIT NOT NULL
        CONSTRAINT DF_Estabelecimento_Ativo DEFAULT (1),

    CriadoEm DATETIME2(0) NOT NULL
        CONSTRAINT DF_Estabelecimento_CriadoEm DEFAULT (SYSUTCDATETIME()),

    AtualizadoEm DATETIME2(0) NULL,

    RowVersion ROWVERSION NOT NULL,

    CONSTRAINT PK_Estabelecimento
        PRIMARY KEY CLUSTERED (EstabelecimentoId),

    CONSTRAINT UQ_Estabelecimento_Id
        UNIQUE (EstabelecimentoId),

    CONSTRAINT CK_Estabelecimento_Documento
        CHECK (Documento IS NULL OR LEN(Documento) IN (11,14)),

    CONSTRAINT CK_Estabelecimento_UF
        CHECK (UF IS NULL OR LEN(UF) = 2),

    CONSTRAINT CK_Estabelecimento_CEP
        CHECK (CEP IS NULL OR LEN(CEP) = 8),

    CONSTRAINT CK_Estabelecimento_Horario
        CHECK (
            HoraAbertura IS NULL
            OR HoraFechamento IS NULL
            OR HoraAbertura <> HoraFechamento
        )
);
GO

CREATE UNIQUE INDEX UX_Estabelecimento_Documento
ON cad.Estabelecimento (Documento)
WHERE Documento IS NOT NULL;
GO

CREATE TABLE cfg.ConfiguracaoEstabelecimento
(
    EstabelecimentoId BIGINT NOT NULL,

    TemaPadrao VARCHAR(10) NOT NULL
        CONSTRAINT DF_ConfigEst_Tema DEFAULT ('SISTEMA'),

    Moeda CHAR(3) NOT NULL
        CONSTRAINT DF_ConfigEst_Moeda DEFAULT ('BRL'),

    FusoHorario NVARCHAR(100) NOT NULL
        CONSTRAINT DF_ConfigEst_Fuso DEFAULT ('America/Sao_Paulo'),

    PermiteEstoqueNegativo BIT NOT NULL
        CONSTRAINT DF_ConfigEst_EstoqueNegativo DEFAULT (0),

    ImprimeAutomaticamente BIT NOT NULL
        CONSTRAINT DF_ConfigEst_ImprimeAuto DEFAULT (1),

    AtualizadoEm DATETIME2(0) NOT NULL
        CONSTRAINT DF_ConfigEst_AtualizadoEm DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_ConfiguracaoEstabelecimento
        PRIMARY KEY (EstabelecimentoId),

    CONSTRAINT FK_ConfigEst_Estabelecimento
        FOREIGN KEY (EstabelecimentoId)
        REFERENCES cad.Estabelecimento (EstabelecimentoId),

    CONSTRAINT CK_ConfigEst_Tema
        CHECK (TemaPadrao IN ('CLARO','ESCURO','SISTEMA'))
);
GO

/* ============================================================
   4. SEGURANÇA
   ============================================================ */

CREATE TABLE seg.Usuario
(
    UsuarioId BIGINT IDENTITY(1,1) NOT NULL,

    Nome NVARCHAR(150) NOT NULL,

    UserName NVARCHAR(100) NOT NULL,
    NormalizedUserName NVARCHAR(100) NOT NULL,

    Email NVARCHAR(256) NOT NULL,
    NormalizedEmail NVARCHAR(256) NOT NULL,

    EmailConfirmado BIT NOT NULL
        CONSTRAINT DF_Usuario_EmailConfirmado DEFAULT (0),

    SenhaHash NVARCHAR(500) NOT NULL,

    Telefone VARCHAR(20) NULL,

    TelefoneConfirmado BIT NOT NULL
        CONSTRAINT DF_Usuario_TelefoneConfirmado DEFAULT (0),

    AutenticacaoDoisFatores BIT NOT NULL
        CONSTRAINT DF_Usuario_2FA DEFAULT (0),

    BloqueioHabilitado BIT NOT NULL
        CONSTRAINT DF_Usuario_BloqueioHabilitado DEFAULT (1),

    SecurityStamp NVARCHAR(100) NULL,
    ConcurrencyStamp NVARCHAR(100) NULL,

    Ativo BIT NOT NULL
        CONSTRAINT DF_Usuario_Ativo DEFAULT (1),

    BloqueioFim DATETIMEOFFSET NULL,

    TentativasAcessoFalho INT NOT NULL
        CONSTRAINT DF_Usuario_Tentativas DEFAULT (0),

    CriadoEm DATETIME2(0) NOT NULL
        CONSTRAINT DF_Usuario_CriadoEm DEFAULT (SYSUTCDATETIME()),

    AtualizadoEm DATETIME2(0) NULL,
    UltimoAcessoEm DATETIME2(0) NULL,

    RowVersion ROWVERSION NOT NULL,

    CONSTRAINT PK_Usuario
        PRIMARY KEY CLUSTERED (UsuarioId),

    CONSTRAINT CK_Usuario_Tentativas
        CHECK (TentativasAcessoFalho >= 0)
);
GO

CREATE UNIQUE INDEX UX_Usuario_NormalizedUserName
ON seg.Usuario (NormalizedUserName);
GO

CREATE UNIQUE INDEX UX_Usuario_NormalizedEmail
ON seg.Usuario (NormalizedEmail);
GO

CREATE TABLE seg.RefreshToken
(
    RefreshTokenId BIGINT IDENTITY(1,1) NOT NULL,
    UsuarioId BIGINT NOT NULL,

    TokenHash VARBINARY(64) NOT NULL,

    CriadoEm DATETIME2(0) NOT NULL
        CONSTRAINT DF_RefreshToken_CriadoEm DEFAULT (SYSUTCDATETIME()),

    ExpiraEm DATETIME2(0) NOT NULL,
    RevogadoEm DATETIME2(0) NULL,

    EnderecoIP VARCHAR(45) NULL,
    UserAgent NVARCHAR(300) NULL,

    CONSTRAINT PK_RefreshToken
        PRIMARY KEY (RefreshTokenId),

    CONSTRAINT UQ_RefreshToken_Hash
        UNIQUE (TokenHash),

    CONSTRAINT FK_RefreshToken_Usuario
        FOREIGN KEY (UsuarioId)
        REFERENCES seg.Usuario (UsuarioId),

    CONSTRAINT CK_RefreshToken_Periodo
        CHECK (ExpiraEm > CriadoEm)
);
GO

CREATE INDEX IX_RefreshToken_Usuario_Expiracao
ON seg.RefreshToken (UsuarioId, ExpiraEm)
INCLUDE (RevogadoEm);
GO

CREATE TABLE seg.Perfil
(
    PerfilId BIGINT IDENTITY(1,1) NOT NULL,

    Nome NVARCHAR(100) NOT NULL,

    Escopo VARCHAR(20) NOT NULL
        CONSTRAINT DF_Perfil_Escopo DEFAULT ('ESTABELECIMENTO'),

    Descricao NVARCHAR(300) NULL,

    Ativo BIT NOT NULL
        CONSTRAINT DF_Perfil_Ativo DEFAULT (1),

    CriadoEm DATETIME2(0) NOT NULL
        CONSTRAINT DF_Perfil_CriadoEm DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Perfil
        PRIMARY KEY (PerfilId),

    CONSTRAINT UQ_Perfil_Nome
        UNIQUE (Nome),

    CONSTRAINT CK_Perfil_Escopo
        CHECK (Escopo IN ('SISTEMA','ESTABELECIMENTO'))
);
GO

CREATE TABLE seg.Permissao
(
    PermissaoId BIGINT IDENTITY(1,1) NOT NULL,

    Chave NVARCHAR(150) NOT NULL,
    Modulo NVARCHAR(100) NOT NULL,
    Descricao NVARCHAR(300) NULL,

    Ativo BIT NOT NULL
        CONSTRAINT DF_Permissao_Ativo DEFAULT (1),

    CONSTRAINT PK_Permissao
        PRIMARY KEY (PermissaoId),

    CONSTRAINT UQ_Permissao_Chave
        UNIQUE (Chave)
);
GO

CREATE TABLE seg.PerfilPermissao
(
    PerfilId BIGINT NOT NULL,
    PermissaoId BIGINT NOT NULL,

    CONSTRAINT PK_PerfilPermissao
        PRIMARY KEY (PerfilId, PermissaoId),

    CONSTRAINT FK_PerfilPermissao_Perfil
        FOREIGN KEY (PerfilId)
        REFERENCES seg.Perfil (PerfilId),

    CONSTRAINT FK_PerfilPermissao_Permissao
        FOREIGN KEY (PermissaoId)
        REFERENCES seg.Permissao (PermissaoId)
);
GO

CREATE TABLE seg.UsuarioPerfilGlobal
(
    UsuarioId BIGINT NOT NULL,
    PerfilId BIGINT NOT NULL,

    CONSTRAINT PK_UsuarioPerfilGlobal
        PRIMARY KEY (UsuarioId, PerfilId),

    CONSTRAINT FK_UsuarioPerfilGlobal_Usuario
        FOREIGN KEY (UsuarioId)
        REFERENCES seg.Usuario (UsuarioId),

    CONSTRAINT FK_UsuarioPerfilGlobal_Perfil
        FOREIGN KEY (PerfilId)
        REFERENCES seg.Perfil (PerfilId)
);
GO

CREATE TABLE seg.UsuarioEstabelecimento
(
    UsuarioEstabelecimentoId BIGINT IDENTITY(1,1) NOT NULL,

    UsuarioId BIGINT NOT NULL,
    EstabelecimentoId BIGINT NOT NULL,

    Principal BIT NOT NULL
        CONSTRAINT DF_UsuarioEstabelecimento_Principal DEFAULT (0),

    Ativo BIT NOT NULL
        CONSTRAINT DF_UsuarioEstabelecimento_Ativo DEFAULT (1),

    CriadoEm DATETIME2(0) NOT NULL
        CONSTRAINT DF_UsuarioEstabelecimento_CriadoEm DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_UsuarioEstabelecimento
        PRIMARY KEY (UsuarioEstabelecimentoId),

    CONSTRAINT UQ_UsuarioEstabelecimento
        UNIQUE (UsuarioId, EstabelecimentoId),

    CONSTRAINT FK_UsuarioEstabelecimento_Usuario
        FOREIGN KEY (UsuarioId)
        REFERENCES seg.Usuario (UsuarioId),

    CONSTRAINT FK_UsuarioEstabelecimento_Estabelecimento
        FOREIGN KEY (EstabelecimentoId)
        REFERENCES cad.Estabelecimento (EstabelecimentoId)
);
GO

CREATE TABLE seg.UsuarioEstabelecimentoPerfil
(
    UsuarioEstabelecimentoId BIGINT NOT NULL,
    PerfilId BIGINT NOT NULL,

    CONSTRAINT PK_UsuarioEstabelecimentoPerfil
        PRIMARY KEY (UsuarioEstabelecimentoId, PerfilId),

    CONSTRAINT FK_UsuarioEstabelecimentoPerfil_UsuarioEst
        FOREIGN KEY (UsuarioEstabelecimentoId)
        REFERENCES seg.UsuarioEstabelecimento (UsuarioEstabelecimentoId),

    CONSTRAINT FK_UsuarioEstabelecimentoPerfil_Perfil
        FOREIGN KEY (PerfilId)
        REFERENCES seg.Perfil (PerfilId)
);
GO

/* ============================================================
   5. CADASTROS
   ============================================================ */

CREATE TABLE cad.Cliente
(
    ClienteId BIGINT IDENTITY(1,1) NOT NULL,
    EstabelecimentoId BIGINT NOT NULL,

    CodigoInterno NVARCHAR(30) NULL,
    Nome NVARCHAR(150) NOT NULL,
    Documento VARCHAR(14) NULL,

    Telefone VARCHAR(20) NULL,
    Email NVARCHAR(256) NULL,

    Logradouro NVARCHAR(150) NULL,
    Numero NVARCHAR(20) NULL,
    Complemento NVARCHAR(100) NULL,
    Bairro NVARCHAR(100) NULL,
    Cidade NVARCHAR(100) NULL,
    UF CHAR(2) NULL,
    CEP VARCHAR(8) NULL,

    Observacao NVARCHAR(500) NULL,

    Ativo BIT NOT NULL
        CONSTRAINT DF_Cliente_Ativo DEFAULT (1),

    CriadoEm DATETIME2(0) NOT NULL
        CONSTRAINT DF_Cliente_CriadoEm DEFAULT (SYSUTCDATETIME()),

    AtualizadoEm DATETIME2(0) NULL,

    RowVersion ROWVERSION NOT NULL,

    CONSTRAINT PK_Cliente
        PRIMARY KEY (ClienteId),

    CONSTRAINT UQ_Cliente_IdEst
        UNIQUE (ClienteId, EstabelecimentoId),

    CONSTRAINT FK_Cliente_Estabelecimento
        FOREIGN KEY (EstabelecimentoId)
        REFERENCES cad.Estabelecimento (EstabelecimentoId),

    CONSTRAINT CK_Cliente_Documento
        CHECK (Documento IS NULL OR LEN(Documento) IN (11,14)),

    CONSTRAINT CK_Cliente_UF
        CHECK (UF IS NULL OR LEN(UF) = 2),

    CONSTRAINT CK_Cliente_CEP
        CHECK (CEP IS NULL OR LEN(CEP) = 8)
);
GO

CREATE UNIQUE INDEX UX_Cliente_Estabelecimento_Documento
ON cad.Cliente (EstabelecimentoId, Documento)
WHERE Documento IS NOT NULL;
GO

CREATE UNIQUE INDEX UX_Cliente_Estabelecimento_Codigo
ON cad.Cliente (EstabelecimentoId, CodigoInterno)
WHERE CodigoInterno IS NOT NULL;
GO

CREATE INDEX IX_Cliente_Estabelecimento_Nome
ON cad.Cliente (EstabelecimentoId, Nome)
INCLUDE (Documento, Telefone, Ativo);
GO

CREATE TABLE cad.Categoria
(
    CategoriaId BIGINT IDENTITY(1,1) NOT NULL,
    EstabelecimentoId BIGINT NOT NULL,

    Nome NVARCHAR(100) NOT NULL,
    Descricao NVARCHAR(300) NULL,
    ImagemUrl NVARCHAR(500) NULL,

    OrdemExibicao INT NOT NULL
        CONSTRAINT DF_Categoria_Ordem DEFAULT (0),

    Ativo BIT NOT NULL
        CONSTRAINT DF_Categoria_Ativo DEFAULT (1),

    CriadoEm DATETIME2(0) NOT NULL
        CONSTRAINT DF_Categoria_CriadoEm DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Categoria
        PRIMARY KEY (CategoriaId),

    CONSTRAINT UQ_Categoria_IdEst
        UNIQUE (CategoriaId, EstabelecimentoId),

    CONSTRAINT UQ_Categoria_Estabelecimento_Nome
        UNIQUE (EstabelecimentoId, Nome),

    CONSTRAINT FK_Categoria_Estabelecimento
        FOREIGN KEY (EstabelecimentoId)
        REFERENCES cad.Estabelecimento (EstabelecimentoId),

    CONSTRAINT CK_Categoria_Ordem
        CHECK (OrdemExibicao >= 0)
);
GO

CREATE TABLE cad.SetorProducao
(
    SetorProducaoId BIGINT IDENTITY(1,1) NOT NULL,
    EstabelecimentoId BIGINT NOT NULL,

    Nome NVARCHAR(100) NOT NULL,
    Descricao NVARCHAR(300) NULL,

    Ativo BIT NOT NULL
        CONSTRAINT DF_SetorProducao_Ativo DEFAULT (1),

    CriadoEm DATETIME2(0) NOT NULL
        CONSTRAINT DF_SetorProducao_CriadoEm DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_SetorProducao
        PRIMARY KEY (SetorProducaoId),

    CONSTRAINT UQ_SetorProducao_IdEst
        UNIQUE (SetorProducaoId, EstabelecimentoId),

    CONSTRAINT UQ_SetorProducao_Estabelecimento_Nome
        UNIQUE (EstabelecimentoId, Nome),

    CONSTRAINT FK_SetorProducao_Estabelecimento
        FOREIGN KEY (EstabelecimentoId)
        REFERENCES cad.Estabelecimento (EstabelecimentoId)
);
GO

CREATE TABLE cad.Produto
(
    ProdutoId BIGINT IDENTITY(1,1) NOT NULL,
    EstabelecimentoId BIGINT NOT NULL,

    CategoriaId BIGINT NOT NULL,
    SetorProducaoId BIGINT NULL,

    CodigoInterno NVARCHAR(30) NULL,
    CodigoBarras NVARCHAR(30) NULL,

    Nome NVARCHAR(150) NOT NULL,
    Descricao NVARCHAR(500) NULL,

    PrecoVenda DECIMAL(18,2) NOT NULL,
    Custo DECIMAL(18,2) NULL,

    UnidadeMedida NVARCHAR(10) NOT NULL
        CONSTRAINT DF_Produto_Unidade DEFAULT ('UN'),

    DisponivelCardapio BIT NOT NULL
        CONSTRAINT DF_Produto_DisponivelCardapio DEFAULT (1),

    ControlaEstoque BIT NOT NULL
        CONSTRAINT DF_Produto_ControlaEstoque DEFAULT (0),

    Ativo BIT NOT NULL
        CONSTRAINT DF_Produto_Ativo DEFAULT (1),

    CriadoEm DATETIME2(0) NOT NULL
        CONSTRAINT DF_Produto_CriadoEm DEFAULT (SYSUTCDATETIME()),

    AtualizadoEm DATETIME2(0) NULL,

    RowVersion ROWVERSION NOT NULL,

    CONSTRAINT PK_Produto
        PRIMARY KEY (ProdutoId),

    CONSTRAINT UQ_Produto_IdEst
        UNIQUE (ProdutoId, EstabelecimentoId),

    CONSTRAINT FK_Produto_Estabelecimento
        FOREIGN KEY (EstabelecimentoId)
        REFERENCES cad.Estabelecimento (EstabelecimentoId),

    CONSTRAINT FK_Produto_Categoria_MesmoEstabelecimento
        FOREIGN KEY (CategoriaId, EstabelecimentoId)
        REFERENCES cad.Categoria (CategoriaId, EstabelecimentoId),

    CONSTRAINT FK_Produto_Setor_MesmoEstabelecimento
        FOREIGN KEY (SetorProducaoId, EstabelecimentoId)
        REFERENCES cad.SetorProducao (SetorProducaoId, EstabelecimentoId),

    CONSTRAINT CK_Produto_PrecoVenda
        CHECK (PrecoVenda >= 0),

    CONSTRAINT CK_Produto_Custo
        CHECK (Custo IS NULL OR Custo >= 0)
);
GO

CREATE UNIQUE INDEX UX_Produto_CodigoInterno
ON cad.Produto (EstabelecimentoId, CodigoInterno)
WHERE CodigoInterno IS NOT NULL;
GO

CREATE UNIQUE INDEX UX_Produto_CodigoBarras
ON cad.Produto (EstabelecimentoId, CodigoBarras)
WHERE CodigoBarras IS NOT NULL;
GO

CREATE INDEX IX_Produto_Estabelecimento_Ativo
ON cad.Produto (EstabelecimentoId, Ativo)
INCLUDE (Nome, PrecoVenda, CategoriaId, SetorProducaoId);
GO

CREATE INDEX IX_Produto_Estabelecimento_Categoria
ON cad.Produto (EstabelecimentoId, CategoriaId, Ativo)
INCLUDE (Nome, PrecoVenda);
GO

CREATE INDEX IX_Produto_Estabelecimento_Setor
ON cad.Produto (EstabelecimentoId, SetorProducaoId, Ativo)
INCLUDE (Nome, CategoriaId);
GO

CREATE TABLE cad.ProdutoImagem
(
    ProdutoImagemId BIGINT IDENTITY(1,1) NOT NULL,
    ProdutoId BIGINT NOT NULL,

    Url NVARCHAR(500) NOT NULL,

    Principal BIT NOT NULL
        CONSTRAINT DF_ProdutoImagem_Principal DEFAULT (0),

    OrdemExibicao INT NOT NULL
        CONSTRAINT DF_ProdutoImagem_Ordem DEFAULT (0),

    CONSTRAINT PK_ProdutoImagem
        PRIMARY KEY (ProdutoImagemId),

    CONSTRAINT FK_ProdutoImagem_Produto
        FOREIGN KEY (ProdutoId)
        REFERENCES cad.Produto (ProdutoId),

    CONSTRAINT CK_ProdutoImagem_Ordem
        CHECK (OrdemExibicao >= 0)
);
GO

CREATE TABLE cad.GrupoOpcao
(
    GrupoOpcaoId BIGINT IDENTITY(1,1) NOT NULL,
    EstabelecimentoId BIGINT NOT NULL,

    Nome NVARCHAR(120) NOT NULL,
    Descricao NVARCHAR(300) NULL,

    TipoSelecao CHAR(1) NOT NULL,

    QuantidadeMinima INT NOT NULL
        CONSTRAINT DF_GrupoOpcao_Minimo DEFAULT (0),

    QuantidadeMaxima INT NOT NULL
        CONSTRAINT DF_GrupoOpcao_Maximo DEFAULT (1),

    Obrigatorio BIT NOT NULL
        CONSTRAINT DF_GrupoOpcao_Obrigatorio DEFAULT (0),

    Ativo BIT NOT NULL
        CONSTRAINT DF_GrupoOpcao_Ativo DEFAULT (1),

    CONSTRAINT PK_GrupoOpcao
        PRIMARY KEY (GrupoOpcaoId),

    CONSTRAINT UQ_GrupoOpcao_IdEst
        UNIQUE (GrupoOpcaoId, EstabelecimentoId),

    CONSTRAINT UQ_GrupoOpcao_Estabelecimento_Nome
        UNIQUE (EstabelecimentoId, Nome),

    CONSTRAINT FK_GrupoOpcao_Estabelecimento
        FOREIGN KEY (EstabelecimentoId)
        REFERENCES cad.Estabelecimento (EstabelecimentoId),

    CONSTRAINT CK_GrupoOpcao_TipoSelecao
        CHECK (TipoSelecao IN ('U','M')),

    CONSTRAINT CK_GrupoOpcao_Quantidade
        CHECK (
            QuantidadeMinima >= 0
            AND QuantidadeMaxima >= QuantidadeMinima
        )
);
GO

CREATE TABLE cad.OpcaoProduto
(
    OpcaoProdutoId BIGINT IDENTITY(1,1) NOT NULL,
    EstabelecimentoId BIGINT NOT NULL,
    GrupoOpcaoId BIGINT NOT NULL,

    Nome NVARCHAR(120) NOT NULL,

    ValorAdicional DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_OpcaoProduto_Valor DEFAULT (0),

    OrdemExibicao INT NOT NULL
        CONSTRAINT DF_OpcaoProduto_Ordem DEFAULT (0),

    Ativo BIT NOT NULL
        CONSTRAINT DF_OpcaoProduto_Ativo DEFAULT (1),

    CONSTRAINT PK_OpcaoProduto
        PRIMARY KEY (OpcaoProdutoId),

    CONSTRAINT UQ_OpcaoProduto_IdEst
        UNIQUE (OpcaoProdutoId, EstabelecimentoId),

    CONSTRAINT UQ_OpcaoProduto_Grupo_Nome
        UNIQUE (GrupoOpcaoId, Nome),

    CONSTRAINT FK_OpcaoProduto_Grupo_MesmoEstabelecimento
        FOREIGN KEY (GrupoOpcaoId, EstabelecimentoId)
        REFERENCES cad.GrupoOpcao (GrupoOpcaoId, EstabelecimentoId),

    CONSTRAINT CK_OpcaoProduto_Valor
        CHECK (ValorAdicional >= 0),

    CONSTRAINT CK_OpcaoProduto_Ordem
        CHECK (OrdemExibicao >= 0)
);
GO

CREATE TABLE cad.ProdutoGrupoOpcao
(
    EstabelecimentoId BIGINT NOT NULL,
    ProdutoId BIGINT NOT NULL,
    GrupoOpcaoId BIGINT NOT NULL,

    OrdemExibicao INT NOT NULL
        CONSTRAINT DF_ProdutoGrupoOpcao_Ordem DEFAULT (0),

    CONSTRAINT PK_ProdutoGrupoOpcao
        PRIMARY KEY (ProdutoId, GrupoOpcaoId),

    CONSTRAINT FK_ProdutoGrupoOpcao_Produto
        FOREIGN KEY (ProdutoId, EstabelecimentoId)
        REFERENCES cad.Produto (ProdutoId, EstabelecimentoId),

    CONSTRAINT FK_ProdutoGrupoOpcao_Grupo
        FOREIGN KEY (GrupoOpcaoId, EstabelecimentoId)
        REFERENCES cad.GrupoOpcao (GrupoOpcaoId, EstabelecimentoId),

    CONSTRAINT CK_ProdutoGrupoOpcao_Ordem
        CHECK (OrdemExibicao >= 0)
);
GO

CREATE TABLE cad.Cardapio
(
    CardapioId BIGINT IDENTITY(1,1) NOT NULL,
    EstabelecimentoId BIGINT NOT NULL,

    Nome NVARCHAR(120) NOT NULL,
    Descricao NVARCHAR(300) NULL,

    DataInicio DATETIME2(0) NULL,
    DataFim DATETIME2(0) NULL,

    Publicado BIT NOT NULL
        CONSTRAINT DF_Cardapio_Publicado DEFAULT (0),

    Ativo BIT NOT NULL
        CONSTRAINT DF_Cardapio_Ativo DEFAULT (1),

    CriadoEm DATETIME2(0) NOT NULL
        CONSTRAINT DF_Cardapio_CriadoEm DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Cardapio
        PRIMARY KEY (CardapioId),

    CONSTRAINT UQ_Cardapio_IdEst
        UNIQUE (CardapioId, EstabelecimentoId),

    CONSTRAINT UQ_Cardapio_Estabelecimento_Nome
        UNIQUE (EstabelecimentoId, Nome),

    CONSTRAINT FK_Cardapio_Estabelecimento
        FOREIGN KEY (EstabelecimentoId)
        REFERENCES cad.Estabelecimento (EstabelecimentoId),

    CONSTRAINT CK_Cardapio_Periodo
        CHECK (
            DataInicio IS NULL
            OR DataFim IS NULL
            OR DataFim >= DataInicio
        )
);
GO

CREATE TABLE cad.CardapioProduto
(
    EstabelecimentoId BIGINT NOT NULL,
    CardapioId BIGINT NOT NULL,
    ProdutoId BIGINT NOT NULL,

    OrdemExibicao INT NOT NULL
        CONSTRAINT DF_CardapioProduto_Ordem DEFAULT (0),

    Disponivel BIT NOT NULL
        CONSTRAINT DF_CardapioProduto_Disponivel DEFAULT (1),

    CONSTRAINT PK_CardapioProduto
        PRIMARY KEY (CardapioId, ProdutoId),

    CONSTRAINT FK_CardapioProduto_Cardapio
        FOREIGN KEY (CardapioId, EstabelecimentoId)
        REFERENCES cad.Cardapio (CardapioId, EstabelecimentoId),

    CONSTRAINT FK_CardapioProduto_Produto
        FOREIGN KEY (ProdutoId, EstabelecimentoId)
        REFERENCES cad.Produto (ProdutoId, EstabelecimentoId),

    CONSTRAINT CK_CardapioProduto_Ordem
        CHECK (OrdemExibicao >= 0)
);
GO

/* ============================================================
   6. IMPRESSORAS E CONFIGURAÇÃO DE IMPRESSÃO
   ============================================================ */

CREATE TABLE cfg.Impressora
(
    ImpressoraId BIGINT IDENTITY(1,1) NOT NULL,
    EstabelecimentoId BIGINT NOT NULL,
    SetorProducaoId BIGINT NULL,

    Nome NVARCHAR(120) NOT NULL,
    Modelo NVARCHAR(120) NULL,

    TipoConexao NVARCHAR(20) NOT NULL,

    EnderecoIP VARCHAR(45) NULL,
    Porta INT NULL,
    NomeWindows NVARCHAR(200) NULL,

    LarguraPapel INT NULL,

    Ativo BIT NOT NULL
        CONSTRAINT DF_Impressora_Ativo DEFAULT (1),

    CriadoEm DATETIME2(0) NOT NULL
        CONSTRAINT DF_Impressora_CriadoEm DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Impressora
        PRIMARY KEY (ImpressoraId),

    CONSTRAINT UQ_Impressora_IdEst
        UNIQUE (ImpressoraId, EstabelecimentoId),

    CONSTRAINT UQ_Impressora_Estabelecimento_Nome
        UNIQUE (EstabelecimentoId, Nome),

    CONSTRAINT FK_Impressora_Estabelecimento
        FOREIGN KEY (EstabelecimentoId)
        REFERENCES cad.Estabelecimento (EstabelecimentoId),

    CONSTRAINT FK_Impressora_Setor
        FOREIGN KEY (SetorProducaoId, EstabelecimentoId)
        REFERENCES cad.SetorProducao (SetorProducaoId, EstabelecimentoId),

    CONSTRAINT CK_Impressora_TipoConexao
        CHECK (TipoConexao IN ('REDE','WINDOWS','USB')),

    CONSTRAINT CK_Impressora_Porta
        CHECK (Porta IS NULL OR Porta BETWEEN 1 AND 65535),

    CONSTRAINT CK_Impressora_Largura
        CHECK (LarguraPapel IS NULL OR LarguraPapel BETWEEN 40 AND 120)
);
GO

CREATE INDEX IX_Impressora_Estabelecimento_Setor
ON cfg.Impressora (EstabelecimentoId, SetorProducaoId, Ativo);
GO

CREATE TABLE cfg.ConfiguracaoImpressao
(
    ConfiguracaoImpressaoId BIGINT IDENTITY(1,1) NOT NULL,
    EstabelecimentoId BIGINT NOT NULL,

    ImpressoraId BIGINT NOT NULL,
    SetorProducaoId BIGINT NULL,

    TipoDocumento NVARCHAR(30) NOT NULL,

    QuantidadeCopias INT NOT NULL
        CONSTRAINT DF_ConfigImpressao_Copias DEFAULT (1),

    Ativo BIT NOT NULL
        CONSTRAINT DF_ConfigImpressao_Ativo DEFAULT (1),

    CONSTRAINT PK_ConfiguracaoImpressao
        PRIMARY KEY (ConfiguracaoImpressaoId),

    CONSTRAINT FK_ConfigImpressao_Impressora
        FOREIGN KEY (ImpressoraId, EstabelecimentoId)
        REFERENCES cfg.Impressora (ImpressoraId, EstabelecimentoId),

    CONSTRAINT FK_ConfigImpressao_Setor
        FOREIGN KEY (SetorProducaoId, EstabelecimentoId)
        REFERENCES cad.SetorProducao (SetorProducaoId, EstabelecimentoId),

    CONSTRAINT CK_ConfigImpressao_TipoDocumento
        CHECK (TipoDocumento IN ('PRODUCAO','COMPROVANTE','RELATORIO')),

    CONSTRAINT CK_ConfigImpressao_Copias
        CHECK (QuantidadeCopias BETWEEN 1 AND 10)
);
GO

/* ============================================================
   7. MESA E COMANDA
   ============================================================ */

CREATE TABLE op.Mesa
(
    MesaId BIGINT IDENTITY(1,1) NOT NULL,
    EstabelecimentoId BIGINT NOT NULL,

    Numero INT NOT NULL,
    Nome NVARCHAR(100) NULL,

    Capacidade INT NULL,

    Status VARCHAR(15) NOT NULL
        CONSTRAINT DF_Mesa_Status DEFAULT ('LIVRE'),

    Ativo BIT NOT NULL
        CONSTRAINT DF_Mesa_Ativo DEFAULT (1),

    CONSTRAINT PK_Mesa
        PRIMARY KEY (MesaId),

    CONSTRAINT UQ_Mesa_IdEst
        UNIQUE (MesaId, EstabelecimentoId),

    CONSTRAINT UQ_Mesa_Estabelecimento_Numero
        UNIQUE (EstabelecimentoId, Numero),

    CONSTRAINT FK_Mesa_Estabelecimento
        FOREIGN KEY (EstabelecimentoId)
        REFERENCES cad.Estabelecimento (EstabelecimentoId),

    CONSTRAINT CK_Mesa_Numero
        CHECK (Numero > 0),

    CONSTRAINT CK_Mesa_Capacidade
        CHECK (Capacidade IS NULL OR Capacidade > 0),

    CONSTRAINT CK_Mesa_Status
        CHECK (Status IN ('LIVRE','OCUPADA','RESERVADA','INATIVA'))
);
GO

CREATE TABLE op.Comanda
(
    ComandaId BIGINT IDENTITY(1,1) NOT NULL,
    EstabelecimentoId BIGINT NOT NULL,
    ClienteId BIGINT NULL,

    Codigo NVARCHAR(50) NOT NULL,

    Status VARCHAR(15) NOT NULL
        CONSTRAINT DF_Comanda_Status DEFAULT ('LIVRE'),

    AbertaEm DATETIME2(0) NULL,
    FechadaEm DATETIME2(0) NULL,

    Observacao NVARCHAR(300) NULL,

    CONSTRAINT PK_Comanda
        PRIMARY KEY (ComandaId),

    CONSTRAINT UQ_Comanda_IdEst
        UNIQUE (ComandaId, EstabelecimentoId),

    CONSTRAINT UQ_Comanda_Estabelecimento_Codigo
        UNIQUE (EstabelecimentoId, Codigo),

    CONSTRAINT FK_Comanda_Estabelecimento
        FOREIGN KEY (EstabelecimentoId)
        REFERENCES cad.Estabelecimento (EstabelecimentoId),

    CONSTRAINT FK_Comanda_Cliente
        FOREIGN KEY (ClienteId, EstabelecimentoId)
        REFERENCES cad.Cliente (ClienteId, EstabelecimentoId),

    CONSTRAINT CK_Comanda_Status
        CHECK (Status IN ('LIVRE','ABERTA','FECHADA','BLOQUEADA'))
);
GO

/* ============================================================
   8. PEDIDOS
   ============================================================ */

CREATE TABLE op.Pedido
(
    PedidoId BIGINT IDENTITY(1,1) NOT NULL,

    Numero BIGINT NOT NULL
        CONSTRAINT DF_Pedido_Numero DEFAULT (NEXT VALUE FOR op.SeqNumeroPedido),

    EstabelecimentoId BIGINT NOT NULL,

    ClienteId BIGINT NULL,
    MesaId BIGINT NULL,
    ComandaId BIGINT NULL,

    TipoAtendimento VARCHAR(20) NOT NULL,

    StatusOperacional VARCHAR(20) NOT NULL
        CONSTRAINT DF_Pedido_StatusOperacional DEFAULT ('NOVO'),

    StatusFinanceiro VARCHAR(20) NOT NULL
        CONSTRAINT DF_Pedido_StatusFinanceiro DEFAULT ('PENDENTE'),

    Subtotal DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_Pedido_Subtotal DEFAULT (0),

    Desconto DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_Pedido_Desconto DEFAULT (0),

    Acrescimo DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_Pedido_Acrescimo DEFAULT (0),

    Total AS CONVERT(DECIMAL(18,2), Subtotal - Desconto + Acrescimo) PERSISTED,

    Observacao NVARCHAR(500) NULL,

    CriadoPorUsuarioId BIGINT NOT NULL,

    CriadoEm DATETIME2(0) NOT NULL
        CONSTRAINT DF_Pedido_CriadoEm DEFAULT (SYSUTCDATETIME()),

    AtualizadoEm DATETIME2(0) NULL,
    FinalizadoEm DATETIME2(0) NULL,
    CanceladoEm DATETIME2(0) NULL,

    MotivoCancelamento NVARCHAR(500) NULL,

    RowVersion ROWVERSION NOT NULL,

    CONSTRAINT PK_Pedido
        PRIMARY KEY (PedidoId),

    CONSTRAINT UQ_Pedido_IdEst
        UNIQUE (PedidoId, EstabelecimentoId),

    CONSTRAINT UQ_Pedido_Numero
        UNIQUE (Numero),

    CONSTRAINT FK_Pedido_Estabelecimento
        FOREIGN KEY (EstabelecimentoId)
        REFERENCES cad.Estabelecimento (EstabelecimentoId),

    CONSTRAINT FK_Pedido_Cliente
        FOREIGN KEY (ClienteId, EstabelecimentoId)
        REFERENCES cad.Cliente (ClienteId, EstabelecimentoId),

    CONSTRAINT FK_Pedido_Mesa
        FOREIGN KEY (MesaId, EstabelecimentoId)
        REFERENCES op.Mesa (MesaId, EstabelecimentoId),

    CONSTRAINT FK_Pedido_Comanda
        FOREIGN KEY (ComandaId, EstabelecimentoId)
        REFERENCES op.Comanda (ComandaId, EstabelecimentoId),

    CONSTRAINT FK_Pedido_Usuario
        FOREIGN KEY (CriadoPorUsuarioId)
        REFERENCES seg.Usuario (UsuarioId),

    CONSTRAINT CK_Pedido_TipoAtendimento
        CHECK (
            TipoAtendimento IN
            ('VENDA_AVULSA','BALCAO','MESA','COMANDA','DELIVERY')
        ),

    CONSTRAINT CK_Pedido_StatusOperacional
        CHECK (
            StatusOperacional IN
            ('NOVO','PENDENTE','EM_PREPARACAO','PRONTO','FINALIZADO','CANCELADO')
        ),

    CONSTRAINT CK_Pedido_StatusFinanceiro
        CHECK (
            StatusFinanceiro IN
            ('PENDENTE','PARCIAL','PAGO','CANCELADO')
        ),

    CONSTRAINT CK_Pedido_Valores
        CHECK (
            Subtotal >= 0
            AND Desconto >= 0
            AND Acrescimo >= 0
            AND Desconto <= (Subtotal + Acrescimo)
        )
);
GO

CREATE INDEX IX_Pedido_Estabelecimento_Status_Data
ON op.Pedido (EstabelecimentoId, StatusOperacional, CriadoEm)
INCLUDE (Numero, ClienteId, StatusFinanceiro, Subtotal, Desconto, Acrescimo);
GO

CREATE INDEX IX_Pedido_Estabelecimento_Cliente_Data
ON op.Pedido (EstabelecimentoId, ClienteId, CriadoEm DESC)
INCLUDE (Numero, StatusOperacional, StatusFinanceiro);
GO

CREATE TABLE op.PedidoItem
(
    PedidoItemId BIGINT IDENTITY(1,1) NOT NULL,

    EstabelecimentoId BIGINT NOT NULL,
    PedidoId BIGINT NOT NULL,
    ProdutoId BIGINT NOT NULL,

    NomeProduto NVARCHAR(150) NOT NULL,

    Quantidade DECIMAL(18,3) NOT NULL,

    ValorUnitario DECIMAL(18,2) NOT NULL,

    Desconto DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_PedidoItem_Desconto DEFAULT (0),

    Acrescimo DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_PedidoItem_Acrescimo DEFAULT (0),

    ValorOpcoes DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_PedidoItem_ValorOpcoes DEFAULT (0),

    TotalItem AS CONVERT(
        DECIMAL(18,2),
        (Quantidade * ValorUnitario) - Desconto + Acrescimo + ValorOpcoes
    ) PERSISTED,

    Observacao NVARCHAR(500) NULL,

    CriadoEm DATETIME2(0) NOT NULL
        CONSTRAINT DF_PedidoItem_CriadoEm DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_PedidoItem
        PRIMARY KEY (PedidoItemId),

    CONSTRAINT UQ_PedidoItem_IdEst
        UNIQUE (PedidoItemId, EstabelecimentoId),

    CONSTRAINT FK_PedidoItem_Pedido
        FOREIGN KEY (PedidoId, EstabelecimentoId)
        REFERENCES op.Pedido (PedidoId, EstabelecimentoId),

    CONSTRAINT FK_PedidoItem_Produto
        FOREIGN KEY (ProdutoId, EstabelecimentoId)
        REFERENCES cad.Produto (ProdutoId, EstabelecimentoId),

    CONSTRAINT CK_PedidoItem_Quantidade
        CHECK (Quantidade > 0),

    CONSTRAINT CK_PedidoItem_Valores
        CHECK (
            ValorUnitario >= 0
            AND Desconto >= 0
            AND Acrescimo >= 0
            AND ValorOpcoes >= 0
            AND Desconto <= ((Quantidade * ValorUnitario) + Acrescimo + ValorOpcoes)
        )
);
GO

CREATE INDEX IX_PedidoItem_Pedido
ON op.PedidoItem (PedidoId)
INCLUDE (ProdutoId, Quantidade, ValorUnitario, TotalItem);
GO

CREATE TABLE op.PedidoItemOpcao
(
    PedidoItemOpcaoId BIGINT IDENTITY(1,1) NOT NULL,

    EstabelecimentoId BIGINT NOT NULL,
    PedidoItemId BIGINT NOT NULL,
    OpcaoProdutoId BIGINT NOT NULL,

    NomeOpcao NVARCHAR(120) NOT NULL,

    Quantidade INT NOT NULL
        CONSTRAINT DF_PedidoItemOpcao_Quantidade DEFAULT (1),

    ValorUnitarioAdicional DECIMAL(18,2) NOT NULL,

    TotalOpcao AS CONVERT(
        DECIMAL(18,2),
        Quantidade * ValorUnitarioAdicional
    ) PERSISTED,

    CONSTRAINT PK_PedidoItemOpcao
        PRIMARY KEY (PedidoItemOpcaoId),

    CONSTRAINT UQ_PedidoItemOpcao_ItemOpcao
        UNIQUE (PedidoItemId, OpcaoProdutoId),

    CONSTRAINT FK_PedidoItemOpcao_Item
        FOREIGN KEY (PedidoItemId, EstabelecimentoId)
        REFERENCES op.PedidoItem (PedidoItemId, EstabelecimentoId),

    CONSTRAINT FK_PedidoItemOpcao_Opcao
        FOREIGN KEY (OpcaoProdutoId, EstabelecimentoId)
        REFERENCES cad.OpcaoProduto (OpcaoProdutoId, EstabelecimentoId),

    CONSTRAINT CK_PedidoItemOpcao_Quantidade
        CHECK (Quantidade > 0),

    CONSTRAINT CK_PedidoItemOpcao_Valor
        CHECK (ValorUnitarioAdicional >= 0)
);
GO

CREATE TABLE op.HistoricoStatusPedido
(
    HistoricoStatusPedidoId BIGINT IDENTITY(1,1) NOT NULL,

    EstabelecimentoId BIGINT NOT NULL,
    PedidoId BIGINT NOT NULL,

    StatusAnterior VARCHAR(20) NULL,
    StatusNovo VARCHAR(20) NOT NULL,

    UsuarioId BIGINT NOT NULL,

    Observacao NVARCHAR(500) NULL,

    CriadoEm DATETIME2(0) NOT NULL
        CONSTRAINT DF_HistoricoStatus_CriadoEm DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_HistoricoStatusPedido
        PRIMARY KEY (HistoricoStatusPedidoId),

    CONSTRAINT FK_HistoricoStatus_Pedido
        FOREIGN KEY (PedidoId, EstabelecimentoId)
        REFERENCES op.Pedido (PedidoId, EstabelecimentoId),

    CONSTRAINT FK_HistoricoStatus_Usuario
        FOREIGN KEY (UsuarioId)
        REFERENCES seg.Usuario (UsuarioId)
);
GO

CREATE INDEX IX_HistoricoStatus_Pedido_Data
ON op.HistoricoStatusPedido (PedidoId, CriadoEm);
GO

/* ============================================================
   9. FORMAS DE PAGAMENTO E PAGAMENTOS
   ============================================================ */

CREATE TABLE fin.FormaPagamento
(
    FormaPagamentoId BIGINT IDENTITY(1,1) NOT NULL,
    EstabelecimentoId BIGINT NOT NULL,

    Codigo NVARCHAR(30) NOT NULL,
    Nome NVARCHAR(100) NOT NULL,

    Tipo VARCHAR(20) NOT NULL,

    PermiteTroco BIT NOT NULL
        CONSTRAINT DF_FormaPagamento_Troco DEFAULT (0),

    Ativo BIT NOT NULL
        CONSTRAINT DF_FormaPagamento_Ativo DEFAULT (1),

    CONSTRAINT PK_FormaPagamento
        PRIMARY KEY (FormaPagamentoId),

    CONSTRAINT UQ_FormaPagamento_IdEst
        UNIQUE (FormaPagamentoId, EstabelecimentoId),

    CONSTRAINT UQ_FormaPagamento_Codigo
        UNIQUE (EstabelecimentoId, Codigo),

    CONSTRAINT FK_FormaPagamento_Estabelecimento
        FOREIGN KEY (EstabelecimentoId)
        REFERENCES cad.Estabelecimento (EstabelecimentoId),

    CONSTRAINT CK_FormaPagamento_Tipo
        CHECK (Tipo IN ('DINHEIRO','CREDITO','DEBITO','PIX','OUTRO'))
);
GO

CREATE TABLE fin.Pagamento
(
    PagamentoId BIGINT IDENTITY(1,1) NOT NULL,

    EstabelecimentoId BIGINT NOT NULL,
    PedidoId BIGINT NOT NULL,
    FormaPagamentoId BIGINT NOT NULL,

    Valor DECIMAL(18,2) NOT NULL,

    ValorRecebido DECIMAL(18,2) NULL,
    Troco DECIMAL(18,2) NULL,

    Status VARCHAR(20) NOT NULL
        CONSTRAINT DF_Pagamento_Status DEFAULT ('CONFIRMADO'),

    ReferenciaTransacao NVARCHAR(150) NULL,

    CriadoPorUsuarioId BIGINT NOT NULL,

    CriadoEm DATETIME2(0) NOT NULL
        CONSTRAINT DF_Pagamento_CriadoEm DEFAULT (SYSUTCDATETIME()),

    AtualizadoEm DATETIME2(0) NULL,

    CONSTRAINT PK_Pagamento
        PRIMARY KEY (PagamentoId),

    CONSTRAINT FK_Pagamento_Pedido
        FOREIGN KEY (PedidoId, EstabelecimentoId)
        REFERENCES op.Pedido (PedidoId, EstabelecimentoId),

    CONSTRAINT FK_Pagamento_Forma
        FOREIGN KEY (FormaPagamentoId, EstabelecimentoId)
        REFERENCES fin.FormaPagamento (FormaPagamentoId, EstabelecimentoId),

    CONSTRAINT FK_Pagamento_Usuario
        FOREIGN KEY (CriadoPorUsuarioId)
        REFERENCES seg.Usuario (UsuarioId),

    CONSTRAINT CK_Pagamento_Valor
        CHECK (Valor > 0),

    CONSTRAINT CK_Pagamento_Recebido
        CHECK (ValorRecebido IS NULL OR ValorRecebido >= 0),

    CONSTRAINT CK_Pagamento_Troco
        CHECK (Troco IS NULL OR Troco >= 0),

    CONSTRAINT CK_Pagamento_Status
        CHECK (Status IN ('PENDENTE','CONFIRMADO','CANCELADO','ESTORNADO'))
);
GO

CREATE INDEX IX_Pagamento_Pedido_Status
ON fin.Pagamento (PedidoId, Status)
INCLUDE (Valor, FormaPagamentoId, CriadoEm);
GO

/* ============================================================
   10. ESTOQUE
   ============================================================ */

CREATE TABLE est.EstoqueProduto
(
    EstoqueProdutoId BIGINT IDENTITY(1,1) NOT NULL,

    EstabelecimentoId BIGINT NOT NULL,
    ProdutoId BIGINT NOT NULL,

    Saldo DECIMAL(18,3) NOT NULL
        CONSTRAINT DF_EstoqueProduto_Saldo DEFAULT (0),

    EstoqueMinimo DECIMAL(18,3) NULL,

    AtualizadoEm DATETIME2(0) NOT NULL
        CONSTRAINT DF_EstoqueProduto_AtualizadoEm DEFAULT (SYSUTCDATETIME()),

    RowVersion ROWVERSION NOT NULL,

    CONSTRAINT PK_EstoqueProduto
        PRIMARY KEY (EstoqueProdutoId),

    CONSTRAINT UQ_EstoqueProduto
        UNIQUE (EstabelecimentoId, ProdutoId),

    CONSTRAINT FK_EstoqueProduto_Produto
        FOREIGN KEY (ProdutoId, EstabelecimentoId)
        REFERENCES cad.Produto (ProdutoId, EstabelecimentoId),

    CONSTRAINT CK_EstoqueProduto_Minimo
        CHECK (EstoqueMinimo IS NULL OR EstoqueMinimo >= 0)
);
GO

CREATE TABLE est.MovimentacaoEstoque
(
    MovimentacaoEstoqueId BIGINT IDENTITY(1,1) NOT NULL,

    EstabelecimentoId BIGINT NOT NULL,
    ProdutoId BIGINT NOT NULL,
    PedidoItemId BIGINT NULL,

    TipoMovimento VARCHAR(20) NOT NULL,

    Quantidade DECIMAL(18,3) NOT NULL,

    SaldoAnterior DECIMAL(18,3) NOT NULL,
    SaldoPosterior DECIMAL(18,3) NOT NULL,

    Origem NVARCHAR(50) NULL,
    Observacao NVARCHAR(500) NULL,

    UsuarioId BIGINT NOT NULL,

    CriadoEm DATETIME2(0) NOT NULL
        CONSTRAINT DF_MovimentacaoEstoque_CriadoEm DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_MovimentacaoEstoque
        PRIMARY KEY (MovimentacaoEstoqueId),

    CONSTRAINT FK_MovEstoque_Produto
        FOREIGN KEY (ProdutoId, EstabelecimentoId)
        REFERENCES cad.Produto (ProdutoId, EstabelecimentoId),

    CONSTRAINT FK_MovEstoque_PedidoItem
        FOREIGN KEY (PedidoItemId, EstabelecimentoId)
        REFERENCES op.PedidoItem (PedidoItemId, EstabelecimentoId),

    CONSTRAINT FK_MovEstoque_Usuario
        FOREIGN KEY (UsuarioId)
        REFERENCES seg.Usuario (UsuarioId),

    CONSTRAINT CK_MovEstoque_Tipo
        CHECK (
            TipoMovimento IN
            ('ENTRADA','SAIDA','AJUSTE_ENTRADA','AJUSTE_SAIDA')
        ),

    CONSTRAINT CK_MovEstoque_Quantidade
        CHECK (Quantidade > 0)
);
GO

CREATE INDEX IX_MovEstoque_Produto_Data
ON est.MovimentacaoEstoque (EstabelecimentoId, ProdutoId, CriadoEm DESC);
GO

/* ============================================================
   11. FILA DE IMPRESSÃO
   ============================================================ */

CREATE TABLE imp.TrabalhoImpressao
(
    TrabalhoImpressaoId BIGINT IDENTITY(1,1) NOT NULL,

    EstabelecimentoId BIGINT NOT NULL,

    PedidoId BIGINT NULL,
    ImpressoraId BIGINT NOT NULL,
    SetorProducaoId BIGINT NULL,

    TipoDocumento VARCHAR(30) NOT NULL,

    Status VARCHAR(20) NOT NULL
        CONSTRAINT DF_TrabalhoImpressao_Status DEFAULT ('PENDENTE'),

    Tentativas INT NOT NULL
        CONSTRAINT DF_TrabalhoImpressao_Tentativas DEFAULT (0),

    HashIdempotencia UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_TrabalhoImpressao_Hash DEFAULT (NEWID()),

    PayloadJson NVARCHAR(MAX) NOT NULL,

    UltimoErro NVARCHAR(1000) NULL,

    SolicitadoEm DATETIME2(0) NOT NULL
        CONSTRAINT DF_TrabalhoImpressao_SolicitadoEm DEFAULT (SYSUTCDATETIME()),

    ProcessadoEm DATETIME2(0) NULL,

    CONSTRAINT PK_TrabalhoImpressao
        PRIMARY KEY (TrabalhoImpressaoId),

    CONSTRAINT UQ_TrabalhoImpressao_Hash
        UNIQUE (HashIdempotencia),

    CONSTRAINT FK_TrabalhoImpressao_Pedido
        FOREIGN KEY (PedidoId, EstabelecimentoId)
        REFERENCES op.Pedido (PedidoId, EstabelecimentoId),

    CONSTRAINT FK_TrabalhoImpressao_Impressora
        FOREIGN KEY (ImpressoraId, EstabelecimentoId)
        REFERENCES cfg.Impressora (ImpressoraId, EstabelecimentoId),

    CONSTRAINT FK_TrabalhoImpressao_Setor
        FOREIGN KEY (SetorProducaoId, EstabelecimentoId)
        REFERENCES cad.SetorProducao (SetorProducaoId, EstabelecimentoId),

    CONSTRAINT CK_TrabalhoImpressao_Status
        CHECK (
            Status IN ('PENDENTE','PROCESSANDO','IMPRESSO','ERRO','CANCELADO')
        ),

    CONSTRAINT CK_TrabalhoImpressao_Tentativas
        CHECK (Tentativas >= 0),

    CONSTRAINT CK_TrabalhoImpressao_Json
        CHECK (ISJSON(PayloadJson) = 1)
);
GO

CREATE INDEX IX_TrabalhoImpressao_Fila
ON imp.TrabalhoImpressao (EstabelecimentoId, Status, SolicitadoEm)
INCLUDE (ImpressoraId, PedidoId, Tentativas);
GO

/* ============================================================
   12. AUDITORIA
   ============================================================ */

CREATE TABLE aud.Auditoria
(
    AuditoriaId BIGINT IDENTITY(1,1) NOT NULL,

    EstabelecimentoId BIGINT NULL,
    UsuarioId BIGINT NULL,

    Entidade NVARCHAR(100) NOT NULL,
    EntidadeId NVARCHAR(100) NULL,

    Acao NVARCHAR(50) NOT NULL,

    DadosAnteriores NVARCHAR(MAX) NULL,
    DadosNovos NVARCHAR(MAX) NULL,

    EnderecoIP VARCHAR(45) NULL,
    CorrelationId UNIQUEIDENTIFIER NULL,

    CriadoEm DATETIME2(0) NOT NULL
        CONSTRAINT DF_Auditoria_CriadoEm DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Auditoria
        PRIMARY KEY (AuditoriaId),

    CONSTRAINT FK_Auditoria_Estabelecimento
        FOREIGN KEY (EstabelecimentoId)
        REFERENCES cad.Estabelecimento (EstabelecimentoId),

    CONSTRAINT FK_Auditoria_Usuario
        FOREIGN KEY (UsuarioId)
        REFERENCES seg.Usuario (UsuarioId),

    CONSTRAINT CK_Auditoria_DadosAnteriores
        CHECK (DadosAnteriores IS NULL OR ISJSON(DadosAnteriores) = 1),

    CONSTRAINT CK_Auditoria_DadosNovos
        CHECK (DadosNovos IS NULL OR ISJSON(DadosNovos) = 1)
);
GO

CREATE INDEX IX_Auditoria_Estabelecimento_Data
ON aud.Auditoria (EstabelecimentoId, CriadoEm DESC)
INCLUDE (UsuarioId, Entidade, Acao);
GO

/* ============================================================
   13. TRIGGERS
   ============================================================ */

CREATE OR ALTER TRIGGER cad.trg_Produto_AtualizarData
ON cad.Produto
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF TRIGGER_NESTLEVEL() > 1
        RETURN;

    UPDATE p
       SET AtualizadoEm = SYSUTCDATETIME()
    FROM cad.Produto p
    INNER JOIN inserted i
        ON i.ProdutoId = p.ProdutoId;
END;
GO

CREATE OR ALTER TRIGGER op.trg_PedidoItem_RecalcularSubtotal
ON op.PedidoItem
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH PedidosAfetados AS
    (
        SELECT PedidoId FROM inserted
        UNION
        SELECT PedidoId FROM deleted
    )
    UPDATE p
       SET Subtotal = ISNULL(x.Subtotal, 0),
           AtualizadoEm = SYSUTCDATETIME()
    FROM op.Pedido p
    INNER JOIN PedidosAfetados a
        ON a.PedidoId = p.PedidoId
    OUTER APPLY
    (
        SELECT SUM(pi.TotalItem) AS Subtotal
        FROM op.PedidoItem pi
        WHERE pi.PedidoId = p.PedidoId
    ) x;
END;
GO

CREATE OR ALTER TRIGGER op.trg_PedidoItemOpcao_Recalcular
ON op.PedidoItemOpcao
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH ItensAfetados AS
    (
        SELECT PedidoItemId FROM inserted
        UNION
        SELECT PedidoItemId FROM deleted
    )
    UPDATE pi
       SET ValorOpcoes = ISNULL(x.TotalOpcoes, 0)
    FROM op.PedidoItem pi
    INNER JOIN ItensAfetados a
        ON a.PedidoItemId = pi.PedidoItemId
    OUTER APPLY
    (
        SELECT SUM(pio.TotalOpcao) AS TotalOpcoes
        FROM op.PedidoItemOpcao pio
        WHERE pio.PedidoItemId = pi.PedidoItemId
    ) x;
END;
GO

CREATE OR ALTER TRIGGER fin.trg_Pagamento_RecalcularStatusPedido
ON fin.Pagamento
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH PedidosAfetados AS
    (
        SELECT PedidoId FROM inserted
        UNION
        SELECT PedidoId FROM deleted
    ),
    Totais AS
    (
        SELECT
            p.PedidoId,
            p.Total,
            ISNULL(SUM(
                CASE
                    WHEN pg.Status = 'CONFIRMADO' THEN pg.Valor
                    ELSE 0
                END
            ), 0) AS TotalPago
        FROM op.Pedido p
        INNER JOIN PedidosAfetados a
            ON a.PedidoId = p.PedidoId
        LEFT JOIN fin.Pagamento pg
            ON pg.PedidoId = p.PedidoId
        GROUP BY p.PedidoId, p.Total
    )
    UPDATE p
       SET StatusFinanceiro =
           CASE
               WHEN p.StatusOperacional = 'CANCELADO' THEN 'CANCELADO'
               WHEN t.TotalPago <= 0 THEN 'PENDENTE'
               WHEN t.TotalPago < t.Total THEN 'PARCIAL'
               ELSE 'PAGO'
           END,
           AtualizadoEm = SYSUTCDATETIME()
    FROM op.Pedido p
    INNER JOIN Totais t
        ON t.PedidoId = p.PedidoId;
END;
GO

/* ============================================================
   14. PROCEDURES - PRODUTOS
   ============================================================ */

CREATE OR ALTER PROCEDURE cad.sp_Produto_Listar
(
    @EstabelecimentoId BIGINT,
    @Pesquisa NVARCHAR(150) = NULL,
    @CategoriaId BIGINT = NULL,
    @Ativo BIT = NULL,
    @Pagina INT = 1,
    @QuantidadePorPagina INT = 20
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @Pagina < 1
        SET @Pagina = 1;

    IF @QuantidadePorPagina < 1
        SET @QuantidadePorPagina = 20;

    IF @QuantidadePorPagina > 100
        SET @QuantidadePorPagina = 100;

    SELECT
        p.ProdutoId,
        p.CodigoInterno,
        p.CodigoBarras,
        p.Nome,
        p.Descricao,
        p.PrecoVenda,
        p.Custo,
        p.UnidadeMedida,
        p.DisponivelCardapio,
        p.ControlaEstoque,
        p.Ativo,
        c.CategoriaId,
        c.Nome AS Categoria,
        s.SetorProducaoId,
        s.Nome AS SetorProducao
    FROM cad.Produto p
    INNER JOIN cad.Categoria c
        ON c.CategoriaId = p.CategoriaId
    LEFT JOIN cad.SetorProducao s
        ON s.SetorProducaoId = p.SetorProducaoId
    WHERE
        p.EstabelecimentoId = @EstabelecimentoId
        AND (@CategoriaId IS NULL OR p.CategoriaId = @CategoriaId)
        AND (@Ativo IS NULL OR p.Ativo = @Ativo)
        AND (
            @Pesquisa IS NULL
            OR p.Nome LIKE '%' + @Pesquisa + '%'
            OR p.CodigoInterno LIKE '%' + @Pesquisa + '%'
            OR p.CodigoBarras LIKE '%' + @Pesquisa + '%'
        )
    ORDER BY p.Nome
    OFFSET (@Pagina - 1) * @QuantidadePorPagina ROWS
    FETCH NEXT @QuantidadePorPagina ROWS ONLY;

    SELECT COUNT_BIG(*) AS TotalRegistros
    FROM cad.Produto p
    WHERE
        p.EstabelecimentoId = @EstabelecimentoId
        AND (@CategoriaId IS NULL OR p.CategoriaId = @CategoriaId)
        AND (@Ativo IS NULL OR p.Ativo = @Ativo)
        AND (
            @Pesquisa IS NULL
            OR p.Nome LIKE '%' + @Pesquisa + '%'
            OR p.CodigoInterno LIKE '%' + @Pesquisa + '%'
            OR p.CodigoBarras LIKE '%' + @Pesquisa + '%'
        );
END;
GO

/* ============================================================
   15. PROCEDURES - PEDIDOS
   ============================================================ */

CREATE OR ALTER PROCEDURE op.sp_Pedido_Criar
(
    @EstabelecimentoId BIGINT,
    @CriadoPorUsuarioId BIGINT,
    @TipoAtendimento VARCHAR(20),
    @ClienteId BIGINT = NULL,
    @MesaId BIGINT = NULL,
    @ComandaId BIGINT = NULL,
    @Observacao NVARCHAR(500) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (
        SELECT 1
        FROM cad.Estabelecimento
        WHERE EstabelecimentoId = @EstabelecimentoId
          AND Ativo = 1
    )
        THROW 50001, 'Estabelecimento inválido ou inativo.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM seg.Usuario
        WHERE UsuarioId = @CriadoPorUsuarioId
          AND Ativo = 1
    )
        THROW 50002, 'Usuário inválido ou inativo.', 1;

    IF @ClienteId IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM cad.Cliente
            WHERE ClienteId = @ClienteId
              AND EstabelecimentoId = @EstabelecimentoId
              AND Ativo = 1
       )
        THROW 50003, 'Cliente inválido para o estabelecimento.', 1;

    IF @MesaId IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM op.Mesa
            WHERE MesaId = @MesaId
              AND EstabelecimentoId = @EstabelecimentoId
              AND Ativo = 1
       )
        THROW 50004, 'Mesa inválida para o estabelecimento.', 1;

    IF @ComandaId IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM op.Comanda
            WHERE ComandaId = @ComandaId
              AND EstabelecimentoId = @EstabelecimentoId
       )
        THROW 50005, 'Comanda inválida para o estabelecimento.', 1;

    INSERT INTO op.Pedido
    (
        EstabelecimentoId,
        ClienteId,
        MesaId,
        ComandaId,
        TipoAtendimento,
        CriadoPorUsuarioId,
        Observacao
    )
    VALUES
    (
        @EstabelecimentoId,
        @ClienteId,
        @MesaId,
        @ComandaId,
        @TipoAtendimento,
        @CriadoPorUsuarioId,
        @Observacao
    );

    DECLARE @PedidoId BIGINT = SCOPE_IDENTITY();

    INSERT INTO op.HistoricoStatusPedido
    (
        EstabelecimentoId,
        PedidoId,
        StatusAnterior,
        StatusNovo,
        UsuarioId,
        Observacao
    )
    VALUES
    (
        @EstabelecimentoId,
        @PedidoId,
        NULL,
        'NOVO',
        @CriadoPorUsuarioId,
        'Pedido criado'
    );

    SELECT *
    FROM op.Pedido
    WHERE PedidoId = @PedidoId;
END;
GO

CREATE OR ALTER PROCEDURE op.sp_Pedido_AdicionarItem
(
    @EstabelecimentoId BIGINT,
    @PedidoId BIGINT,
    @ProdutoId BIGINT,
    @Quantidade DECIMAL(18,3),
    @Observacao NVARCHAR(500) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Quantidade <= 0
        THROW 50010, 'Quantidade deve ser maior que zero.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM op.Pedido
        WHERE PedidoId = @PedidoId
          AND EstabelecimentoId = @EstabelecimentoId
          AND StatusOperacional NOT IN ('FINALIZADO','CANCELADO')
    )
        THROW 50011, 'Pedido inválido ou não pode mais ser alterado.', 1;

    DECLARE
        @NomeProduto NVARCHAR(150),
        @PrecoVenda DECIMAL(18,2);

    SELECT
        @NomeProduto = Nome,
        @PrecoVenda = PrecoVenda
    FROM cad.Produto
    WHERE ProdutoId = @ProdutoId
      AND EstabelecimentoId = @EstabelecimentoId
      AND Ativo = 1;

    IF @NomeProduto IS NULL
        THROW 50012, 'Produto inválido ou inativo.', 1;

    INSERT INTO op.PedidoItem
    (
        EstabelecimentoId,
        PedidoId,
        ProdutoId,
        NomeProduto,
        Quantidade,
        ValorUnitario,
        Observacao
    )
    VALUES
    (
        @EstabelecimentoId,
        @PedidoId,
        @ProdutoId,
        @NomeProduto,
        @Quantidade,
        @PrecoVenda,
        @Observacao
    );

    DECLARE @PedidoItemId BIGINT = SCOPE_IDENTITY();

    SELECT *
    FROM op.PedidoItem
    WHERE PedidoItemId = @PedidoItemId;
END;
GO

CREATE OR ALTER PROCEDURE op.sp_Pedido_AdicionarOpcao
(
    @EstabelecimentoId BIGINT,
    @PedidoItemId BIGINT,
    @OpcaoProdutoId BIGINT,
    @Quantidade INT = 1
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Quantidade <= 0
        THROW 50020, 'Quantidade da opção deve ser maior que zero.', 1;

    DECLARE
        @ProdutoId BIGINT,
        @PedidoId BIGINT;

    SELECT
        @ProdutoId = pi.ProdutoId,
        @PedidoId = pi.PedidoId
    FROM op.PedidoItem pi
    INNER JOIN op.Pedido p
        ON p.PedidoId = pi.PedidoId
    WHERE pi.PedidoItemId = @PedidoItemId
      AND pi.EstabelecimentoId = @EstabelecimentoId
      AND p.StatusOperacional NOT IN ('FINALIZADO','CANCELADO');

    IF @ProdutoId IS NULL
        THROW 50021, 'Item de pedido inválido ou pedido bloqueado.', 1;

    DECLARE
        @NomeOpcao NVARCHAR(120),
        @ValorAdicional DECIMAL(18,2),
        @GrupoOpcaoId BIGINT;

    SELECT
        @NomeOpcao = o.Nome,
        @ValorAdicional = o.ValorAdicional,
        @GrupoOpcaoId = o.GrupoOpcaoId
    FROM cad.OpcaoProduto o
    WHERE o.OpcaoProdutoId = @OpcaoProdutoId
      AND o.EstabelecimentoId = @EstabelecimentoId
      AND o.Ativo = 1;

    IF @NomeOpcao IS NULL
        THROW 50022, 'Opção inválida ou inativa.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM cad.ProdutoGrupoOpcao
        WHERE EstabelecimentoId = @EstabelecimentoId
          AND ProdutoId = @ProdutoId
          AND GrupoOpcaoId = @GrupoOpcaoId
    )
        THROW 50023, 'A opção não está vinculada ao produto do item.', 1;

    IF EXISTS (
        SELECT 1
        FROM op.PedidoItemOpcao
        WHERE PedidoItemId = @PedidoItemId
          AND OpcaoProdutoId = @OpcaoProdutoId
    )
        THROW 50024, 'A opção já foi adicionada ao item. Altere a quantidade em vez de duplicá-la.', 1;

    DECLARE
        @QuantidadeMaxima INT,
        @QuantidadeSelecionada INT;

    SELECT @QuantidadeMaxima = QuantidadeMaxima
    FROM cad.GrupoOpcao
    WHERE GrupoOpcaoId = @GrupoOpcaoId
      AND EstabelecimentoId = @EstabelecimentoId
      AND Ativo = 1;

    SELECT @QuantidadeSelecionada = ISNULL(SUM(pio.Quantidade), 0)
    FROM op.PedidoItemOpcao pio
    INNER JOIN cad.OpcaoProduto opx
        ON opx.OpcaoProdutoId = pio.OpcaoProdutoId
    WHERE pio.PedidoItemId = @PedidoItemId
      AND opx.GrupoOpcaoId = @GrupoOpcaoId;

    IF ISNULL(@QuantidadeSelecionada, 0) + @Quantidade > @QuantidadeMaxima
        THROW 50025, 'Quantidade máxima permitida para o grupo de opções excedida.', 1;

    INSERT INTO op.PedidoItemOpcao
    (
        EstabelecimentoId,
        PedidoItemId,
        OpcaoProdutoId,
        NomeOpcao,
        Quantidade,
        ValorUnitarioAdicional
    )
    VALUES
    (
        @EstabelecimentoId,
        @PedidoItemId,
        @OpcaoProdutoId,
        @NomeOpcao,
        @Quantidade,
        @ValorAdicional
    );

    SELECT *
    FROM op.PedidoItemOpcao
    WHERE PedidoItemOpcaoId = SCOPE_IDENTITY();
END;
GO

CREATE OR ALTER PROCEDURE op.sp_Pedido_AlterarStatus
(
    @EstabelecimentoId BIGINT,
    @PedidoId BIGINT,
    @NovoStatus VARCHAR(20),
    @UsuarioId BIGINT,
    @Observacao NVARCHAR(500) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRAN;

    DECLARE
        @StatusAtual VARCHAR(20),
        @StatusFinanceiro VARCHAR(20);

    SELECT
        @StatusAtual = StatusOperacional,
        @StatusFinanceiro = StatusFinanceiro
    FROM op.Pedido WITH (UPDLOCK, HOLDLOCK)
    WHERE PedidoId = @PedidoId
      AND EstabelecimentoId = @EstabelecimentoId;

    IF @StatusAtual IS NULL
    BEGIN
        ROLLBACK;
        THROW 50030, 'Pedido não encontrado.', 1;
    END

    IF @NovoStatus NOT IN
       ('NOVO','PENDENTE','EM_PREPARACAO','PRONTO','FINALIZADO','CANCELADO')
    BEGIN
        ROLLBACK;
        THROW 50031, 'Status inválido.', 1;
    END

    IF @StatusAtual = @NovoStatus
    BEGIN
        COMMIT;
        RETURN;
    END

    IF @NovoStatus = 'PENDENTE'
    BEGIN
        IF NOT EXISTS (
            SELECT 1
            FROM op.PedidoItem
            WHERE PedidoId = @PedidoId
        )
        BEGIN
            ROLLBACK;
            THROW 50034, 'O pedido precisa possuir pelo menos um item.', 1;
        END

        IF EXISTS
        (
            SELECT 1
            FROM op.PedidoItem pi
            INNER JOIN cad.ProdutoGrupoOpcao pgo
                ON pgo.ProdutoId = pi.ProdutoId
               AND pgo.EstabelecimentoId = pi.EstabelecimentoId
            INNER JOIN cad.GrupoOpcao g
                ON g.GrupoOpcaoId = pgo.GrupoOpcaoId
               AND g.EstabelecimentoId = pgo.EstabelecimentoId
            OUTER APPLY
            (
                SELECT ISNULL(SUM(pio.Quantidade), 0) AS QuantidadeSelecionada
                FROM op.PedidoItemOpcao pio
                INNER JOIN cad.OpcaoProduto o
                    ON o.OpcaoProdutoId = pio.OpcaoProdutoId
                WHERE pio.PedidoItemId = pi.PedidoItemId
                  AND o.GrupoOpcaoId = g.GrupoOpcaoId
            ) sel
            WHERE pi.PedidoId = @PedidoId
              AND g.Ativo = 1
              AND (
                    sel.QuantidadeSelecionada < g.QuantidadeMinima
                    OR sel.QuantidadeSelecionada > g.QuantidadeMaxima
                    OR (g.Obrigatorio = 1 AND sel.QuantidadeSelecionada = 0)
                  )
        )
        BEGIN
            ROLLBACK;
            THROW 50035, 'Existem grupos de opções obrigatórios ou quantidades de seleção inválidas.', 1;
        END
    END

    IF NOT (
        (@StatusAtual = 'NOVO' AND @NovoStatus IN ('PENDENTE','CANCELADO'))
        OR
        (@StatusAtual = 'PENDENTE' AND @NovoStatus IN ('EM_PREPARACAO','CANCELADO'))
        OR
        (@StatusAtual = 'EM_PREPARACAO' AND @NovoStatus IN ('PRONTO','CANCELADO'))
        OR
        (@StatusAtual = 'PRONTO' AND @NovoStatus IN ('FINALIZADO','CANCELADO'))
    )
    BEGIN
        ROLLBACK;
        THROW 50032, 'Transição de status não permitida.', 1;
    END

    IF @NovoStatus = 'FINALIZADO'
       AND @StatusFinanceiro <> 'PAGO'
    BEGIN
        ROLLBACK;
        THROW 50033, 'O pedido precisa estar pago para ser finalizado.', 1;
    END

    UPDATE op.Pedido
       SET StatusOperacional = @NovoStatus,
           AtualizadoEm = SYSUTCDATETIME(),
           FinalizadoEm =
               CASE
                   WHEN @NovoStatus = 'FINALIZADO' THEN SYSUTCDATETIME()
                   ELSE FinalizadoEm
               END,
           CanceladoEm =
               CASE
                   WHEN @NovoStatus = 'CANCELADO' THEN SYSUTCDATETIME()
                   ELSE CanceladoEm
               END,
           StatusFinanceiro =
               CASE
                   WHEN @NovoStatus = 'CANCELADO' THEN 'CANCELADO'
                   ELSE StatusFinanceiro
               END
    WHERE PedidoId = @PedidoId
      AND EstabelecimentoId = @EstabelecimentoId;

    INSERT INTO op.HistoricoStatusPedido
    (
        EstabelecimentoId,
        PedidoId,
        StatusAnterior,
        StatusNovo,
        UsuarioId,
        Observacao
    )
    VALUES
    (
        @EstabelecimentoId,
        @PedidoId,
        @StatusAtual,
        @NovoStatus,
        @UsuarioId,
        @Observacao
    );

    COMMIT;
END;
GO

/* ============================================================
   16. PROCEDURES - PAGAMENTOS
   ============================================================ */

CREATE OR ALTER PROCEDURE fin.sp_FormaPagamento_CriarPadroes
(
    @EstabelecimentoId BIGINT
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO fin.FormaPagamento
    (
        EstabelecimentoId,
        Codigo,
        Nome,
        Tipo,
        PermiteTroco
    )
    SELECT
        @EstabelecimentoId,
        v.Codigo,
        v.Nome,
        v.Tipo,
        v.PermiteTroco
    FROM
    (
        VALUES
            ('DINHEIRO', 'Dinheiro', 'DINHEIRO', CAST(1 AS BIT)),
            ('CREDITO', 'Cartão de crédito', 'CREDITO', CAST(0 AS BIT)),
            ('DEBITO', 'Cartão de débito', 'DEBITO', CAST(0 AS BIT)),
            ('PIX', 'PIX', 'PIX', CAST(0 AS BIT))
    ) v(Codigo, Nome, Tipo, PermiteTroco)
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM fin.FormaPagamento f
        WHERE f.EstabelecimentoId = @EstabelecimentoId
          AND f.Codigo = v.Codigo
    );
END;
GO

CREATE OR ALTER PROCEDURE fin.sp_Pagamento_Registrar
(
    @EstabelecimentoId BIGINT,
    @PedidoId BIGINT,
    @FormaPagamentoId BIGINT,
    @Valor DECIMAL(18,2),
    @CriadoPorUsuarioId BIGINT,
    @ValorRecebido DECIMAL(18,2) = NULL,
    @ReferenciaTransacao NVARCHAR(150) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Valor <= 0
        THROW 50040, 'Valor do pagamento deve ser maior que zero.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM op.Pedido
        WHERE PedidoId = @PedidoId
          AND EstabelecimentoId = @EstabelecimentoId
          AND StatusOperacional <> 'CANCELADO'
    )
        THROW 50041, 'Pedido inválido ou cancelado.', 1;

    DECLARE @PermiteTroco BIT;

    SELECT @PermiteTroco = PermiteTroco
    FROM fin.FormaPagamento
    WHERE FormaPagamentoId = @FormaPagamentoId
      AND EstabelecimentoId = @EstabelecimentoId
      AND Ativo = 1;

    IF @PermiteTroco IS NULL
        THROW 50042, 'Forma de pagamento inválida ou inativa.', 1;

    DECLARE @Troco DECIMAL(18,2) = NULL;

    IF @ValorRecebido IS NOT NULL
    BEGIN
        IF @ValorRecebido < @Valor
            THROW 50043, 'Valor recebido não pode ser menor que o pagamento.', 1;

        IF @PermiteTroco = 0 AND @ValorRecebido <> @Valor
            THROW 50044, 'Esta forma de pagamento não permite troco.', 1;

        SET @Troco = @ValorRecebido - @Valor;
    END

    INSERT INTO fin.Pagamento
    (
        EstabelecimentoId,
        PedidoId,
        FormaPagamentoId,
        Valor,
        ValorRecebido,
        Troco,
        Status,
        ReferenciaTransacao,
        CriadoPorUsuarioId
    )
    VALUES
    (
        @EstabelecimentoId,
        @PedidoId,
        @FormaPagamentoId,
        @Valor,
        @ValorRecebido,
        @Troco,
        'CONFIRMADO',
        @ReferenciaTransacao,
        @CriadoPorUsuarioId
    );

    SELECT *
    FROM fin.Pagamento
    WHERE PagamentoId = SCOPE_IDENTITY();
END;
GO

/* ============================================================
   17. PROCEDURES - ESTOQUE
   ============================================================ */

CREATE OR ALTER PROCEDURE est.sp_Estoque_Movimentar
(
    @EstabelecimentoId BIGINT,
    @ProdutoId BIGINT,
    @TipoMovimento VARCHAR(20),
    @Quantidade DECIMAL(18,3),
    @UsuarioId BIGINT,
    @Origem NVARCHAR(50) = NULL,
    @Observacao NVARCHAR(500) = NULL,
    @PedidoItemId BIGINT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Quantidade <= 0
        THROW 50050, 'Quantidade deve ser maior que zero.', 1;

    IF @TipoMovimento NOT IN
       ('ENTRADA','SAIDA','AJUSTE_ENTRADA','AJUSTE_SAIDA')
        THROW 50051, 'Tipo de movimentação inválido.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM cad.Produto
        WHERE ProdutoId = @ProdutoId
          AND EstabelecimentoId = @EstabelecimentoId
          AND Ativo = 1
    )
        THROW 50052, 'Produto inválido.', 1;

    BEGIN TRAN;

    IF NOT EXISTS (
        SELECT 1
        FROM est.EstoqueProduto WITH (UPDLOCK, HOLDLOCK)
        WHERE EstabelecimentoId = @EstabelecimentoId
          AND ProdutoId = @ProdutoId
    )
    BEGIN
        INSERT INTO est.EstoqueProduto
        (
            EstabelecimentoId,
            ProdutoId,
            Saldo
        )
        VALUES
        (
            @EstabelecimentoId,
            @ProdutoId,
            0
        );
    END

    DECLARE
        @SaldoAnterior DECIMAL(18,3),
        @SaldoPosterior DECIMAL(18,3),
        @PermiteNegativo BIT;

    SELECT @SaldoAnterior = Saldo
    FROM est.EstoqueProduto WITH (UPDLOCK, HOLDLOCK)
    WHERE EstabelecimentoId = @EstabelecimentoId
      AND ProdutoId = @ProdutoId;

    SELECT @PermiteNegativo = PermiteEstoqueNegativo
    FROM cfg.ConfiguracaoEstabelecimento
    WHERE EstabelecimentoId = @EstabelecimentoId;

    SET @PermiteNegativo = ISNULL(@PermiteNegativo, 0);

    SET @SaldoPosterior =
        CASE
            WHEN @TipoMovimento IN ('ENTRADA','AJUSTE_ENTRADA')
                THEN @SaldoAnterior + @Quantidade
            ELSE @SaldoAnterior - @Quantidade
        END;

    IF @SaldoPosterior < 0 AND @PermiteNegativo = 0
    BEGIN
        ROLLBACK;
        THROW 50053, 'Saldo insuficiente e estoque negativo não permitido.', 1;
    END

    UPDATE est.EstoqueProduto
       SET Saldo = @SaldoPosterior,
           AtualizadoEm = SYSUTCDATETIME()
    WHERE EstabelecimentoId = @EstabelecimentoId
      AND ProdutoId = @ProdutoId;

    INSERT INTO est.MovimentacaoEstoque
    (
        EstabelecimentoId,
        ProdutoId,
        PedidoItemId,
        TipoMovimento,
        Quantidade,
        SaldoAnterior,
        SaldoPosterior,
        Origem,
        Observacao,
        UsuarioId
    )
    VALUES
    (
        @EstabelecimentoId,
        @ProdutoId,
        @PedidoItemId,
        @TipoMovimento,
        @Quantidade,
        @SaldoAnterior,
        @SaldoPosterior,
        @Origem,
        @Observacao,
        @UsuarioId
    );

    COMMIT;

    SELECT
        EstabelecimentoId,
        ProdutoId,
        Saldo,
        EstoqueMinimo,
        AtualizadoEm
    FROM est.EstoqueProduto
    WHERE EstabelecimentoId = @EstabelecimentoId
      AND ProdutoId = @ProdutoId;
END;
GO

/* ============================================================
   18. PROCEDURES - IMPRESSÃO
   ============================================================ */

CREATE OR ALTER PROCEDURE imp.sp_TrabalhoImpressao_Enfileirar
(
    @EstabelecimentoId BIGINT,
    @PedidoId BIGINT = NULL,
    @ImpressoraId BIGINT,
    @SetorProducaoId BIGINT = NULL,
    @TipoDocumento VARCHAR(30),
    @PayloadJson NVARCHAR(MAX),
    @HashIdempotencia UNIQUEIDENTIFIER = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF ISJSON(@PayloadJson) <> 1
        THROW 50060, 'Payload de impressão deve ser JSON válido.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM cfg.Impressora
        WHERE ImpressoraId = @ImpressoraId
          AND EstabelecimentoId = @EstabelecimentoId
          AND Ativo = 1
    )
        THROW 50061, 'Impressora inválida ou inativa.', 1;

    IF @PedidoId IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM op.Pedido
            WHERE PedidoId = @PedidoId
              AND EstabelecimentoId = @EstabelecimentoId
       )
        THROW 50062, 'Pedido inválido.', 1;

    SET @HashIdempotencia = ISNULL(@HashIdempotencia, NEWID());

    IF EXISTS (
        SELECT 1
        FROM imp.TrabalhoImpressao
        WHERE HashIdempotencia = @HashIdempotencia
    )
    BEGIN
        SELECT *
        FROM imp.TrabalhoImpressao
        WHERE HashIdempotencia = @HashIdempotencia;
        RETURN;
    END

    INSERT INTO imp.TrabalhoImpressao
    (
        EstabelecimentoId,
        PedidoId,
        ImpressoraId,
        SetorProducaoId,
        TipoDocumento,
        HashIdempotencia,
        PayloadJson
    )
    VALUES
    (
        @EstabelecimentoId,
        @PedidoId,
        @ImpressoraId,
        @SetorProducaoId,
        @TipoDocumento,
        @HashIdempotencia,
        @PayloadJson
    );

    SELECT *
    FROM imp.TrabalhoImpressao
    WHERE TrabalhoImpressaoId = SCOPE_IDENTITY();
END;
GO

/* ============================================================
   19. VIEWS
   ============================================================ */

CREATE OR ALTER VIEW cad.vw_CardapioProdutos
AS
SELECT
    cp.EstabelecimentoId,
    c.CardapioId,
    c.Nome AS Cardapio,
    p.ProdutoId,
    p.Nome AS Produto,
    p.Descricao,
    p.PrecoVenda,
    cat.CategoriaId,
    cat.Nome AS Categoria,
    cp.OrdemExibicao,
    cp.Disponivel,
    p.DisponivelCardapio,
    p.Ativo AS ProdutoAtivo,
    c.Ativo AS CardapioAtivo,
    c.Publicado
FROM cad.CardapioProduto cp
INNER JOIN cad.Cardapio c
    ON c.CardapioId = cp.CardapioId
INNER JOIN cad.Produto p
    ON p.ProdutoId = cp.ProdutoId
INNER JOIN cad.Categoria cat
    ON cat.CategoriaId = p.CategoriaId;
GO

CREATE OR ALTER VIEW op.vw_PedidoResumo
AS
SELECT
    p.PedidoId,
    p.Numero,
    p.EstabelecimentoId,
    p.ClienteId,
    c.Nome AS Cliente,
    p.MesaId,
    m.Numero AS NumeroMesa,
    p.ComandaId,
    co.Codigo AS CodigoComanda,
    p.TipoAtendimento,
    p.StatusOperacional,
    p.StatusFinanceiro,
    p.Subtotal,
    p.Desconto,
    p.Acrescimo,
    p.Total,
    ISNULL(SUM(
        CASE
            WHEN pg.Status = 'CONFIRMADO' THEN pg.Valor
            ELSE 0
        END
    ), 0) AS TotalPago,
    p.CriadoEm,
    p.FinalizadoEm
FROM op.Pedido p
LEFT JOIN cad.Cliente c
    ON c.ClienteId = p.ClienteId
LEFT JOIN op.Mesa m
    ON m.MesaId = p.MesaId
LEFT JOIN op.Comanda co
    ON co.ComandaId = p.ComandaId
LEFT JOIN fin.Pagamento pg
    ON pg.PedidoId = p.PedidoId
GROUP BY
    p.PedidoId,
    p.Numero,
    p.EstabelecimentoId,
    p.ClienteId,
    c.Nome,
    p.MesaId,
    m.Numero,
    p.ComandaId,
    co.Codigo,
    p.TipoAtendimento,
    p.StatusOperacional,
    p.StatusFinanceiro,
    p.Subtotal,
    p.Desconto,
    p.Acrescimo,
    p.Total,
    p.CriadoEm,
    p.FinalizadoEm;
GO

CREATE OR ALTER VIEW rel.vw_VendasDiarias
AS
SELECT
    EstabelecimentoId,
    CONVERT(DATE, FinalizadoEm) AS DataVenda,
    COUNT_BIG(*) AS QuantidadePedidos,
    SUM(Total) AS FaturamentoBruto,
    AVG(Total) AS TicketMedio
FROM op.Pedido
WHERE StatusOperacional = 'FINALIZADO'
  AND FinalizadoEm IS NOT NULL
GROUP BY
    EstabelecimentoId,
    CONVERT(DATE, FinalizadoEm);
GO

/* ============================================================
   20. SEED - PERFIS
   ============================================================ */

INSERT INTO seg.Perfil (Nome, Escopo, Descricao)
SELECT v.Nome, v.Escopo, v.Descricao
FROM
(
    VALUES
        (N'Administrador Geral', 'SISTEMA', N'Administração global da plataforma NAXAPA'),
        (N'Administrador do Estabelecimento', 'ESTABELECIMENTO', N'Administração completa do estabelecimento'),
        (N'Gerente', 'ESTABELECIMENTO', N'Gestão operacional e relatórios'),
        (N'Operador de Caixa', 'ESTABELECIMENTO', N'Operações de caixa e pagamentos'),
        (N'Atendente', 'ESTABELECIMENTO', N'Atendimento e pedidos'),
        (N'Produção', 'ESTABELECIMENTO', N'Cozinha, bar e demais setores de produção')
) v(Nome, Escopo, Descricao)
WHERE NOT EXISTS
(
    SELECT 1
    FROM seg.Perfil p
    WHERE p.Nome = v.Nome
);
GO

/* ============================================================
   21. SEED - PERMISSÕES
   ============================================================ */

INSERT INTO seg.Permissao (Chave, Modulo, Descricao)
SELECT v.Chave, v.Modulo, v.Descricao
FROM
(
    VALUES
        (N'ESTABELECIMENTO.VISUALIZAR', N'Estabelecimentos', N'Visualizar estabelecimentos'),
        (N'ESTABELECIMENTO.EDITAR', N'Estabelecimentos', N'Editar estabelecimentos'),

        (N'USUARIO.VISUALIZAR', N'Usuários', N'Visualizar usuários'),
        (N'USUARIO.CRIAR', N'Usuários', N'Criar usuários'),
        (N'USUARIO.EDITAR', N'Usuários', N'Editar usuários'),
        (N'USUARIO.PERMISSOES', N'Usuários', N'Gerenciar perfis e permissões'),

        (N'CLIENTE.VISUALIZAR', N'Clientes', N'Visualizar clientes'),
        (N'CLIENTE.CRIAR', N'Clientes', N'Criar clientes'),
        (N'CLIENTE.EDITAR', N'Clientes', N'Editar clientes'),

        (N'CATEGORIA.VISUALIZAR', N'Categorias', N'Visualizar categorias'),
        (N'CATEGORIA.EDITAR', N'Categorias', N'Gerenciar categorias'),

        (N'PRODUTO.VISUALIZAR', N'Produtos', N'Visualizar produtos'),
        (N'PRODUTO.CRIAR', N'Produtos', N'Criar produtos'),
        (N'PRODUTO.EDITAR', N'Produtos', N'Editar produtos'),

        (N'CARDAPIO.VISUALIZAR', N'Cardápios', N'Visualizar cardápios'),
        (N'CARDAPIO.EDITAR', N'Cardápios', N'Gerenciar cardápios'),

        (N'SETOR.VISUALIZAR', N'Setores', N'Visualizar setores'),
        (N'SETOR.EDITAR', N'Setores', N'Gerenciar setores'),

        (N'IMPRESSORA.VISUALIZAR', N'Impressoras', N'Visualizar impressoras'),
        (N'IMPRESSORA.EDITAR', N'Impressoras', N'Gerenciar impressoras'),

        (N'PEDIDO.VISUALIZAR', N'Pedidos', N'Visualizar pedidos'),
        (N'PEDIDO.CRIAR', N'Pedidos', N'Criar pedidos'),
        (N'PEDIDO.EDITAR', N'Pedidos', N'Editar pedidos'),
        (N'PEDIDO.CANCELAR', N'Pedidos', N'Cancelar pedidos'),
        (N'PEDIDO.FINALIZAR', N'Pedidos', N'Finalizar pedidos'),

        (N'PAGAMENTO.REGISTRAR', N'Pagamentos', N'Registrar pagamentos'),
        (N'PAGAMENTO.ESTORNAR', N'Pagamentos', N'Estornar pagamentos'),

        (N'ESTOQUE.VISUALIZAR', N'Estoque', N'Visualizar estoque'),
        (N'ESTOQUE.MOVIMENTAR', N'Estoque', N'Movimentar estoque'),

        (N'RELATORIO.VISUALIZAR', N'Relatórios', N'Visualizar relatórios'),
        (N'AUDITORIA.VISUALIZAR', N'Auditoria', N'Visualizar auditoria')
) v(Chave, Modulo, Descricao)
WHERE NOT EXISTS
(
    SELECT 1
    FROM seg.Permissao p
    WHERE p.Chave = v.Chave
);
GO

/* Administradores recebem todas as permissões. */
INSERT INTO seg.PerfilPermissao (PerfilId, PermissaoId)
SELECT
    pf.PerfilId,
    pe.PermissaoId
FROM seg.Perfil pf
CROSS JOIN seg.Permissao pe
WHERE pf.Nome IN (N'Administrador Geral', N'Administrador do Estabelecimento')
  AND NOT EXISTS
  (
      SELECT 1
      FROM seg.PerfilPermissao pp
      WHERE pp.PerfilId = pf.PerfilId
        AND pp.PermissaoId = pe.PermissaoId
  );
GO

/* ============================================================
   22. PROCEDURE DE INICIALIZAÇÃO DE ESTABELECIMENTO
   ============================================================ */

CREATE OR ALTER PROCEDURE cad.sp_Estabelecimento_Inicializar
(
    @EstabelecimentoId BIGINT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (
        SELECT 1
        FROM cad.Estabelecimento
        WHERE EstabelecimentoId = @EstabelecimentoId
    )
        THROW 50070, 'Estabelecimento não encontrado.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM cfg.ConfiguracaoEstabelecimento
        WHERE EstabelecimentoId = @EstabelecimentoId
    )
    BEGIN
        INSERT INTO cfg.ConfiguracaoEstabelecimento (EstabelecimentoId)
        VALUES (@EstabelecimentoId);
    END

    EXEC fin.sp_FormaPagamento_CriarPadroes
        @EstabelecimentoId = @EstabelecimentoId;
END;
GO


/* ============================================================
   23. MÓDULO DE CAIXA
   - Cadastro de caixas
   - Abertura e fechamento
   - Sangria e suprimento
   - Vínculo dos pagamentos à sessão
   - Movimentação física do numerário
   - Relatórios detalhados
   ============================================================ */

CREATE TABLE fin.Caixa
(
    CaixaId BIGINT IDENTITY(1,1) NOT NULL,
    EstabelecimentoId BIGINT NOT NULL,

    Codigo NVARCHAR(30) NOT NULL,
    Nome NVARCHAR(100) NOT NULL,
    Descricao NVARCHAR(300) NULL,

    Ativo BIT NOT NULL
        CONSTRAINT DF_Caixa_Ativo DEFAULT (1),

    CriadoEm DATETIME2(0) NOT NULL
        CONSTRAINT DF_Caixa_CriadoEm DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Caixa
        PRIMARY KEY (CaixaId),

    CONSTRAINT UQ_Caixa_IdEst
        UNIQUE (CaixaId, EstabelecimentoId),

    CONSTRAINT UQ_Caixa_Estabelecimento_Codigo
        UNIQUE (EstabelecimentoId, Codigo),

    CONSTRAINT FK_Caixa_Estabelecimento
        FOREIGN KEY (EstabelecimentoId)
        REFERENCES cad.Estabelecimento (EstabelecimentoId)
);
GO

CREATE TABLE fin.CaixaSessao
(
    CaixaSessaoId BIGINT IDENTITY(1,1) NOT NULL,

    EstabelecimentoId BIGINT NOT NULL,
    CaixaId BIGINT NOT NULL,

    UsuarioAberturaId BIGINT NOT NULL,
    UsuarioFechamentoId BIGINT NULL,

    Status VARCHAR(10) NOT NULL
        CONSTRAINT DF_CaixaSessao_Status DEFAULT ('ABERTO'),

    ValorAbertura DECIMAL(18,2) NOT NULL,

    AbertoEm DATETIME2(0) NOT NULL
        CONSTRAINT DF_CaixaSessao_AbertoEm DEFAULT (SYSUTCDATETIME()),

    FechadoEm DATETIME2(0) NULL,

    ValorFechamentoCalculado DECIMAL(18,2) NULL,
    ValorFechamentoInformado DECIMAL(18,2) NULL,

    Diferenca AS
    (
        CASE
            WHEN ValorFechamentoCalculado IS NULL
              OR ValorFechamentoInformado IS NULL
                THEN NULL
            ELSE CONVERT
            (
                DECIMAL(18,2),
                ValorFechamentoInformado - ValorFechamentoCalculado
            )
        END
    ) PERSISTED,

    ObservacaoAbertura NVARCHAR(500) NULL,
    ObservacaoFechamento NVARCHAR(500) NULL,

    RowVersion ROWVERSION NOT NULL,

    CONSTRAINT PK_CaixaSessao
        PRIMARY KEY (CaixaSessaoId),

    CONSTRAINT UQ_CaixaSessao_IdEst
        UNIQUE (CaixaSessaoId, EstabelecimentoId),

    CONSTRAINT FK_CaixaSessao_Caixa
        FOREIGN KEY (CaixaId, EstabelecimentoId)
        REFERENCES fin.Caixa (CaixaId, EstabelecimentoId),

    CONSTRAINT FK_CaixaSessao_UsuarioAbertura
        FOREIGN KEY (UsuarioAberturaId)
        REFERENCES seg.Usuario (UsuarioId),

    CONSTRAINT FK_CaixaSessao_UsuarioFechamento
        FOREIGN KEY (UsuarioFechamentoId)
        REFERENCES seg.Usuario (UsuarioId),

    CONSTRAINT CK_CaixaSessao_Status
        CHECK (Status IN ('ABERTO','FECHADO')),

    CONSTRAINT CK_CaixaSessao_ValorAbertura
        CHECK (ValorAbertura >= 0),

    CONSTRAINT CK_CaixaSessao_ValorFechamentoCalculado
        CHECK (
            ValorFechamentoCalculado IS NULL
            OR ValorFechamentoCalculado >= 0
        ),

    CONSTRAINT CK_CaixaSessao_ValorFechamentoInformado
        CHECK (
            ValorFechamentoInformado IS NULL
            OR ValorFechamentoInformado >= 0
        )
);
GO

/* Garante somente uma sessão ABERTA por caixa. */
CREATE UNIQUE INDEX UX_CaixaSessao_Caixa_Aberto
ON fin.CaixaSessao (CaixaId)
WHERE Status = 'ABERTO';
GO

CREATE INDEX IX_CaixaSessao_Estabelecimento_Data
ON fin.CaixaSessao
(
    EstabelecimentoId,
    AbertoEm DESC
)
INCLUDE
(
    CaixaId,
    UsuarioAberturaId,
    Status,
    FechadoEm,
    ValorAbertura
);
GO

/* Vincula pagamentos à sessão do caixa.
   É nullable porque pagamentos futuros do Mobile/Online
   podem não passar por um caixa físico. */
ALTER TABLE fin.Pagamento
ADD CaixaSessaoId BIGINT NULL;
GO

ALTER TABLE fin.Pagamento
ADD CONSTRAINT UQ_Pagamento_IdEst
    UNIQUE (PagamentoId, EstabelecimentoId);
GO

ALTER TABLE fin.Pagamento
ADD CONSTRAINT FK_Pagamento_CaixaSessao
    FOREIGN KEY (CaixaSessaoId, EstabelecimentoId)
    REFERENCES fin.CaixaSessao (CaixaSessaoId, EstabelecimentoId);
GO

CREATE INDEX IX_Pagamento_CaixaSessao
ON fin.Pagamento
(
    CaixaSessaoId,
    Status,
    CriadoEm
)
INCLUDE
(
    Valor,
    FormaPagamentoId,
    PedidoId
);
GO

CREATE TABLE fin.MovimentoCaixa
(
    MovimentoCaixaId BIGINT IDENTITY(1,1) NOT NULL,

    EstabelecimentoId BIGINT NOT NULL,
    CaixaSessaoId BIGINT NOT NULL,

    PagamentoId BIGINT NULL,

    TipoMovimento VARCHAR(30) NOT NULL,
    Natureza CHAR(1) NOT NULL,

    Valor DECIMAL(18,2) NOT NULL,

    Descricao NVARCHAR(500) NULL,

    UsuarioId BIGINT NOT NULL,

    CriadoEm DATETIME2(0) NOT NULL
        CONSTRAINT DF_MovimentoCaixa_CriadoEm
        DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_MovimentoCaixa
        PRIMARY KEY (MovimentoCaixaId),

    CONSTRAINT FK_MovimentoCaixa_Sessao
        FOREIGN KEY (CaixaSessaoId, EstabelecimentoId)
        REFERENCES fin.CaixaSessao (CaixaSessaoId, EstabelecimentoId),

    CONSTRAINT FK_MovimentoCaixa_Pagamento
        FOREIGN KEY (PagamentoId, EstabelecimentoId)
        REFERENCES fin.Pagamento (PagamentoId, EstabelecimentoId),

    CONSTRAINT FK_MovimentoCaixa_Usuario
        FOREIGN KEY (UsuarioId)
        REFERENCES seg.Usuario (UsuarioId),

    CONSTRAINT CK_MovimentoCaixa_Tipo
        CHECK
        (
            TipoMovimento IN
            (
                'VENDA_DINHEIRO',
                'ESTORNO_DINHEIRO',
                'SUPRIMENTO',
                'SANGRIA',
                'AJUSTE_ENTRADA',
                'AJUSTE_SAIDA'
            )
        ),

    CONSTRAINT CK_MovimentoCaixa_Natureza
        CHECK (Natureza IN ('E','S')),

    CONSTRAINT CK_MovimentoCaixa_Valor
        CHECK (Valor > 0)
);
GO

CREATE INDEX IX_MovimentoCaixa_Sessao_Data
ON fin.MovimentoCaixa
(
    CaixaSessaoId,
    CriadoEm
)
INCLUDE
(
    TipoMovimento,
    Natureza,
    Valor,
    UsuarioId,
    PagamentoId
);
GO

/* ============================================================
   23.1 ABERTURA DE CAIXA
   ============================================================ */

CREATE OR ALTER PROCEDURE fin.sp_Caixa_Abrir
(
    @EstabelecimentoId BIGINT,
    @CaixaId BIGINT,
    @UsuarioId BIGINT,
    @ValorAbertura DECIMAL(18,2),
    @Observacao NVARCHAR(500) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ValorAbertura < 0
        THROW 50100, 'O valor de abertura não pode ser negativo.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM fin.Caixa
        WHERE CaixaId = @CaixaId
          AND EstabelecimentoId = @EstabelecimentoId
          AND Ativo = 1
    )
        THROW 50101, 'Caixa inválido ou inativo.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM seg.Usuario
        WHERE UsuarioId = @UsuarioId
          AND Ativo = 1
    )
        THROW 50102, 'Usuário inválido ou inativo.', 1;

    BEGIN TRAN;

    IF EXISTS
    (
        SELECT 1
        FROM fin.CaixaSessao WITH (UPDLOCK, HOLDLOCK)
        WHERE CaixaId = @CaixaId
          AND Status = 'ABERTO'
    )
    BEGIN
        ROLLBACK;
        THROW 50103, 'Este caixa já possui uma sessão aberta.', 1;
    END

    INSERT INTO fin.CaixaSessao
    (
        EstabelecimentoId,
        CaixaId,
        UsuarioAberturaId,
        ValorAbertura,
        ObservacaoAbertura
    )
    VALUES
    (
        @EstabelecimentoId,
        @CaixaId,
        @UsuarioId,
        @ValorAbertura,
        @Observacao
    );

    DECLARE @CaixaSessaoId BIGINT = SCOPE_IDENTITY();

    COMMIT;

    SELECT
        cs.CaixaSessaoId,
        cs.EstabelecimentoId,
        cs.CaixaId,
        c.Nome AS Caixa,
        cs.UsuarioAberturaId,
        u.Nome AS UsuarioAbertura,
        cs.Status,
        cs.ValorAbertura,
        cs.AbertoEm
    FROM fin.CaixaSessao cs
    INNER JOIN fin.Caixa c
        ON c.CaixaId = cs.CaixaId
    INNER JOIN seg.Usuario u
        ON u.UsuarioId = cs.UsuarioAberturaId
    WHERE cs.CaixaSessaoId = @CaixaSessaoId;
END;
GO

/* ============================================================
   23.2 SANGRIA / SUPRIMENTO / AJUSTES
   ============================================================ */

CREATE OR ALTER PROCEDURE fin.sp_Caixa_Movimentar
(
    @EstabelecimentoId BIGINT,
    @CaixaSessaoId BIGINT,
    @TipoMovimento VARCHAR(30),
    @Valor DECIMAL(18,2),
    @UsuarioId BIGINT,
    @Descricao NVARCHAR(500) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Valor <= 0
        THROW 50110, 'O valor da movimentação deve ser maior que zero.', 1;

    IF @TipoMovimento NOT IN
       ('SUPRIMENTO','SANGRIA','AJUSTE_ENTRADA','AJUSTE_SAIDA')
        THROW 50111, 'Tipo de movimentação manual inválido.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM seg.Usuario
        WHERE UsuarioId = @UsuarioId
          AND Ativo = 1
    )
        THROW 50112, 'Usuário inválido ou inativo.', 1;

    BEGIN TRAN;

    DECLARE @ValorAbertura DECIMAL(18,2);

    SELECT @ValorAbertura = ValorAbertura
    FROM fin.CaixaSessao WITH (UPDLOCK, HOLDLOCK)
    WHERE CaixaSessaoId = @CaixaSessaoId
      AND EstabelecimentoId = @EstabelecimentoId
      AND Status = 'ABERTO';

    IF @ValorAbertura IS NULL
    BEGIN
        ROLLBACK;
        THROW 50113, 'Sessão de caixa inexistente ou fechada.', 1;
    END

    DECLARE @Natureza CHAR(1) =
        CASE
            WHEN @TipoMovimento IN ('SUPRIMENTO','AJUSTE_ENTRADA')
                THEN 'E'
            ELSE 'S'
        END;

    IF @Natureza = 'S'
    BEGIN
        DECLARE @SaldoFisico DECIMAL(18,2);

        SELECT
            @SaldoFisico =
                @ValorAbertura
                + ISNULL(SUM(CASE WHEN Natureza = 'E' THEN Valor ELSE 0 END), 0)
                - ISNULL(SUM(CASE WHEN Natureza = 'S' THEN Valor ELSE 0 END), 0)
        FROM fin.MovimentoCaixa
        WHERE CaixaSessaoId = @CaixaSessaoId;

        IF @Valor > @SaldoFisico
        BEGIN
            ROLLBACK;
            THROW 50114, 'Saldo físico insuficiente para esta saída de caixa.', 1;
        END
    END

    INSERT INTO fin.MovimentoCaixa
    (
        EstabelecimentoId,
        CaixaSessaoId,
        TipoMovimento,
        Natureza,
        Valor,
        Descricao,
        UsuarioId
    )
    VALUES
    (
        @EstabelecimentoId,
        @CaixaSessaoId,
        @TipoMovimento,
        @Natureza,
        @Valor,
        @Descricao,
        @UsuarioId
    );

    COMMIT;

    SELECT *
    FROM fin.MovimentoCaixa
    WHERE MovimentoCaixaId = SCOPE_IDENTITY();
END;
GO

/* ============================================================
   23.3 PAGAMENTO INTEGRADO AO CAIXA
   Substitui a versão anterior da procedure.
   ============================================================ */

CREATE OR ALTER PROCEDURE fin.sp_Pagamento_Registrar
(
    @EstabelecimentoId BIGINT,
    @PedidoId BIGINT,
    @FormaPagamentoId BIGINT,
    @Valor DECIMAL(18,2),
    @CriadoPorUsuarioId BIGINT,
    @ValorRecebido DECIMAL(18,2) = NULL,
    @ReferenciaTransacao NVARCHAR(150) = NULL,
    @CaixaSessaoId BIGINT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Valor <= 0
        THROW 50120, 'Valor do pagamento deve ser maior que zero.', 1;

    BEGIN TRAN;

    DECLARE
        @TotalPedido DECIMAL(18,2),
        @StatusOperacional VARCHAR(20);

    SELECT
        @TotalPedido = Total,
        @StatusOperacional = StatusOperacional
    FROM op.Pedido WITH (UPDLOCK, HOLDLOCK)
    WHERE PedidoId = @PedidoId
      AND EstabelecimentoId = @EstabelecimentoId;

    IF @TotalPedido IS NULL OR @StatusOperacional = 'CANCELADO'
    BEGIN
        ROLLBACK;
        THROW 50121, 'Pedido inválido ou cancelado.', 1;
    END

    DECLARE
        @PermiteTroco BIT,
        @TipoForma VARCHAR(20);

    SELECT
        @PermiteTroco = PermiteTroco,
        @TipoForma = Tipo
    FROM fin.FormaPagamento
    WHERE FormaPagamentoId = @FormaPagamentoId
      AND EstabelecimentoId = @EstabelecimentoId
      AND Ativo = 1;

    IF @TipoForma IS NULL
    BEGIN
        ROLLBACK;
        THROW 50122, 'Forma de pagamento inválida ou inativa.', 1;
    END

    IF @CaixaSessaoId IS NOT NULL
       AND NOT EXISTS
       (
            SELECT 1
            FROM fin.CaixaSessao
            WHERE CaixaSessaoId = @CaixaSessaoId
              AND EstabelecimentoId = @EstabelecimentoId
              AND Status = 'ABERTO'
       )
    BEGIN
        ROLLBACK;
        THROW 50123, 'Sessão de caixa inválida ou fechada.', 1;
    END

    /* Pagamento em dinheiro no PDV exige caixa aberto. */
    IF @TipoForma = 'DINHEIRO'
       AND @CaixaSessaoId IS NULL
    BEGIN
        ROLLBACK;
        THROW 50124, 'Pagamento em dinheiro exige uma sessão de caixa aberta.', 1;
    END

    DECLARE @JaPago DECIMAL(18,2);

    SELECT
        @JaPago = ISNULL(SUM(Valor), 0)
    FROM fin.Pagamento
    WHERE PedidoId = @PedidoId
      AND Status = 'CONFIRMADO';

    IF @Valor > (@TotalPedido - @JaPago)
    BEGIN
        ROLLBACK;
        THROW 50125, 'O valor informado excede o saldo restante do pedido.', 1;
    END

    DECLARE @Troco DECIMAL(18,2) = NULL;

    IF @ValorRecebido IS NOT NULL
    BEGIN
        IF @ValorRecebido < @Valor
        BEGIN
            ROLLBACK;
            THROW 50126, 'Valor recebido não pode ser menor que o pagamento.', 1;
        END

        IF @PermiteTroco = 0
           AND @ValorRecebido <> @Valor
        BEGIN
            ROLLBACK;
            THROW 50127, 'Esta forma de pagamento não permite troco.', 1;
        END

        SET @Troco = @ValorRecebido - @Valor;
    END

    INSERT INTO fin.Pagamento
    (
        EstabelecimentoId,
        PedidoId,
        FormaPagamentoId,
        CaixaSessaoId,
        Valor,
        ValorRecebido,
        Troco,
        Status,
        ReferenciaTransacao,
        CriadoPorUsuarioId
    )
    VALUES
    (
        @EstabelecimentoId,
        @PedidoId,
        @FormaPagamentoId,
        @CaixaSessaoId,
        @Valor,
        @ValorRecebido,
        @Troco,
        'CONFIRMADO',
        @ReferenciaTransacao,
        @CriadoPorUsuarioId
    );

    DECLARE @PagamentoId BIGINT = SCOPE_IDENTITY();

    /* Somente dinheiro altera o numerário físico do caixa.
       Cartão/PIX ficam vinculados à sessão para relatórios. */
    IF @TipoForma = 'DINHEIRO'
    BEGIN
        INSERT INTO fin.MovimentoCaixa
        (
            EstabelecimentoId,
            CaixaSessaoId,
            PagamentoId,
            TipoMovimento,
            Natureza,
            Valor,
            Descricao,
            UsuarioId
        )
        VALUES
        (
            @EstabelecimentoId,
            @CaixaSessaoId,
            @PagamentoId,
            'VENDA_DINHEIRO',
            'E',
            @Valor,
            N'Recebimento de venda em dinheiro',
            @CriadoPorUsuarioId
        );
    END

    COMMIT;

    SELECT *
    FROM fin.Pagamento
    WHERE PagamentoId = @PagamentoId;
END;
GO

/* ============================================================
   23.4 ESTORNO DE PAGAMENTO
   ============================================================ */

CREATE OR ALTER PROCEDURE fin.sp_Pagamento_Estornar
(
    @EstabelecimentoId BIGINT,
    @PagamentoId BIGINT,
    @UsuarioId BIGINT,
    @Motivo NVARCHAR(500)
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NULLIF(LTRIM(RTRIM(@Motivo)), N'') IS NULL
        THROW 50130, 'Informe o motivo do estorno.', 1;

    BEGIN TRAN;

    DECLARE
        @Status VARCHAR(20),
        @Valor DECIMAL(18,2),
        @CaixaSessaoId BIGINT,
        @TipoForma VARCHAR(20);

    SELECT
        @Status = pg.Status,
        @Valor = pg.Valor,
        @CaixaSessaoId = pg.CaixaSessaoId,
        @TipoForma = fp.Tipo
    FROM fin.Pagamento pg WITH (UPDLOCK, HOLDLOCK)
    INNER JOIN fin.FormaPagamento fp
        ON fp.FormaPagamentoId = pg.FormaPagamentoId
    WHERE pg.PagamentoId = @PagamentoId
      AND pg.EstabelecimentoId = @EstabelecimentoId;

    IF @Status IS NULL
    BEGIN
        ROLLBACK;
        THROW 50131, 'Pagamento não encontrado.', 1;
    END

    IF @Status <> 'CONFIRMADO'
    BEGIN
        ROLLBACK;
        THROW 50132, 'Somente pagamentos confirmados podem ser estornados.', 1;
    END

    IF @TipoForma = 'DINHEIRO'
    BEGIN
        IF @CaixaSessaoId IS NULL
        BEGIN
            ROLLBACK;
            THROW 50133, 'Pagamento em dinheiro sem sessão de caixa vinculada.', 1;
        END

        IF NOT EXISTS
        (
            SELECT 1
            FROM fin.CaixaSessao
            WHERE CaixaSessaoId = @CaixaSessaoId
              AND EstabelecimentoId = @EstabelecimentoId
              AND Status = 'ABERTO'
        )
        BEGIN
            ROLLBACK;
            THROW 50134, 'Pagamento em dinheiro só pode ser estornado enquanto o caixa original estiver aberto.', 1;
        END
    END

    UPDATE fin.Pagamento
       SET Status = 'ESTORNADO',
           AtualizadoEm = SYSUTCDATETIME()
    WHERE PagamentoId = @PagamentoId
      AND EstabelecimentoId = @EstabelecimentoId;

    IF @TipoForma = 'DINHEIRO'
    BEGIN
        INSERT INTO fin.MovimentoCaixa
        (
            EstabelecimentoId,
            CaixaSessaoId,
            PagamentoId,
            TipoMovimento,
            Natureza,
            Valor,
            Descricao,
            UsuarioId
        )
        VALUES
        (
            @EstabelecimentoId,
            @CaixaSessaoId,
            @PagamentoId,
            'ESTORNO_DINHEIRO',
            'S',
            @Valor,
            N'Estorno de pagamento em dinheiro: ' + @Motivo,
            @UsuarioId
        );
    END

    COMMIT;
END;
GO

/* ============================================================
   23.5 FECHAMENTO DE CAIXA
   ============================================================ */

CREATE OR ALTER PROCEDURE fin.sp_Caixa_Fechar
(
    @EstabelecimentoId BIGINT,
    @CaixaSessaoId BIGINT,
    @UsuarioId BIGINT,
    @ValorFechamentoInformado DECIMAL(18,2),
    @Observacao NVARCHAR(500) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ValorFechamentoInformado < 0
        THROW 50140, 'O valor informado no fechamento não pode ser negativo.', 1;

    BEGIN TRAN;

    DECLARE @ValorAbertura DECIMAL(18,2);

    SELECT @ValorAbertura = ValorAbertura
    FROM fin.CaixaSessao WITH (UPDLOCK, HOLDLOCK)
    WHERE CaixaSessaoId = @CaixaSessaoId
      AND EstabelecimentoId = @EstabelecimentoId
      AND Status = 'ABERTO';

    IF @ValorAbertura IS NULL
    BEGIN
        ROLLBACK;
        THROW 50141, 'Sessão de caixa inexistente ou já fechada.', 1;
    END

    DECLARE
        @TotalEntradas DECIMAL(18,2),
        @TotalSaidas DECIMAL(18,2),
        @ValorCalculado DECIMAL(18,2);

    SELECT
        @TotalEntradas =
            ISNULL(SUM(CASE WHEN Natureza = 'E' THEN Valor ELSE 0 END), 0),
        @TotalSaidas =
            ISNULL(SUM(CASE WHEN Natureza = 'S' THEN Valor ELSE 0 END), 0)
    FROM fin.MovimentoCaixa
    WHERE CaixaSessaoId = @CaixaSessaoId;

    SET @ValorCalculado =
        @ValorAbertura + @TotalEntradas - @TotalSaidas;

    UPDATE fin.CaixaSessao
       SET Status = 'FECHADO',
           UsuarioFechamentoId = @UsuarioId,
           FechadoEm = SYSUTCDATETIME(),
           ValorFechamentoCalculado = @ValorCalculado,
           ValorFechamentoInformado = @ValorFechamentoInformado,
           ObservacaoFechamento = @Observacao
    WHERE CaixaSessaoId = @CaixaSessaoId
      AND EstabelecimentoId = @EstabelecimentoId;

    COMMIT;

    SELECT
        CaixaSessaoId,
        Status,
        ValorAbertura,
        ValorFechamentoCalculado,
        ValorFechamentoInformado,
        Diferenca,
        AbertoEm,
        FechadoEm
    FROM fin.CaixaSessao
    WHERE CaixaSessaoId = @CaixaSessaoId;
END;
GO

/* ============================================================
   23.6 VIEW DE RESUMO DO CAIXA
   ============================================================ */

CREATE OR ALTER VIEW rel.vw_CaixaResumo
AS
SELECT
    cs.CaixaSessaoId,
    cs.EstabelecimentoId,
    cs.CaixaId,
    c.Codigo AS CodigoCaixa,
    c.Nome AS Caixa,

    cs.UsuarioAberturaId,
    ua.Nome AS UsuarioAbertura,

    cs.UsuarioFechamentoId,
    uf.Nome AS UsuarioFechamento,

    cs.Status,
    cs.AbertoEm,
    cs.FechadoEm,

    cs.ValorAbertura,

    ISNULL(m.EntradasFisicas, 0) AS EntradasFisicas,
    ISNULL(m.SaidasFisicas, 0) AS SaidasFisicas,

    CONVERT
    (
        DECIMAL(18,2),
        cs.ValorAbertura
        + ISNULL(m.EntradasFisicas, 0)
        - ISNULL(m.SaidasFisicas, 0)
    ) AS SaldoFisicoEsperado,

    ISNULL(p.Dinheiro, 0) AS PagamentosDinheiro,
    ISNULL(p.Credito, 0) AS PagamentosCredito,
    ISNULL(p.Debito, 0) AS PagamentosDebito,
    ISNULL(p.Pix, 0) AS PagamentosPix,
    ISNULL(p.Outros, 0) AS PagamentosOutros,
    ISNULL(p.TotalPagamentos, 0) AS TotalPagamentos,

    ISNULL(m.Suprimentos, 0) AS Suprimentos,
    ISNULL(m.Sangrias, 0) AS Sangrias,
    ISNULL(m.AjustesEntrada, 0) AS AjustesEntrada,
    ISNULL(m.AjustesSaida, 0) AS AjustesSaida,
    ISNULL(m.EstornosDinheiro, 0) AS EstornosDinheiro,

    cs.ValorFechamentoCalculado,
    cs.ValorFechamentoInformado,
    cs.Diferenca,

    cs.ObservacaoAbertura,
    cs.ObservacaoFechamento

FROM fin.CaixaSessao cs

INNER JOIN fin.Caixa c
    ON c.CaixaId = cs.CaixaId

INNER JOIN seg.Usuario ua
    ON ua.UsuarioId = cs.UsuarioAberturaId

LEFT JOIN seg.Usuario uf
    ON uf.UsuarioId = cs.UsuarioFechamentoId

OUTER APPLY
(
    SELECT
        SUM(CASE WHEN mc.Natureza = 'E' THEN mc.Valor ELSE 0 END) AS EntradasFisicas,
        SUM(CASE WHEN mc.Natureza = 'S' THEN mc.Valor ELSE 0 END) AS SaidasFisicas,
        SUM(CASE WHEN mc.TipoMovimento = 'SUPRIMENTO' THEN mc.Valor ELSE 0 END) AS Suprimentos,
        SUM(CASE WHEN mc.TipoMovimento = 'SANGRIA' THEN mc.Valor ELSE 0 END) AS Sangrias,
        SUM(CASE WHEN mc.TipoMovimento = 'AJUSTE_ENTRADA' THEN mc.Valor ELSE 0 END) AS AjustesEntrada,
        SUM(CASE WHEN mc.TipoMovimento = 'AJUSTE_SAIDA' THEN mc.Valor ELSE 0 END) AS AjustesSaida,
        SUM(CASE WHEN mc.TipoMovimento = 'ESTORNO_DINHEIRO' THEN mc.Valor ELSE 0 END) AS EstornosDinheiro
    FROM fin.MovimentoCaixa mc
    WHERE mc.CaixaSessaoId = cs.CaixaSessaoId
) m

OUTER APPLY
(
    SELECT
        SUM(CASE WHEN fp.Tipo = 'DINHEIRO' THEN pg.Valor ELSE 0 END) AS Dinheiro,
        SUM(CASE WHEN fp.Tipo = 'CREDITO' THEN pg.Valor ELSE 0 END) AS Credito,
        SUM(CASE WHEN fp.Tipo = 'DEBITO' THEN pg.Valor ELSE 0 END) AS Debito,
        SUM(CASE WHEN fp.Tipo = 'PIX' THEN pg.Valor ELSE 0 END) AS Pix,
        SUM(CASE WHEN fp.Tipo = 'OUTRO' THEN pg.Valor ELSE 0 END) AS Outros,
        SUM(pg.Valor) AS TotalPagamentos
    FROM fin.Pagamento pg
    INNER JOIN fin.FormaPagamento fp
        ON fp.FormaPagamentoId = pg.FormaPagamentoId
    WHERE pg.CaixaSessaoId = cs.CaixaSessaoId
      AND pg.Status = 'CONFIRMADO'
) p;
GO

/* ============================================================
   23.7 RELATÓRIO DETALHADO DE CAIXA
   Retorna:
   1º result set: resumo das sessões
   2º result set: movimentações físicas
   3º result set: pagamentos por forma
   ============================================================ */

CREATE OR ALTER PROCEDURE rel.sp_Caixa_RelatorioDetalhado
(
    @EstabelecimentoId BIGINT,
    @DataInicial DATE,
    @DataFinal DATE,
    @CaixaId BIGINT = NULL,
    @UsuarioId BIGINT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @DataFinal < @DataInicial
        THROW 50150, 'A data final não pode ser menor que a data inicial.', 1;

    /* Resumo */
    SELECT *
    FROM rel.vw_CaixaResumo r
    WHERE r.EstabelecimentoId = @EstabelecimentoId
      AND r.AbertoEm >= @DataInicial
      AND r.AbertoEm < DATEADD(DAY, 1, @DataFinal)
      AND (@CaixaId IS NULL OR r.CaixaId = @CaixaId)
      AND (@UsuarioId IS NULL OR r.UsuarioAberturaId = @UsuarioId)
    ORDER BY r.AbertoEm DESC;

    /* Movimentações físicas */
    SELECT
        mc.MovimentoCaixaId,
        mc.CaixaSessaoId,
        c.Codigo AS CodigoCaixa,
        c.Nome AS Caixa,
        mc.TipoMovimento,
        mc.Natureza,
        mc.Valor,
        mc.Descricao,
        mc.PagamentoId,
        mc.UsuarioId,
        u.Nome AS Usuario,
        mc.CriadoEm
    FROM fin.MovimentoCaixa mc
    INNER JOIN fin.CaixaSessao cs
        ON cs.CaixaSessaoId = mc.CaixaSessaoId
    INNER JOIN fin.Caixa c
        ON c.CaixaId = cs.CaixaId
    INNER JOIN seg.Usuario u
        ON u.UsuarioId = mc.UsuarioId
    WHERE mc.EstabelecimentoId = @EstabelecimentoId
      AND cs.AbertoEm >= @DataInicial
      AND cs.AbertoEm < DATEADD(DAY, 1, @DataFinal)
      AND (@CaixaId IS NULL OR cs.CaixaId = @CaixaId)
      AND (@UsuarioId IS NULL OR cs.UsuarioAberturaId = @UsuarioId)
    ORDER BY mc.CriadoEm DESC;

    /* Pagamentos agrupados por sessão e forma */
    SELECT
        pg.CaixaSessaoId,
        c.Codigo AS CodigoCaixa,
        c.Nome AS Caixa,
        fp.Tipo,
        fp.Nome AS FormaPagamento,
        COUNT_BIG(*) AS QuantidadePagamentos,
        SUM(pg.Valor) AS ValorTotal
    FROM fin.Pagamento pg
    INNER JOIN fin.FormaPagamento fp
        ON fp.FormaPagamentoId = pg.FormaPagamentoId
    INNER JOIN fin.CaixaSessao cs
        ON cs.CaixaSessaoId = pg.CaixaSessaoId
    INNER JOIN fin.Caixa c
        ON c.CaixaId = cs.CaixaId
    WHERE pg.EstabelecimentoId = @EstabelecimentoId
      AND pg.Status = 'CONFIRMADO'
      AND cs.AbertoEm >= @DataInicial
      AND cs.AbertoEm < DATEADD(DAY, 1, @DataFinal)
      AND (@CaixaId IS NULL OR cs.CaixaId = @CaixaId)
      AND (@UsuarioId IS NULL OR cs.UsuarioAberturaId = @UsuarioId)
    GROUP BY
        pg.CaixaSessaoId,
        c.Codigo,
        c.Nome,
        fp.Tipo,
        fp.Nome
    ORDER BY
        pg.CaixaSessaoId,
        fp.Nome;
END;
GO

/* ============================================================
   23.8 PERMISSÕES DO MÓDULO DE CAIXA
   ============================================================ */

INSERT INTO seg.Permissao
(
    Chave,
    Modulo,
    Descricao
)
SELECT
    v.Chave,
    N'Caixa',
    v.Descricao
FROM
(
    VALUES
        (N'CAIXA.VISUALIZAR', N'Visualizar caixas e sessões'),
        (N'CAIXA.ABRIR', N'Abrir sessão de caixa'),
        (N'CAIXA.MOVIMENTAR', N'Realizar sangria, suprimento e ajustes'),
        (N'CAIXA.FECHAR', N'Fechar e conferir caixa'),
        (N'CAIXA.RELATORIO', N'Visualizar relatórios detalhados de caixa')
) v(Chave, Descricao)
WHERE NOT EXISTS
(
    SELECT 1
    FROM seg.Permissao p
    WHERE p.Chave = v.Chave
);
GO

/* Administradores recebem também as novas permissões de caixa. */
INSERT INTO seg.PerfilPermissao
(
    PerfilId,
    PermissaoId
)
SELECT
    pf.PerfilId,
    pe.PermissaoId
FROM seg.Perfil pf
INNER JOIN seg.Permissao pe
    ON pe.Modulo = N'Caixa'
WHERE pf.Nome IN
(
    N'Administrador Geral',
    N'Administrador do Estabelecimento'
)
AND NOT EXISTS
(
    SELECT 1
    FROM seg.PerfilPermissao pp
    WHERE pp.PerfilId = pf.PerfilId
      AND pp.PermissaoId = pe.PermissaoId
);
GO

/* ============================================================
   23.9 EXEMPLOS DE USO DO CAIXA
   (comentados para não inserir dados automaticamente)
   ============================================================ */

/*
-- 1. Cadastrar caixa:
INSERT INTO fin.Caixa
(
    EstabelecimentoId,
    Codigo,
    Nome
)
VALUES
(
    1,
    'CX01',
    'Caixa Principal'
);

-- 2. Abrir caixa:
EXEC fin.sp_Caixa_Abrir
    @EstabelecimentoId = 1,
    @CaixaId = 1,
    @UsuarioId = 1,
    @ValorAbertura = 100.00,
    @Observacao = N'Abertura do turno';

-- 3. Suprimento:
EXEC fin.sp_Caixa_Movimentar
    @EstabelecimentoId = 1,
    @CaixaSessaoId = 1,
    @TipoMovimento = 'SUPRIMENTO',
    @Valor = 50.00,
    @UsuarioId = 1,
    @Descricao = N'Reforço de troco';

-- 4. Sangria:
EXEC fin.sp_Caixa_Movimentar
    @EstabelecimentoId = 1,
    @CaixaSessaoId = 1,
    @TipoMovimento = 'SANGRIA',
    @Valor = 80.00,
    @UsuarioId = 1,
    @Descricao = N'Retirada para cofre';

-- 5. Pagamento vinculado ao caixa:
EXEC fin.sp_Pagamento_Registrar
    @EstabelecimentoId = 1,
    @PedidoId = 1,
    @FormaPagamentoId = 1,
    @Valor = 35.00,
    @CriadoPorUsuarioId = 1,
    @ValorRecebido = 50.00,
    @CaixaSessaoId = 1;

-- 6. Fechar caixa:
EXEC fin.sp_Caixa_Fechar
    @EstabelecimentoId = 1,
    @CaixaSessaoId = 1,
    @UsuarioId = 1,
    @ValorFechamentoInformado = 105.00,
    @Observacao = N'Fechamento do turno';

-- 7. Relatório:
EXEC rel.sp_Caixa_RelatorioDetalhado
    @EstabelecimentoId = 1,
    @DataInicial = '2026-10-01',
    @DataFinal = '2026-10-31';
*/
GO

/* ============================================================
   24. VALIDAÇÃO FINAL DA INSTALAÇÃO
   ============================================================ */

SELECT
    DB_NAME() AS BancoAtual,
    COUNT(*) AS QuantidadeTabelas
FROM sys.tables;
GO

SELECT
    s.name AS SchemaNome,
    COUNT(*) AS QuantidadeTabelas
FROM sys.tables t
INNER JOIN sys.schemas s
    ON s.schema_id = t.schema_id
GROUP BY s.name
ORDER BY s.name;
GO

SELECT
    name AS ProcedureNome,
    SCHEMA_NAME(schema_id) AS SchemaNome
FROM sys.procedures
ORDER BY SchemaNome, ProcedureNome;
GO

SELECT
    name AS TriggerNome,
    OBJECT_SCHEMA_NAME(parent_id) AS SchemaTabela,
    OBJECT_NAME(parent_id) AS Tabela
FROM sys.triggers
WHERE parent_class_desc = 'OBJECT_OR_COLUMN'
ORDER BY SchemaTabela, Tabela, TriggerNome;
GO

PRINT 'NaxapaDB v2 com módulo de Caixa criado com sucesso.';
GO


/* ================= PATCH V3 ================= */
/* ============================================================
   NAXAPA - PATCH v3
   Segurança, estabelecimento, insumos/receitas e impressão
   Idempotente para bancos v2 existentes.
   ============================================================ */
USE NaxapaDB;
GO

IF OBJECT_ID(N'cfg.SchemaVersion', N'U') IS NULL
BEGIN
    CREATE TABLE cfg.SchemaVersion
    (
        Versao INT NOT NULL CONSTRAINT PK_SchemaVersion PRIMARY KEY,
        Descricao NVARCHAR(250) NOT NULL,
        AplicadoEm DATETIME2(0) NOT NULL CONSTRAINT DF_SchemaVersion_AplicadoEm DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF COL_LENGTH('cad.Estabelecimento', 'Codigo') IS NULL
BEGIN
    ALTER TABLE cad.Estabelecimento ADD Codigo NVARCHAR(30) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'cad.Estabelecimento') AND name = N'UX_Estabelecimento_Codigo')
BEGIN
    CREATE UNIQUE INDEX UX_Estabelecimento_Codigo
        ON cad.Estabelecimento (Codigo)
        WHERE Codigo IS NOT NULL;
END
GO

IF COL_LENGTH('seg.RefreshToken', 'EstabelecimentoId') IS NULL
BEGIN
    ALTER TABLE seg.RefreshToken ADD EstabelecimentoId BIGINT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RefreshToken_Estabelecimento')
BEGIN
    ALTER TABLE seg.RefreshToken
        ADD CONSTRAINT FK_RefreshToken_Estabelecimento
        FOREIGN KEY (EstabelecimentoId)
        REFERENCES cad.Estabelecimento (EstabelecimentoId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'seg.RefreshToken') AND name = N'IX_RefreshToken_Estabelecimento')
BEGIN
    CREATE INDEX IX_RefreshToken_Estabelecimento
        ON seg.RefreshToken (EstabelecimentoId, ExpiraEm)
        INCLUDE (UsuarioId, RevogadoEm);
END
GO

IF COL_LENGTH('cfg.Impressora', 'Padrao') IS NULL
BEGIN
    ALTER TABLE cfg.Impressora
        ADD Padrao BIT NOT NULL
            CONSTRAINT DF_Impressora_Padrao DEFAULT (0);
END
GO

IF OBJECT_ID(N'cad.Insumo', N'U') IS NULL
BEGIN
    CREATE TABLE cad.Insumo
    (
        InsumoId BIGINT IDENTITY(1,1) NOT NULL,
        EstabelecimentoId BIGINT NOT NULL,
        CodigoInterno NVARCHAR(30) NULL,
        Nome NVARCHAR(150) NOT NULL,
        UnidadeMedida NVARCHAR(10) NOT NULL CONSTRAINT DF_Insumo_Unidade DEFAULT ('UN'),
        CustoMedio DECIMAL(18,4) NULL,
        Ativo BIT NOT NULL CONSTRAINT DF_Insumo_Ativo DEFAULT (1),
        CriadoEm DATETIME2(0) NOT NULL CONSTRAINT DF_Insumo_CriadoEm DEFAULT (SYSUTCDATETIME()),
        AtualizadoEm DATETIME2(0) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT PK_Insumo PRIMARY KEY (InsumoId),
        CONSTRAINT UQ_Insumo_IdEst UNIQUE (InsumoId, EstabelecimentoId),
        CONSTRAINT FK_Insumo_Estabelecimento FOREIGN KEY (EstabelecimentoId)
            REFERENCES cad.Estabelecimento (EstabelecimentoId),
        CONSTRAINT CK_Insumo_Custo CHECK (CustoMedio IS NULL OR CustoMedio >= 0)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'cad.Insumo') AND name = N'UX_Insumo_Codigo')
BEGIN
    CREATE UNIQUE INDEX UX_Insumo_Codigo
        ON cad.Insumo (EstabelecimentoId, CodigoInterno)
        WHERE CodigoInterno IS NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'cad.Insumo') AND name = N'IX_Insumo_Nome')
BEGIN
    CREATE INDEX IX_Insumo_Nome
        ON cad.Insumo (EstabelecimentoId, Nome)
        INCLUDE (UnidadeMedida, CustoMedio, Ativo);
END
GO

IF OBJECT_ID(N'cad.Receita', N'U') IS NULL
BEGIN
    CREATE TABLE cad.Receita
    (
        ReceitaId BIGINT IDENTITY(1,1) NOT NULL,
        EstabelecimentoId BIGINT NOT NULL,
        ProdutoId BIGINT NOT NULL,
        Rendimento DECIMAL(18,3) NOT NULL CONSTRAINT DF_Receita_Rendimento DEFAULT (1),
        Observacao NVARCHAR(500) NULL,
        Ativo BIT NOT NULL CONSTRAINT DF_Receita_Ativo DEFAULT (1),
        CriadoEm DATETIME2(0) NOT NULL CONSTRAINT DF_Receita_CriadoEm DEFAULT (SYSUTCDATETIME()),
        AtualizadoEm DATETIME2(0) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT PK_Receita PRIMARY KEY (ReceitaId),
        CONSTRAINT UQ_Receita_IdEst UNIQUE (ReceitaId, EstabelecimentoId),
        CONSTRAINT UQ_Receita_Produto UNIQUE (EstabelecimentoId, ProdutoId),
        CONSTRAINT FK_Receita_Produto FOREIGN KEY (ProdutoId, EstabelecimentoId)
            REFERENCES cad.Produto (ProdutoId, EstabelecimentoId),
        CONSTRAINT CK_Receita_Rendimento CHECK (Rendimento > 0)
    );
END
GO

IF OBJECT_ID(N'cad.ReceitaItem', N'U') IS NULL
BEGIN
    CREATE TABLE cad.ReceitaItem
    (
        ReceitaItemId BIGINT IDENTITY(1,1) NOT NULL,
        EstabelecimentoId BIGINT NOT NULL,
        ReceitaId BIGINT NOT NULL,
        InsumoId BIGINT NOT NULL,
        Quantidade DECIMAL(18,4) NOT NULL,
        Observacao NVARCHAR(300) NULL,
        CONSTRAINT PK_ReceitaItem PRIMARY KEY (ReceitaItemId),
        CONSTRAINT UQ_ReceitaItem UNIQUE (ReceitaId, InsumoId),
        CONSTRAINT FK_ReceitaItem_Receita FOREIGN KEY (ReceitaId, EstabelecimentoId)
            REFERENCES cad.Receita (ReceitaId, EstabelecimentoId),
        CONSTRAINT FK_ReceitaItem_Insumo FOREIGN KEY (InsumoId, EstabelecimentoId)
            REFERENCES cad.Insumo (InsumoId, EstabelecimentoId),
        CONSTRAINT CK_ReceitaItem_Quantidade CHECK (Quantidade > 0)
    );
END
GO

INSERT INTO seg.Permissao (Chave, Modulo, Descricao)
SELECT v.Chave, v.Modulo, v.Descricao
FROM (VALUES
    (N'INSUMO.VISUALIZAR', N'Insumos', N'Visualizar insumos'),
    (N'INSUMO.EDITAR', N'Insumos', N'Criar e editar insumos'),
    (N'RECEITA.VISUALIZAR', N'Receitas', N'Visualizar fichas técnicas'),
    (N'RECEITA.EDITAR', N'Receitas', N'Criar e editar fichas técnicas'),
    (N'CAIXA.CADASTRAR', N'Caixa', N'Cadastrar e editar caixas')
) v(Chave, Modulo, Descricao)
WHERE NOT EXISTS (SELECT 1 FROM seg.Permissao p WHERE p.Chave = v.Chave);
GO

INSERT INTO seg.PerfilPermissao (PerfilId, PermissaoId)
SELECT pf.PerfilId, pe.PermissaoId
FROM seg.Perfil pf
CROSS JOIN seg.Permissao pe
WHERE pf.Nome IN (N'Administrador Geral', N'Administrador do Estabelecimento')
  AND NOT EXISTS
  (
      SELECT 1 FROM seg.PerfilPermissao pp
      WHERE pp.PerfilId = pf.PerfilId AND pp.PermissaoId = pe.PermissaoId
  );
GO

IF NOT EXISTS (SELECT 1 FROM cfg.SchemaVersion WHERE Versao = 3)
BEGIN
    INSERT INTO cfg.SchemaVersion (Versao, Descricao)
    VALUES (3, N'Autenticação/API, código de estabelecimento, insumos/receitas e impressora padrão');
END
GO

/* Perfis operacionais: permissões mínimas por função. */
DECLARE @RolePermissions TABLE (Perfil NVARCHAR(100), Chave NVARCHAR(150));
INSERT INTO @RolePermissions (Perfil, Chave) VALUES
(N'Gerente', N'CLIENTE.VISUALIZAR'), (N'Gerente', N'CLIENTE.CRIAR'), (N'Gerente', N'CLIENTE.EDITAR'),
(N'Gerente', N'PRODUTO.VISUALIZAR'), (N'Gerente', N'PRODUTO.CRIAR'), (N'Gerente', N'PRODUTO.EDITAR'),
(N'Gerente', N'CATEGORIA.VISUALIZAR'), (N'Gerente', N'CATEGORIA.EDITAR'),
(N'Gerente', N'CARDAPIO.VISUALIZAR'), (N'Gerente', N'CARDAPIO.EDITAR'),
(N'Gerente', N'SETOR.VISUALIZAR'), (N'Gerente', N'SETOR.EDITAR'),
(N'Gerente', N'IMPRESSORA.VISUALIZAR'), (N'Gerente', N'IMPRESSORA.EDITAR'), (N'Gerente', N'PEDIDO.VISUALIZAR'), (N'Gerente', N'PEDIDO.EDITAR'),
(N'Gerente', N'PEDIDO.CANCELAR'), (N'Gerente', N'PEDIDO.FINALIZAR'), (N'Gerente', N'PAGAMENTO.REGISTRAR'), (N'Gerente', N'PAGAMENTO.ESTORNAR'),
(N'Gerente', N'CAIXA.VISUALIZAR'), (N'Gerente', N'CAIXA.ABRIR'), (N'Gerente', N'CAIXA.MOVIMENTAR'),
(N'Gerente', N'CAIXA.FECHAR'), (N'Gerente', N'CAIXA.RELATORIO'),
(N'Gerente', N'ESTOQUE.VISUALIZAR'), (N'Gerente', N'ESTOQUE.MOVIMENTAR'),
(N'Gerente', N'RELATORIO.VISUALIZAR'), (N'Gerente', N'INSUMO.VISUALIZAR'), (N'Gerente', N'INSUMO.EDITAR'),
(N'Gerente', N'RECEITA.VISUALIZAR'), (N'Gerente', N'RECEITA.EDITAR'),

(N'Atendente', N'CLIENTE.VISUALIZAR'), (N'Atendente', N'CLIENTE.CRIAR'), (N'Atendente', N'CLIENTE.EDITAR'),
(N'Atendente', N'PRODUTO.VISUALIZAR'), (N'Atendente', N'CARDAPIO.VISUALIZAR'),
(N'Atendente', N'PEDIDO.VISUALIZAR'), (N'Atendente', N'PEDIDO.CRIAR'), (N'Atendente', N'PEDIDO.EDITAR'),

(N'Operador de Caixa', N'CLIENTE.VISUALIZAR'), (N'Operador de Caixa', N'CLIENTE.CRIAR'),
(N'Operador de Caixa', N'PRODUTO.VISUALIZAR'), (N'Operador de Caixa', N'CARDAPIO.VISUALIZAR'),
(N'Operador de Caixa', N'PEDIDO.VISUALIZAR'), (N'Operador de Caixa', N'PEDIDO.CRIAR'),
(N'Operador de Caixa', N'PEDIDO.EDITAR'), (N'Operador de Caixa', N'PEDIDO.FINALIZAR'),
(N'Operador de Caixa', N'PAGAMENTO.REGISTRAR'), (N'Operador de Caixa', N'CAIXA.VISUALIZAR'),
(N'Operador de Caixa', N'CAIXA.ABRIR'), (N'Operador de Caixa', N'CAIXA.MOVIMENTAR'),
(N'Operador de Caixa', N'CAIXA.FECHAR'),

(N'Produção', N'PEDIDO.VISUALIZAR'), (N'Produção', N'PEDIDO.EDITAR'), (N'Produção', N'PRODUTO.VISUALIZAR'),
(N'Produção', N'CARDAPIO.VISUALIZAR');

INSERT INTO seg.PerfilPermissao (PerfilId, PermissaoId)
SELECT pf.PerfilId, pe.PermissaoId
FROM @RolePermissions rp
JOIN seg.Perfil pf ON pf.Nome = rp.Perfil AND pf.Ativo = 1
JOIN seg.Permissao pe ON pe.Chave = rp.Chave AND pe.Ativo = 1
WHERE NOT EXISTS
(
    SELECT 1 FROM seg.PerfilPermissao pp
    WHERE pp.PerfilId = pf.PerfilId AND pp.PermissaoId = pe.PermissaoId
);
GO

/* Correção v3: adicionais são configurados por unidade do item.
   ValorOpcoes da linha deve refletir opções x quantidade do produto. */
CREATE OR ALTER TRIGGER op.trg_PedidoItemOpcao_Recalcular
ON op.PedidoItemOpcao
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH ItensAfetados AS
    (
        SELECT PedidoItemId FROM inserted
        UNION
        SELECT PedidoItemId FROM deleted
    )
    UPDATE pi
       SET ValorOpcoes = CONVERT(DECIMAL(18,2), ISNULL(x.TotalOpcoesUnitarias, 0) * pi.Quantidade)
    FROM op.PedidoItem pi
    INNER JOIN ItensAfetados a
        ON a.PedidoItemId = pi.PedidoItemId
    OUTER APPLY
    (
        SELECT SUM(pio.TotalOpcao) AS TotalOpcoesUnitarias
        FROM op.PedidoItemOpcao pio
        WHERE pio.PedidoItemId = pi.PedidoItemId
    ) x;
END;
GO
