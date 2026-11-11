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
