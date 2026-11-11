# NAXAPA v3.4 — relatório de validação da atualização

## Escopo alterado

- Produto: geração de GTIN-13 e interface de upload de imagem por arrastar e soltar.
- Cardápio digital: nova visualização pública com produtos ativos/disponíveis, categoria, imagem, descrição e preço.
- Setup: CPF/CNPJ obrigatório, sanitizado, validado e gravado em `cad.Estabelecimento.Documento`.

## Validações executadas neste ambiente

- `tests/static_validate.py`: **202/202 verificações aprovadas**.
- JavaScript `product-form.js`: sintaxe validada com `node --check`.
- JavaScript `digital-menu.js`: sintaxe validada com `node --check`.
- Regressão Razor: nenhuma diretiva `@model IReadOnlyList<T>` quebrada.
- Comparação binária confirmou que arquivos sensíveis não solicitados permaneceram inalterados, incluindo `NaxapaDbContext`, `DatabaseInitializer`, autenticação, workflow de pedidos, Caixa, Pedidos, `appsettings.json`, `.csproj` e scripts SQL principais.
- Algoritmos de CPF, CNPJ e dígito verificador GTIN-13 foram verificados contra casos válidos e inválidos conhecidos.

## Testes automatizados adicionados

- `Gtin13ServiceTests.cs`: geração e validação do GTIN-13.
- `BrazilianDocumentValidatorTests.cs`: CPF/CNPJ válidos e inválidos.
- `SetupLoginIntegrationTests.cs`: atualizado para exigir/persistir CNPJ, testar endpoint GTIN e acesso anônimo ao cardápio digital.

## Limitação do ambiente de geração

Este contêiner não possui o SDK .NET 10 nem SQL Server LocalDB, e a rede não permite baixar o SDK. Portanto, **não foi declarado build/runtime .NET aprovado neste ambiente**. O pacote mantém `scripts/Validar-NAXAPA.ps1`, que executa `restore`, `build`, `dotnet test` e a validação estática no Windows com .NET 10/LocalDB.
