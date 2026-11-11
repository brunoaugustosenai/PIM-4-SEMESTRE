# NAXAPA Mobile — Relatório de correção v3.5.0

## Escopo
Somente `Naxapa.Mobile`, scripts de preparação/validação do Flutter e configuração de execução do VS Code foram alterados. Web, API, SQL Server, PDV e Print Agent não foram modificados.

## Causas encontradas
1. Cálculos financeiros no Dart usavam `fold(0, ...)`, fazendo o tipo acumulador ser inferido como `int` enquanto os valores eram `double`.
2. O módulo não possuía scaffold Android no pacote e dependia de preparação externa.
3. Tratamento de conexão/API era muito rígido e pouco informativo.
4. As imagens retornadas pela API podiam ser URLs relativas e não eram exibidas no catálogo mobile.

## Correções
- `fold<double>(0.0, ...)` nos cálculos monetários.
- `fold<int>(0, ...)` para quantidade do carrinho.
- Modelos Dart reorganizados e alinhados aos DTOs C# atuais.
- Teste de conexão com `/health` na tela de login.
- Timeout e mensagens de erro de rede.
- Resolução de URLs relativas de imagens.
- Exibição de imagens no catálogo e detalhe do produto.
- Catálogo mobile filtra `Available == true`.
- Logout revoga token quando possível e sempre limpa sessão local.
- Scaffold Android gerado com a versão do Flutter instalada no computador.
- HTTP local liberado apenas no manifest Android de debug.
- Scripts `PREPARAR_MOBILE.bat`, `VALIDAR_MOBILE.bat`, `RUN_MOBILE.bat` e `RUN_WEB_PARA_MOBILE.bat`.
- Configuração Flutter adicionada ao `.vscode/launch.json`.

## Testes adicionados
- cálculo de carrinho/adicionais;
- parsing dos modelos JSON;
- contrato do endpoint de autenticação;
- contrato de `ApiPagedResponse` de produtos.

## Validação realizada neste ambiente
- comparação byte a byte: Web/API/SQL/PDV/PrintAgent inalterados;
- imports locais Dart existentes;
- delimitadores Dart balanceados;
- padrão incorreto `fold(0, ...)` eliminado;
- chaves JSON conferidas contra DTOs C#;
- JSON do VS Code válido;
- `pubspec.yaml` válido.

O ambiente de geração não possui Flutter/Android SDK, portanto `flutter analyze`, `flutter test` e `flutter build apk --debug` devem ser executados no Windows pelo `VALIDAR_MOBILE.bat`. O script só declara aprovação se esses comandos retornarem sucesso.
