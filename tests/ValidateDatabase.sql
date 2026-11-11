USE NaxapaDB;
GO
SET NOCOUNT ON;
DECLARE @Errors TABLE(Message NVARCHAR(500));
DECLARE @Required TABLE(SchemaName SYSNAME,ObjectName SYSNAME,ObjectType CHAR(2));
INSERT INTO @Required VALUES
('seg','Usuario','U'),('cad','Estabelecimento','U'),('cad','Produto','U'),('cad','Cliente','U'),
('op','Pedido','U'),('op','PedidoItem','U'),('op','Mesa','U'),('op','Comanda','U'),
('fin','Pagamento','U'),('fin','Caixa','U'),('fin','CaixaSessao','U'),('fin','MovimentoCaixa','U'),
('imp','TrabalhoImpressao','U'),('cad','Insumo','U'),('cad','Receita','U'),('cfg','SchemaVersion','U');
INSERT INTO @Errors
SELECT CONCAT('Objeto ausente: ',SchemaName,'.',ObjectName)
FROM @Required r WHERE OBJECT_ID(QUOTENAME(r.SchemaName)+'.'+QUOTENAME(r.ObjectName),r.ObjectType) IS NULL;
IF OBJECT_ID('op.sp_Pedido_Criar','P') IS NULL INSERT INTO @Errors VALUES('Procedure ausente: op.sp_Pedido_Criar');
IF OBJECT_ID('op.sp_Pedido_AlterarStatus','P') IS NULL INSERT INTO @Errors VALUES('Procedure ausente: op.sp_Pedido_AlterarStatus');
IF OBJECT_ID('fin.sp_Caixa_Abrir','P') IS NULL INSERT INTO @Errors VALUES('Procedure ausente: fin.sp_Caixa_Abrir');
IF OBJECT_ID('fin.sp_Caixa_Fechar','P') IS NULL INSERT INTO @Errors VALUES('Procedure ausente: fin.sp_Caixa_Fechar');
IF OBJECT_ID('imp.sp_TrabalhoImpressao_Enfileirar','P') IS NULL INSERT INTO @Errors VALUES('Procedure ausente: imp.sp_TrabalhoImpressao_Enfileirar');
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('fin.CaixaSessao') AND name='UX_CaixaSessao_Caixa_Aberto') INSERT INTO @Errors VALUES('Índice de sessão única de caixa ausente.');
IF EXISTS(SELECT 1 FROM @Errors) BEGIN SELECT * FROM @Errors; THROW 51000,'Validação do NaxapaDB falhou.',1; END;
SELECT 'OK' AS Status, DB_NAME() AS DatabaseName, (SELECT MAX(VersionNumber) FROM cfg.SchemaVersion) AS SchemaVersion;
GO
