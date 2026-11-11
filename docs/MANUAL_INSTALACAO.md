# NAXAPA v3 — Manual de instalação

## Requisitos

- Windows 10/11 x64.
- Acesso de Administrador para instalar pré-requisitos e criar regra de firewall.
- Internet na primeira instalação para NuGet/WinGet e, opcionalmente, Flutter/Android.

## 1. Dependências

Execute como Administrador:

`NAXAPA_Instalar_Dependencias_v3.exe`

O verificador instala/verifica:

- .NET 10 SDK.
- Google Chrome.
- Git.
- SQL Server Express/SQLEXPRESS quando necessário.
- Regra de firewall TCP 5180 para rede Privada/Domain.
- Opcionalmente Android Studio e Flutter stable.

Ele evita reinstalar uma instância `SQLEXPRESS` detectada.

## 2. Iniciar Gestão/API

Execute:

`NAXAPA_Abrir_Gestao_API_v3.exe`

O launcher:

1. valida que o SDK encontrado começa por `10.`;
2. extrai a versão atual do NAXAPA Web/API para o cache do usuário;
3. executa `dotnet restore`;
4. executa `dotnet build -c Release`;
5. somente se o build passar, inicia em `http://0.0.0.0:5180`;
6. abre `http://localhost:5180` no navegador;
7. mostra os IPs para acesso pela mesma rede;
8. grava log `naxapa-startup.log` na pasta extraída.

## 3. Banco

A conexão local padrão é:

`Server=localhost\\SQLEXPRESS;Database=NaxapaDB;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True`

Na primeira execução, se `NaxapaDB` não existir e `AutoCreateDatabase=true`, a aplicação executa o script base e o patch v3 idempotente. Dados existentes não são apagados.

Para ambientes reais, configure `ConnectionStrings__NaxapaDb` por variável de ambiente e utilize certificado SQL confiável.

## 4. Primeiro administrador

Se não existir nenhum usuário, abra **no próprio servidor**:

`http://localhost:5180/Setup`

A página de setup é bloqueada para acesso remoto e deixa de funcionar assim que o primeiro usuário existir. Não existe senha padrão embutida.

## 5. Acesso na rede local

No launcher será mostrado, por exemplo:

`http://192.168.1.20:5180`

Use esse endereço em outro computador/celular na mesma rede. A rede do Windows deve estar marcada como Privada/Domain e a porta 5180 precisa estar liberada.

O SQL Server não deve ser exposto diretamente à Internet.

## 6. Build de todos os módulos

No PowerShell:

`./scripts/Build-All.ps1`

O script compila/testa Web, publica PDV e Print Agent self-contained e, se Flutter estiver disponível, prepara Android, roda analyze/test e cria APK.

## 7. Backup

Use `scripts/Backup-NaxapaDB.ps1`. O script executa `BACKUP DATABASE ... WITH CHECKSUM` e `RESTORE VERIFYONLY`.
