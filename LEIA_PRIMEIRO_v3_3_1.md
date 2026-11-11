# NAXAPA v3.3.1 — LocalDB + referência de banco + correção de runtime

Esta versão foi revisada usando o projeto de referência enviado pelo usuário para comparar a configuração do Entity Framework Core e SQL Server.

## Banco configurado

`Server=(localdb)\\MSSQLLocalDB;Database=NaxapaDB;Trusted_Connection=True`

## Correção principal

Foi removido o `EnableRetryOnFailure()` da configuração padrão do EF Core. A versão anterior utilizava retry automático ao mesmo tempo que diversas operações utilizavam `BeginTransactionAsync()`, causando:

`SqlServerRetryingExecutionStrategy does not support user-initiated transactions`

A conexão agora segue `UseSqlServer(...)` direto, adequada ao LocalDB e às transações manuais do NAXAPA.

## Primeiro acesso

1. Rode a aplicação.
2. Abra `http://localhost:5180/Setup` se ainda não houver usuário.
3. Ou execute em outro PowerShell:

`powershell -ExecutionPolicy Bypass -File .\\scripts\\CriarAcessoETestar.ps1`

O script cria o primeiro administrador somente quando `seg.Usuario` está vazio, gera uma senha forte aleatória e testa as principais rotas.

## Teste de integração isolado

`dotnet test .\\Naxapa.Web.Tests\\Naxapa.Web.Tests.csproj -c Debug --filter SetupLoginIntegrationTests`

No Windows, esse teste usa um banco temporário `NaxapaDB_Test_*` no LocalDB, cria um administrador, testa login e páginas principais e remove o banco ao final.

## Validação geral

`powershell -ExecutionPolicy Bypass -File .\\scripts\\Validar-NAXAPA.ps1`
