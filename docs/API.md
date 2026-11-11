# NAXAPA API REST v1

Base local padrão: `http://localhost:5180/api/v1`

Todas as rotas, exceto obtenção de token, usam:

`Authorization: Bearer <token>`

Os tokens são opacos; somente o hash é armazenado no SQL Server. A autorização também verifica usuário ativo, vínculo ativo ao estabelecimento e permissão da operação.

## Autenticação

- `POST /auth/token` — login e emissão do token.
- `POST /auth/revoke` — revoga o token atual.

## Catálogo

- `GET /products?q=&page=&pageSize=` — produtos paginados.
- `GET /products/{id}` — produto, descrição e grupos/opções.
- `GET /categories` — categorias ativas.
- `GET /menu` — cardápio publicado do estabelecimento.

## Clientes

- `GET /clients?q=&page=&pageSize=` — pesquisa/paginação.
- `GET /clients/{id}` — cliente.
- `POST /clients` — cadastro.

## Pedidos

- `GET /orders?status=&page=&pageSize=` — consulta.
- `GET /orders/{id}` — resumo.
- `POST /orders` — cria pedido.
- `POST /orders/{id}/items` — adiciona produto.
- `POST /orders/items/{itemId}/options` — adiciona opção/adicional.
- `POST /orders/{id}/status` — transição operacional; ao ir para PENDENTE gera fila de produção; ao FINALIZAR baixa estoque controlado e enfileira comprovante não fiscal.

## Financeiro e caixa

- `GET /payment-methods` — formas de pagamento.
- `POST /orders/{id}/payments` — registra pagamento transacional.
- `GET /cash-registers` — caixas cadastrados.
- `GET /cash` — sessões/resumo.
- `POST /cash/open` — abertura.
- `POST /cash/{id}/movements` — suprimento/sangria/ajustes autorizados.
- `POST /cash/{id}/close` — fechamento e conferência.

## Operação

- `GET /tables` — mesas.
- `POST /tables` — cadastro de mesa.
- `GET /tabs?status=` — comandas.
- `POST /tabs/open` — abre comanda.
- `POST /tabs/{id}/close` — fecha comanda.

## Impressão

- `GET /printers` — impressoras acessíveis ao agente.
- `GET /print-jobs/pending` — fila pendente/erro elegível.
- `POST /print-jobs/{id}/claim` — marca processamento.
- `POST /print-jobs/{id}/complete` — confirma envio concluído.
- `POST /print-jobs/{id}/fail` — registra falha/tentativa.

## Administração/relatórios

A API administrativa expõe estabelecimentos autorizados, usuários, perfis, setores, insumos, receitas e indicadores do dashboard, sempre com permissão correspondente.

## Erros

A API utiliza códigos HTTP e `ProblemDetails` para erros de validação/regra de negócio. Operações críticas usam transação e, quando aplicável, idempotência.

## OpenAPI

Com a Gestão em execução, o documento OpenAPI é mapeado pelo ASP.NET Core e pode ser usado como referência para clientes e ferramentas de teste.
