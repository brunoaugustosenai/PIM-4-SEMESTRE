# NAXAPA v3.2.2 — Correção Razor IReadOnlyList<T>

## Problema corrigido
A v3.2.1 continha 14 views com a diretiva Razor genérica quebrada em duas linhas:

```cshtml
@model IReadOnlyList
<ProductRow>
```

O compilador interpretava `IReadOnlyList` sem argumento de tipo e gerava CS0305 / erros relacionados. Como cada view podia gerar mais de um diagnóstico, o build mostrava 28 erros.

## Correção
As 14 views foram alteradas para manter o tipo genérico completo na mesma diretiva:

```cshtml
@model IReadOnlyList<ProductRow>
```

Arquivos corrigidos:
- Views/Receitas/Index.cshtml
- Views/Receitas/Items.cshtml
- Views/Clientes/Index.cshtml
- Views/Clientes/History.cshtml
- Views/Estabelecimentos/Index.cshtml
- Views/Categorias/Index.cshtml
- Views/Insumos/Index.cshtml
- Views/Opcoes/Index.cshtml
- Views/Setores/Index.cshtml
- Views/Usuarios/Index.cshtml
- Views/Cardapio/Index.cshtml
- Views/Impressoras/Index.cshtml
- Views/Impressoras/History.cshtml
- Views/Produtos/Index.cshtml

A configuração LocalDB, /Setup e as correções anteriores da v3.2.1 foram preservadas.
