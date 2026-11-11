# NAXAPA v3.2 — correção LocalDB e login inicial

- `appsettings.json` alterado para `(localdb)\MSSQLLocalDB`.
- Mantido banco `NaxapaDB` com Windows Authentication.
- Precisão `decimal(18,2)` explicitada em propriedades computadas do EF Core.
- Diagnóstico do banco não assume mais `SQLEXPRESS`.
- Primeiro usuário: acesse `/Conta/Login`; quando não houver usuários, o sistema redireciona para `/Setup`.
- Nenhuma senha padrão foi embutida.
