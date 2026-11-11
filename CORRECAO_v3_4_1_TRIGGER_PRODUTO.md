# NAXAPA v3.4.1 — correção de gravação de Produto com trigger

## Erro corrigido

Ao editar um produto (inclusive ao trocar a imagem), o SQL Server retornava erro informando que `cad.Produto` possui trigger habilitado e não aceita a cláusula `OUTPUT` utilizada automaticamente pelo Entity Framework Core.

## Causa

A tabela `cad.Produto` possui o trigger `cad.trg_Produto_AtualizarData` (`AFTER UPDATE`). O EF Core 10 usa `OUTPUT` em operações de gravação por padrão. SQL Server não permite esse formato quando a tabela possui trigger habilitado.

## Correção aplicada

Somente o mapeamento da entidade `ProdutoEntity` foi alterado:

```csharp
e.ToTable("Produto", "cad", tb => tb.UseSqlOutputClause(false));
```

Isso faz o EF Core usar a estratégia compatível com triggers apenas em `cad.Produto`.

Nenhuma tabela, trigger, controller, autenticação, conexão, caixa, pedido ou script de banco foi alterado.
