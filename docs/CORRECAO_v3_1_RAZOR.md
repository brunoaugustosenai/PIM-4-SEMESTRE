# NAXAPA v3.1 — Correção Razor CS1963

Correção aplicada em 09/11/2026 para falha de compilação em partial views Razor.

## Causa
Oito arquivos `_Form.cshtml` tinham a diretiva `@model` concatenada ao primeiro elemento HTML, por exemplo:

`@model MenuFormViewModel<div ...>`

Isso fazia o Razor gerar código incorreto e provocava muitos erros `CS1963` em cascata.

## Arquivos corrigidos
- Views/Cardapio/_Form.cshtml
- Views/Impressoras/_Form.cshtml
- Views/Insumos/_Form.cshtml
- Views/Opcoes/_GroupForm.cshtml
- Views/Opcoes/_OptionForm.cshtml
- Views/Receitas/_Form.cshtml
- Views/Setores/_Form.cshtml
- Views/Usuarios/_Form.cshtml

## Prevenção
Foi adicionado um teste estático de regressão que falha se uma diretiva `@model` voltar a ficar concatenada ao HTML.
O launcher v3.1 também remove `bin` e `obj` antes de restaurar/compilar, eliminando artefatos Razor antigos.
