# NAXAPA v3 — Auditoria técnica e status de implementação

Data da auditoria: 06/11/2026

## Critério usado

Os estados abaixo significam:

- **Implementado em código**: existe implementação concreta, integrada aos componentes correspondentes e sem dados simulados no fluxo principal.
- **Validado estaticamente**: arquivos, contratos, estrutura, XML/JSON, referências e regras críticas passaram pelas verificações automatizadas disponíveis neste ambiente.
- **Aguardando validação de runtime**: depende de Windows/.NET 10, SQL Server, Flutter/Android ou equipamento físico que não está disponível no ambiente de geração.
- **Pendente real**: requisito ainda não foi implementado integralmente.

## Auditoria do projeto original recebido

A base existente possuía o NAXAPA Gestão Web em ASP.NET Core MVC, integração inicial ao SQL Server, banco NaxapaDB e launchers. Não havia autenticação completa, API central suficiente para os demais aplicativos, PDV funcional completo, aplicativo Flutter integrado nem agente de impressão. Algumas telas utilizavam implementação parcial e a seleção do estabelecimento ainda dependia de configuração fixa.

O trabalho foi realizado preservando o banco e as funcionalidades existentes, sem recriar desnecessariamente o sistema.

## Status por módulo

| Módulo | Estado | Evidência / observação |
|---|---|---|
| Banco SQL Server | Implementado em código + validado estaticamente | Script completo v3, patch idempotente, PK/FK/constraints, procedures, triggers, índices, caixa, insumos/receitas, fila de impressão |
| Gestão Web | Implementado em código + validado estaticamente | MVC .NET 10, login, perfis/permissões, multiestabelecimento, CRUDs, dashboard, pedidos, caixa, cardápios e impressão |
| Autenticação Web | Implementado em código + validado estaticamente | Cookie, PasswordHasher, bloqueio por tentativas, rotas protegidas, seleção de estabelecimento |
| API REST | Implementado em código + validado estaticamente | Token opaco, autorização, rate limiting, catálogo, clientes, pedidos, pagamentos, caixa, mesas, comandas, impressão, administração |
| Produtos | Implementado em código + validado estaticamente | CRUD, imagens, filtros, paginação, CSV com pré-visualização, categorias/setores, adicionais |
| Clientes | Implementado em código + validado estaticamente | CRUD, pesquisa, desativação e histórico de compras |
| Categorias/Cardápios | Implementado em código + validado estaticamente | CRUD, ordem, imagens, produtos associados, publicação |
| Adicionais/Opções | Implementado em código + validado estaticamente | Grupos, mínimo/máximo, obrigatório, valores adicionais, associação a produtos |
| Insumos/Receitas | Implementado em código + validado estaticamente | Insumos, receita/ficha técnica e composição/custo |
| Setores | Implementado em código + validado estaticamente | CRUD e vínculo de produtos |
| Impressoras | Implementado em código + validado estaticamente | Cadastro, padrão, setor, teste, configuração, histórico, reimpressão |
| Fila de produção | Implementado em código + validado estaticamente | Geração automática ao enviar pedido para PENDENTE, agrupamento por setor, idempotência |
| Comprovante não fiscal | Implementado em código + validado estaticamente | Enfileiramento automático ao FINALIZAR, dados de pedido/pagamentos e aviso DOCUMENTO NÃO FISCAL |
| Caixa | Implementado em código + validado estaticamente | Cadastro, abertura, suprimento, sangria, ajustes, fechamento, diferença e resumo |
| Dashboard | Implementado em código + validado estaticamente | Períodos Hoje/Ontem/7 dias/Mês/Personalizado e indicadores reais do banco |
| PDV Windows | Implementado em código + validado estaticamente | WPF .NET 10, login API, produtos, opções, pedidos, status, pagamentos, mesas/comandas e caixa |
| Mobile Flutter | Implementado em código + validado estaticamente | Login, catálogo, opções, carrinho, pedidos, status e SQLite apenas como cache |
| Print Agent | Implementado em código + validado estaticamente | Poll da fila, claim/complete/fail, TCP/Windows RAW, produção e comprovante |
| Testes unitários .NET | Código criado; runtime pendente | xUnit para helpers de segurança e relógio; não executado aqui por ausência do SDK .NET 10 |
| Flutter tests/analyze | Código e script criados; runtime pendente | `flutter analyze`, `flutter test` e APK no Build-All; Flutter indisponível neste ambiente |
| SQL runtime/integridade | Script de validação criado; runtime pendente | `tests/ValidateDatabase.sql` deve ser executado em SQL Server real |
| Performance | Pendente de medição real | Não foram inventados tempos. Deve ser medido no SQL Server/API do ambiente alvo |
| Impressoras físicas | Pendente de hardware real | Agente implementado; requer teste em Epson/Elgin/Tanca/ESC-POS físicos |
| Recuperação “esqueci minha senha” via e-mail | **Pendente real** | Alteração pelo próprio usuário e reset por administrador existem; não há provedor de e-mail configurado, portanto recuperação externa não foi simulada |
| Integração fiscal NFC-e/NF-e | Fora do escopo implementado | Comprovante é explicitamente não fiscal; emissão fiscal exige integração legal específica |

## Riscos e limitações ainda reais

1. O ambiente desta geração não dispõe de `dotnet`, Flutter, Windows, SQL Server nem impressora térmica. Por isso não há alegação de build/runtime aprovado desses componentes.
2. O launcher do Windows executa `dotnet restore` e `dotnet build` antes de iniciar a Gestão; se houver falha real de compilação na máquina alvo, ele interrompe e gera log em vez de mascarar o erro.
3. O agente de impressão confirma que o trabalho foi aceito pelo transporte/spooler. Impressoras sem protocolo de confirmação física não permitem provar que o papel foi efetivamente impresso apenas por software.
4. Para produção comercial, a conexão SQL deverá usar certificado confiável e segredo/configuração por ambiente; a configuração atual prioriza localhost/SQLEXPRESS.
5. Medições de carga e concorrência precisam ser executadas com volume representativo no ambiente Windows/SQL Server.
