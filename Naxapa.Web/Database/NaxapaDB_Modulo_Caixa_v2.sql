/* ============================================================
   NAXAPA - PATCH v2 - MÓDULO DE CAIXA
   Execute SOMENTE se o NaxapaDB v1 já estiver criado.
   ============================================================ */

USE NaxapaDB;
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
