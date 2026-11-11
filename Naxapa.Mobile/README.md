# NAXAPA Mobile — v3.5.0

Aplicativo Flutter Android integrado à API REST do NAXAPA.

## O que foi corrigido

- Corrigidos erros de tipagem Dart nos cálculos do carrinho (`fold<double>`).
- Código Dart reorganizado e tipado para facilitar `flutter analyze`.
- Contratos JSON alinhados aos DTOs reais da API NAXAPA.
- Login com teste de conexão `/health`.
- Catálogo mostra somente produtos disponíveis no cardápio.
- Imagens cadastradas na Gestão são resolvidas pela URL da API e exibidas no mobile.
- Cache SQLite preservado para consulta do último cardápio sincronizado.
- Logout tenta revogar o token na API e sempre limpa a sessão local.
- Tratamento de timeout e erro de conexão.
- Testes de modelos, carrinho e contrato da API.

## Preparação obrigatória no Windows

A partir da raiz do projeto NAXAPA, execute:

```powershell
.\PREPARAR_MOBILE.bat
```

Esse script usa o Flutter instalado na máquina para gerar o scaffold Android compatível com a sua versão do SDK, habilita a rede HTTP somente no build de debug para desenvolvimento local e executa `flutter pub get`.

Depois valide:

```powershell
.\VALIDAR_MOBILE.bat
```

E execute:

```powershell
.\RUN_MOBILE.bat
```

## API

- Emulador Android: `http://10.0.2.2:5180`
- Celular físico: `http://IP-DO-PC:5180`

Para celular físico, a Gestão/API deve estar rodando com:

```powershell
dotnet run --project Naxapa.Web\Naxapa.Web.csproj --urls http://0.0.0.0:5180
```

## Segurança de rede

O acesso HTTP sem TLS é habilitado pelo script somente para o manifest de debug Android. Para uma publicação real, configure HTTPS na API.
