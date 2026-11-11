# NAXAPA — Manual básico do usuário

## Gestão Web

1. Entre com usuário/e-mail e senha.
2. O estabelecimento ativo aparece no menu de usuário. Usuários autorizados podem alternar estabelecimento.
3. Use o Dashboard para consultar faturamento e pedidos por período.
4. Em **Produtos**, cadastre nome, categoria, preço, setor, disponibilidade, imagem e estoque; a importação CSV possui pré-visualização.
5. Em **Opções e adicionais**, crie grupos, mínimo/máximo, obrigatoriedade, valores e associe aos produtos.
6. Em **Cardápios**, publique apenas os produtos desejados.
7. Em **Setores/Impressoras**, vincule cada produto ao setor e configure uma impressora ativa para produção. Marque uma impressora padrão para comprovantes.
8. Em **Caixa**, cadastre o caixa, abra a sessão com valor inicial, registre suprimentos/sangrias e faça a conferência no fechamento.
9. Em **Usuários**, administradores gerenciam perfis e redefinição administrativa de senha.

## PDV

1. Informe o endereço da API, usuário e senha.
2. Pesquise o produto e dê duplo clique.
3. Se houver adicionais, selecione-os respeitando mínimo/máximo.
4. Escolha BALCÃO, VENDA AVULSA, MESA ou COMANDA.
5. Envie o pedido; ele entra como PENDENTE e gera fila por setor quando configurado.
6. Na tela Pedidos, acompanhe e avance status conforme permissões.
7. Registre o pagamento. Dinheiro exige sessão de caixa aberta.
8. Ao finalizar um pedido pago, o sistema pode gerar comprovante não fiscal na fila da impressora padrão/configurada.

## Print Agent

O agente deve rodar no computador que enxerga as impressoras. Ele solicita API, usuário, senha e estabelecimento e não grava a senha. Trabalhos são reivindicados, enviados e marcados como concluídos ou erro.

## Mobile

O Mobile consulta a mesma API. No Android Emulator use normalmente `http://10.0.2.2:5180`; em celular físico use o IP LAN mostrado pelo launcher. SQLite mantém apenas cache local do catálogo, não substitui o SQL Server.
