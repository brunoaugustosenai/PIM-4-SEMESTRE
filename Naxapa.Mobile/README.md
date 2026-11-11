# NAXAPA Mobile — Expo

Esta pasta substitui a pasta `Naxapa.Mobile` Flutter quando você quiser trabalhar com Expo/React Native.
Ela não altera a Web/API nem o banco SQL Server.

## Recursos equivalentes ao Flutter

- Login pela API NAXAPA (`/api/v1/auth/token`)
- Teste de conexão (`/health`)
- Sessão protegida com `expo-secure-store`
- Cardápio/produtos cadastrados
- Imagens dos produtos
- Detalhes, adicionais e opções
- Carrinho e envio do pedido
- Pedidos e status
- Cache local de produtos com SQLite
- Configurações e logout

## Primeira execução

1. Dê dois cliques em `PREPARAR_EXPO.bat`.
2. No projeto principal, execute `Naxapa.Mobile\RUN_WEB_PARA_EXPO.bat` ou rode a Web/API em `0.0.0.0:5180`.
3. Descubra o IPv4 do PC com `ipconfig`.
4. Dê dois cliques em `RUN_EXPO.bat`.
5. Abra o Expo Go no celular e leia o QR Code.
6. Na tela de login, informe `http://IP_DO_PC:5180`.

Exemplo: `http://192.168.0.15:5180`.

O celular e o PC precisam estar na mesma rede Wi-Fi. O Firewall do Windows deve permitir a porta 5180 em rede privada.

## Observação sobre Expo Go

Este projeto está preparado no SDK 57 por compatibilidade com Expo Go durante a transição do SDK 58. Quando o Expo Go do aparelho atualizar para SDK 58 de forma estável, o projeto pode ser atualizado com as ferramentas oficiais do Expo.
