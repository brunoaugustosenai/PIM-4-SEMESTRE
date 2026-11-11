# NAXAPA v3.2.3 — Correção de build e dependência de segurança

Correções:

- 14 views Razor com `@model IReadOnlyList<T>` corrigidas em uma única linha.
- Mantida conexão padrão com `(localdb)\MSSQLLocalDB` e banco `NaxapaDB`.
- Mantido `/Setup` para criação do primeiro administrador.
- Adicionada referência explícita `System.Security.Cryptography.Pkcs` 10.0.14 para substituir a versão transitiva 9.0.11 reportada pelos avisos NU1902/NU1903.
- Nenhum projeto usa .NET 8; o projeto Web continua em `net10.0`.

Use esta versão em uma pasta nova e não compile a pasta v3.2.1 antiga.
