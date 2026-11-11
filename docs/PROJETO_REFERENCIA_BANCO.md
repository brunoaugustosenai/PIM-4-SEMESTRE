# Projeto de referência — decisões aproveitadas no NAXAPA

O projeto de referência enviado foi analisado especificamente para conexão com banco e uso do Entity Framework Core.

## Padrões aproveitados

- `DbContext` registrado diretamente com `UseSqlServer(...)`.
- String de conexão obtida da configuração da aplicação.
- Precisão explícita com `HasPrecision(...)` em propriedades `decimal`.
- Separação entre entidades e contexto de persistência.

## O que NÃO foi copiado

- O projeto de referência usa `.NET 8`; o NAXAPA continua obrigatoriamente em `.NET 10` (`net10.0`).
- O projeto de referência usa CORS aberto (`AllowAnyOrigin`); o NAXAPA mantém controle mais restritivo e autenticação/autorização.
- Não foi adotado armazenamento ou autenticação incompatível com o modelo de segurança já definido no NAXAPA.

## Correção de runtime aplicada

A versão anterior do NAXAPA combinava `EnableRetryOnFailure()` com `BeginTransactionAsync()` em fluxos como Setup, Produtos, Cardápio, Impressoras, Usuários e Pedidos. O EF Core rejeita transações iniciadas pelo usuário quando a estratégia de retry está ativa sem `CreateExecutionStrategy()`.

Para o ambiente local do NAXAPA (`(localdb)\\MSSQLLocalDB`) foi adotado o mesmo padrão simples do projeto de referência: `UseSqlServer` direto, sem retry automático. Isso mantém as transações manuais válidas e previsíveis.
