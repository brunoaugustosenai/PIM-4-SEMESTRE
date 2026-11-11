# NAXAPA v3.3 — correção de runtime e teste real local

## Correção principal
A versão anterior usava `EnableRetryOnFailure()` junto com várias transações manuais (`BeginTransactionAsync`). Isso causava:

`SqlServerRetryingExecutionStrategy does not support user-initiated transactions`

Na v3.3 a estratégia de retry foi removida da configuração padrão do EF Core. Os fluxos transacionais existentes voltam a funcionar corretamente com SQL Server LocalDB/SQL Server local.

## Banco padrão
`Server=(localdb)\\MSSQLLocalDB;Database=NaxapaDB;Trusted_Connection=True`

## Criar acesso e testar
1. Execute a aplicação:
   `dotnet run --project Naxapa.Web/Naxapa.Web.csproj --urls http://localhost:5180`
2. Em outro terminal PowerShell:
   `powershell -ExecutionPolicy Bypass -File .\\scripts\\CriarAcessoETestar.ps1`

Se o banco estiver sem usuários, o script cria um administrador com senha aleatória forte e mostra a senha uma única vez. Depois testa login e as principais páginas autenticadas.

O script não possui senha fixa embutida no código.
