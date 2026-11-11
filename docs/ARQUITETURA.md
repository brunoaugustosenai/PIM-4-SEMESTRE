# NAXAPA v3 — Arquitetura técnica

## Visão

```text
NAXAPA Gestão Web (ASP.NET Core MVC) ─┐
                                      │
NAXAPA PDV (WPF .NET 10) ───── API REST /api/v1 ─── Serviços/Regras ─── SQL Server NaxapaDB
                                      │                     │
NAXAPA Mobile (Flutter + cache SQLite)┘                     └── Fila de impressão
                                                                    │
                                                           NAXAPA Print Agent
                                                                    │
                                                      TCP / Windows RAW / ESC-POS
```

## Gestão/API

Projeto `Naxapa.Web`, target `net10.0`:

- Controllers MVC: interface administrativa.
- Controllers API: contratos REST.
- Services: autenticação, clock, arquivos, importação CSV e workflow de pedidos.
- Security: cookie, token opaco, policy provider e autorização por permissão.
- Data: `NaxapaDbContext` e inicialização/versionamento do banco.
- Entities/Models: persistência e DTOs/ViewModels.

A API e a Gestão compartilham a mesma camada de serviços e contexto para evitar duplicar regras.

## PDV

Projeto `Naxapa.Pdv`, `net10.0-windows`, WPF. Não acessa SQL diretamente. Consome a API para autenticação, produtos/opções, pedidos, status, pagamentos, mesas, comandas e caixa.

## Mobile

Flutter/Dart. Consome a API. SQLite (`sqflite`) é usado somente para cache local do catálogo e metadados de sincronização. Pedidos críticos não são confirmados apenas no dispositivo.

## Impressão

O navegador não fala diretamente com impressoras. A aplicação grava `imp.TrabalhoImpressao`; o Print Agent autenticado consome a fila, reivindica o job, envia para TCP ou spooler Windows e registra sucesso/falha.

Produção é separada por setor. Chaves idempotentes impedem criação repetida do mesmo trabalho em condições normais. Comprovante final é identificado como documento não fiscal.

## Banco

Schemas:

- `seg`: segurança.
- `cad`: cadastros/cardápio.
- `cfg`: configurações/impressoras.
- `op`: pedidos/mesas/comandas.
- `fin`: pagamentos/caixa.
- `est`: estoque.
- `imp`: fila de impressão.
- `aud`: auditoria.
- `rel`: views de relatório.

O script v3 preserva a base v2 e aplica patch idempotente para as extensões mais recentes.

## Segurança

- Senha via `PasswordHasher<UsuarioEntity>`; nunca texto puro.
- Token API opaco e hash SHA-256 persistido.
- Lockout de login.
- Anti-forgery em formulários MVC de alteração.
- Autorização por estabelecimento e permissão.
- Rate limiting da API.
- SQL parametrizado por EF Core/SqlClient.
- Setup inicial somente por loopback quando não existem usuários.
- SQL Server não deve ser exposto diretamente à Internet.
