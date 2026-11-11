# NAXAPA PDV

Aplicação WPF em .NET 10 (`net10.0-windows`) que consome exclusivamente a API REST do NAXAPA Gestão.

Fluxos implementados em código: login, produtos, carrinho, criação/envio de pedido, acompanhamento de pedidos, avanço de status, pagamento, consulta de mesas/comandas e consulta do caixa.

Não há conexão direta com SQL Server. A URL da API é configurável no login e persistida em `%APPDATA%\NAXAPA\PDV\settings.json`.

## Build no Windows

```powershell
dotnet restore
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

A compilação precisa ser validada em uma máquina com .NET 10 SDK; o ambiente de geração deste pacote não possui o SDK .NET.
