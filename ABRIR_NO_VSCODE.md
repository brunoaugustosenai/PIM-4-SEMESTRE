# NAXAPA no VS Code

## Requisitos

- Windows 10/11 x64.
- .NET 10 SDK (`dotnet --version` deve começar com `10.`).
- SQL Server Express com instância `SQLEXPRESS` para a configuração padrão.
- VS Code.
- Para Mobile: Flutter + Android SDK.

## Abrir o projeto

1. Extraia este ZIP.
2. Abra a pasta `NAXAPA` no VS Code ou abra `NAXAPA.code-workspace`.
3. Aceite as extensões recomendadas pelo VS Code.
4. Abra o terminal integrado (`Ctrl+J`).

## Gestão Web + API

A conexão padrão está em `Naxapa.Web/appsettings.json`:

```text
Server=localhost\\SQLEXPRESS;Database=NaxapaDB;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True
```

Execute:

```powershell
dotnet restore NAXAPA.sln
dotnet build Naxapa.Web/Naxapa.Web.csproj -c Debug
dotnet run --project Naxapa.Web/Naxapa.Web.csproj --urls http://0.0.0.0:5180
```

Abra:

```text
http://localhost:5180
```

Também é possível usar `Terminal > Run Task > NAXAPA: Run Web/API` ou pressionar `F5` e selecionar **NAXAPA Gestão/API (.NET 10)**.

Na primeira execução, o projeto está configurado para inicializar o `NaxapaDB` quando necessário. Se você já possui o banco, preserve-o e faça backup antes de aplicar patches.

## Testes .NET

```powershell
dotnet test Naxapa.Web.Tests/Naxapa.Web.Tests.csproj -c Debug
```

Ou use `Terminal > Run Task > NAXAPA: Test Web`.

## PDV Windows

```powershell
dotnet run --project Naxapa.Pdv/Naxapa.Pdv.csproj
```

O PDV é WPF e precisa ser executado no Windows.

## Print Agent

```powershell
dotnet run --project Naxapa.PrintAgent/Naxapa.PrintAgent.csproj
```

## Flutter Mobile

Entre na pasta:

```powershell
cd Naxapa.Mobile
flutter pub get
flutter analyze
flutter run
```

No Android Emulator, a API do computador normalmente é acessada por `http://10.0.2.2:5180`. Em celular físico, use o IP local do PC, por exemplo `http://192.168.0.10:5180`, com ambos na mesma rede e a porta 5180 liberada no firewall.

## Banco

Arquivos principais:

- `NaxapaDB_Completo_v3.sql` — criação completa.
- `NaxapaDB_Patch_v3.sql` — atualização da versão anterior.
- `tests/ValidateDatabase.sql` — validações do banco.

Não execute o script completo sobre um banco com dados importantes sem backup.


## Banco local usado nesta versão

A configuração padrão do `Naxapa.Web/appsettings.json` está apontando para:

```text
(localdb)\MSSQLLocalDB
Banco: NaxapaDB
Autenticação: Windows
```

Na primeira execução, se `seg.Usuario` estiver vazio, abra `http://localhost:5180/Conta/Login`; o sistema redirecionará automaticamente para `/Setup` para cadastrar o primeiro administrador.
