# NAXAPA v3.4 — melhorias incrementais solicitadas

Esta versão parte da v3.3.1 estável e altera somente os pontos solicitados:

- Gerador de GTIN-13 no cadastro de produto, com verificação de duplicidade e dígito verificador.
- Cardápio digital público em `/cardapio-digital`, usando produtos reais marcados como disponíveis.
- Upload de imagem de produto com arrastar e soltar, mantendo o FileStorageService existente.
- CPF/CNPJ obrigatório e validado na primeira criação da empresa (`/Setup`).

Não foram alterados os mecanismos centrais de autenticação, conexão SQL Server, transações, pedidos, caixa, API, PDV ou Mobile. Nenhuma tabela nova é necessária.
