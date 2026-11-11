# NAXAPA v3.2.1 — Correção Setup + LocalDB

Esta revisão corrige os problemas reproduzidos no Windows:

1. O código-fonte v3.1 redirecionava para `/Setup`, mas o `SetupController` e a view `Views/Setup/Index.cshtml` não estavam presentes no pacote entregue. Resultado: HTTP 404.
2. A conexão padrão estava apontando para outra instância SQL em versões anteriores. Agora o desenvolvimento local usa `(localdb)\\MSSQLLocalDB`, banco `NaxapaDB`, com autenticação integrada do Windows.
3. Propriedades `decimal` computadas foram configuradas explicitamente com precisão `decimal(18,2)` no EF Core para eliminar avisos de truncamento.
4. Quando `seg.Usuario` estiver vazia, `/Conta/Login` redireciona automaticamente para `/Setup`.
5. `/Setup` só funciona via localhost e cria o primeiro administrador usando `PasswordHasher`, sem senha padrão em código.

## Executar

```powershell
dotnet restore NAXAPA.sln
dotnet clean Naxapa.Web/Naxapa.Web.csproj
dotnet build Naxapa.Web/Naxapa.Web.csproj -c Debug
dotnet run --project Naxapa.Web/Naxapa.Web.csproj --urls http://localhost:5180
```

Abra `http://localhost:5180`. Se ainda não houver usuário, o sistema abrirá a configuração inicial automaticamente.
