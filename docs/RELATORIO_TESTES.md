# NAXAPA v3 — Relatório de testes

## Testes efetivamente executados neste ambiente

- Validação automatizada de estrutura dos quatro projetos.
- Parse XML de `.csproj` e arquivos WPF XAML.
- Parse JSON de configurações.
- Verificação de `net10.0`/`net10.0-windows` e ausência de target .NET 8.
- Verificação das versões Microsoft 10.0.14 no Web.
- Verificação heurística de delimitadores em C#, Dart e JavaScript.
- `node --check` nos JavaScripts do Web.
- Verificações dos contratos de autenticação/API, rotas, permissões e rate limiting.
- Verificações do script SQL: tabelas, procedures críticas, patch v3 e schema version.
- Verificação de fila de produção e comprovante idempotentes.
- Verificações do PDV: produtos, opções, pedidos, pagamentos, mesas/comandas e caixa.
- Verificações do Flutter: API, SQLite cache, adicionais e preparação Android.
- Verificação dos scripts de build, testes, backup e validação SQL.
- `go vet` dos fontes dos dois launchers.
- Compilação real dos dois executáveis Go para Windows x64.
- Recompilação determinística: duas compilações consecutivas produziram SHA-256 idêntico para cada executável.
- Identificação dos executáveis como PE32+ Windows x86-64.

### Resultado da suíte estática

Consultar `RELATORIO_TESTES_ESTATICOS.txt`: **171/171 verificações aprovadas** após a última alteração do código.

## Hash dos executáveis gerados

- `NAXAPA_Abrir_Gestao_API_v3.exe`: `9f84285b216a56fb579633694017eeb9fca1fc8b8ef2565a2451a90c0bfb7c95`
- `NAXAPA_Instalar_Dependencias_v3.exe`: `a27867b1cad168ca05628cf9d4d74e300a3f7ce73f73d906e4185a2b258e64b3`

## Testes preparados, mas NÃO executados neste ambiente

Os itens abaixo não são marcados como aprovados:

1. `dotnet restore`, `dotnet build`, `dotnet test` e `dotnet publish` — SDK .NET 10 não está instalado e o download direto do SDK não foi permitido pelo ambiente.
2. Execução da Gestão/API contra SQL Server real.
3. `tests/ValidateDatabase.sql` em SQL Server.
4. Fluxo real login → produto → pedido → pagamento → caixa.
5. Concorrência de operadores/caixa.
6. `flutter pub get`, `flutter analyze`, `flutter test` e build APK — Flutter/Android SDK ausentes.
7. Execução do PDV em Windows/WPF.
8. Impressão física Epson, Elgin, Tanca ou ESC/POS.
9. Testes de carga/tempo de resposta — não há ambiente de runtime representativo aqui.

## Como executar a validação completa no Windows

1. Rode `NAXAPA_Instalar_Dependencias_v3.exe` como Administrador.
2. Abra PowerShell na raiz e execute `./scripts/Build-All.ps1`.
3. Inicie a Gestão com `NAXAPA_Abrir_Gestao_API_v3.exe`.
4. Faça o Setup inicial em `http://localhost:5180/Setup` se não houver usuário.
5. Rode `./scripts/Test-Naxapa.ps1` com credenciais de teste.
6. Execute `tests/ValidateDatabase.sql` no `NaxapaDB`.
7. Teste PDV e Print Agent publicados em `artifacts`.
8. Para Mobile, execute `scripts/Prepare-Mobile.ps1`, depois analise/teste/build com Flutter.
