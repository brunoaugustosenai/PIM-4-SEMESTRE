# NAXAPA v3 — Relatório de bugs e lacunas corrigidas

## Correções principais

1. **Acesso ao painel sem autenticação suficiente** — foi adicionada autenticação por cookie, proteção global de rotas, logout e sessão.
2. **Perfis sem autorização efetiva** — permissões são carregadas do banco por usuário/estabelecimento e aplicadas às actions e ao menu.
3. **Estabelecimento fixo em configuração** — substituído por estabelecimento autorizado do usuário e troca de estabelecimento.
4. **Senha/usuário inicial inseguro** — não existe senha padrão; o primeiro administrador é criado em `/Setup` apenas via loopback/localhost.
5. **API insuficiente para PDV/Mobile** — adicionados endpoints autenticados e autorizados para catálogo, clientes, pedidos, pagamentos, caixa, mesas, comandas, impressão e administração.
6. **Token API incompatível entre clientes e servidor** — contrato foi padronizado (`token`, `userId`, `userName`, estabelecimento e expiração).
7. **Tokens expostos no banco** — somente hash SHA-256 do token opaco é persistido; token puro existe apenas no cliente.
8. **Ausência de bloqueio de tentativa de login** — implementado controle de tentativas e bloqueio temporário.
9. **Dados demonstrativos no fluxo real** — removidos do caminho principal; Gestão/API consultam SQL Server.
10. **Produtos sem importação robusta** — adicionada importação CSV com validação e pré-visualização.
11. **Produtos sem imagem real** — armazenamento de imagens com limite de tamanho e validação por assinatura JPG/PNG/WEBP.
12. **Clientes sem histórico** — histórico de compras foi ligado aos pedidos reais.
13. **Dashboard fixo em “hoje”** — adicionados filtros Hoje/Ontem/7 dias/Mês/Personalizado.
14. **Caixa apenas de leitura** — Gestão e API passaram a abrir, movimentar e fechar caixa usando procedures transacionais.
15. **Permissões de caixa insuficientes** — `CAIXA.CADASTRAR`, abrir, movimentar, fechar e relatório foram reconciliadas por perfil.
16. **Erro T-SQL no teste de impressora** — corrigida a construção do trabalho de teste.
17. **Impressora/setor sem configuração automática** — cadastro/edição sincroniza configuração de produção.
18. **Pedido não gerava fila de produção automaticamente** — mudança para `PENDENTE` agora cria trabalhos por setor/impressora.
19. **Risco de impressão duplicada** — chave idempotente determinística por pedido/setor/impressora.
20. **Sem histórico/reimpressão de trabalhos** — adicionados histórico e reimpressão autorizada.
21. **Sem comprovante ao finalizar** — finalização gera comprovante não fiscal idempotente na impressora padrão/configurada.
22. **PDV não tratava adicionais** — detalhe do produto, seleção/validação e envio de opções vinculadas ao item foram adicionados.
23. **PDV sem mesas/comandas** — consulta, abertura e fechamento foram adicionados via API.
24. **PDV sem caixa integrado** — cadastro/lista de caixas, abertura, suprimento, sangria e fechamento foram conectados à API.
25. **Flutter seria isolado** — agora usa API real; SQLite serve somente de cache do catálogo.
26. **Flutter sem scaffold Android no pacote fonte** — `Prepare-Mobile.ps1` cria o host Android em temporário e habilita acesso HTTP à API local.
27. **Print Agent não existia** — criado agente .NET 10 para fila persistente e transporte TCP/Windows RAW.
28. **Pacotes Microsoft estavam em 10.0.0** — atualizados para ASP.NET Core OpenAPI e EF Core 10.0.14, mantendo `net10.0`.
29. **Launchers desatualizados em relação ao código** — pacote embutido foi regenerado e executáveis Windows x64 recompilados de forma determinística.
30. **Adicional não acompanhava a quantidade do item** — o trigger de opções foi corrigido para multiplicar o valor unitário das opções pela quantidade da linha do pedido, evitando total incorreto em 2+ unidades.
31. **Produto controlado não baixava estoque na finalização** — a finalização passou a executar `est.sp_Estoque_Movimentar` na mesma transação para produtos com `ControlaEstoque`, revertendo a venda se a regra de saldo impedir a saída.
32. **Acentos podiam sair corrompidos em ESC/POS genérico** — o renderer passou a normalizar/transliterar texto para ASCII antes do envio RAW, priorizando compatibilidade entre impressoras térmicas sem exigir code page específica.
33. **Razor CS1963 em formulários administrativos** — oito partial views tinham a diretiva `@model` concatenada ao HTML na mesma linha (`@model Tipo<div...`), fazendo o Razor tratar o modelo incorretamente e gerar dezenas de erros `CS1963` em cascata. As views foram corrigidas e foi adicionado teste de regressão específico; o launcher v3.1 também limpa `bin/obj` antes de compilar para eliminar artefatos Razor antigos.
