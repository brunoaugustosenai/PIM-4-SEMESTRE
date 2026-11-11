from pathlib import Path
import json,re,xml.etree.ElementTree as ET,sys
root=Path(__file__).resolve().parents[1]
checks=[]
def check(name, cond, detail=''):
    checks.append((name,bool(cond),detail))

def text(rel): return (root/rel).read_text(encoding='utf-8')

# Required project structure
for rel in [
    'NAXAPA.sln','Naxapa.Web/Naxapa.Web.csproj','Naxapa.Pdv/Naxapa.Pdv.csproj',
    'Naxapa.PrintAgent/Naxapa.PrintAgent.csproj','Naxapa.Mobile/pubspec.yaml',
    'Naxapa.Web.Tests/Naxapa.Web.Tests.csproj','scripts/Build-All.ps1','scripts/Prepare-Mobile.ps1',
    'tests/ValidateDatabase.sql','NaxapaDB_Completo_v3.sql']:
    check(f'Arquivo obrigatório: {rel}', (root/rel).exists())

# XML files
for p in list(root.rglob('*.csproj'))+list(root.rglob('*.xaml')):
    try: ET.parse(p); ok=True; err=''
    except Exception as e: ok=False; err=str(e)
    check(f'XML válido: {p.relative_to(root)}',ok,err)

# JSON files
for p in root.rglob('*.json'):
    if any(x in p.parts for x in ('bin','obj','.dart_tool','build')): continue
    try: json.loads(p.read_text(encoding='utf-8-sig'));ok=True;err=''
    except Exception as e:ok=False;err=str(e)
    check(f'JSON válido: {p.relative_to(root)}',ok,err)

# .NET 10 only
for p in root.rglob('*.csproj'):
    s=p.read_text(encoding='utf-8')
    check(f'.NET 10 target: {p.relative_to(root)}','<TargetFramework>net10.0' in s)
    check(f'Sem target .NET 8: {p.relative_to(root)}','net8.0' not in s.lower())
webproj=text('Naxapa.Web/Naxapa.Web.csproj')
check('ASP.NET OpenAPI 10.0.14','Microsoft.AspNetCore.OpenApi" Version="10.0.14' in webproj)
check('EF Core SQL Server 10.0.14','Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.14' in webproj)
check('EF Core Design 10.0.14','Microsoft.EntityFrameworkCore.Design" Version="10.0.14' in webproj)

# Balanced source syntax heuristic with simple lexical scanner
def balanced(p):
    s=p.read_text(encoding='utf-8-sig',errors='replace')
    pairs={'{':'}','(':')','[':']'}; rev={v:k for k,v in pairs.items()}; st=[]
    i=0;n=len(s);state='code';quote='';verbatim=False
    while i<n:
        c=s[i];nxt=s[i+1] if i+1<n else ''
        if state=='line_comment':
            if c=='\n': state='code'
            i+=1;continue
        if state=='block_comment':
            if c=='*' and nxt=='/': state='code';i+=2;continue
            i+=1;continue
        if state=='string':
            if verbatim:
                if c=='"' and nxt=='"': i+=2;continue
                if c=='"': state='code';verbatim=False;i+=1;continue
                i+=1;continue
            if c=='\\': i+=2;continue
            if c==quote: state='code';i+=1;continue
            i+=1;continue
        if c=='/' and nxt=='/': state='line_comment';i+=2;continue
        if c=='/' and nxt=='*': state='block_comment';i+=2;continue
        if c=='@' and nxt=='"': state='string';quote='"';verbatim=True;i+=2;continue
        if c in ('"',"'"):
            # Dart triple strings are treated as ordinary strings; enough for delimiter checks.
            state='string';quote=c;verbatim=False;i+=1;continue
        if c in pairs: st.append(c)
        elif c in rev:
            if not st or st[-1]!=rev[c]: return False
            st.pop()
        i+=1
    return state not in ('block_comment',) and not st
for ext in ('*.cs','*.dart','*.js'):
    for p in root.rglob(ext):
        if any(x in p.parts for x in ('bin','obj','.dart_tool','build')): continue
        check(f'Delimitadores balanceados: {p.relative_to(root)}',balanced(p))

# Razor regression: directives must end before HTML markup.
razor_bad=[]
html_after_model=re.compile(r'^@model\s+[^\r\n]*<(?:div|form|input|label|textarea|select|section|table|span|main|header|nav|article|aside|h[1-6])\b', re.I)
for rp in (root/'Naxapa.Web/Views').rglob('*.cshtml'):
    content=rp.read_text(encoding='utf-8-sig',errors='replace')
    first=content.splitlines()[0] if content.splitlines() else ''
    if html_after_model.search(first):
        razor_bad.append(str(rp.relative_to(root)))
check('Razor @model não está colado ao HTML', not razor_bad, ', '.join(razor_bad))


# Database connection / transaction regression
program=text('Naxapa.Web/Program.cs')
check('SQL Server configurado via UseSqlServer direto','UseSqlServer(naxapaConnectionString' in program)
check('Sem EnableRetryOnFailure ativo','EnableRetryOnFailure(' not in re.sub(r'//.*','',program))
check('Connection string LocalDB padrão','(localdb)\\\\MSSQLLocalDB' in text('Naxapa.Web/appsettings.json'))
check('Teste integração Setup/Login presente',(root/'Naxapa.Web.Tests/SetupLoginIntegrationTests.cs').exists())
check('Script criar acesso e testar presente',(root/'scripts/CriarAcessoETestar.ps1').exists())
check('Script validação completa presente',(root/'scripts/Validar-NAXAPA.ps1').exists())

# Core web/auth/API architecture
program=text('Naxapa.Web/Program.cs')
security=text('Naxapa.Web/Security/NaxapaSecurity.cs')
check('Autorização global MVC','AuthorizeFilter' in program)
check('Cookie authentication','NAXAPA.Session' in program and 'AddAuthentication' in program)
check('Bearer API customizado','ApiBearer' in program and 'public const string ApiBearer = "NaxapaApiBearer"' in security)
check('Rate limiting API','AddRateLimiter' in program and 'EnableRateLimiting("api")' in ''.join(p.read_text(errors='ignore') for p in (root/'Naxapa.Web/Controllers/Api').glob('*.cs')))
check('Senha com PasswordHasher','PasswordHasher' in program)
check('OpenAPI mapeado','MapOpenApi' in program)
check('Sem DemoStore',(root/'Naxapa.Web').exists() and not any('DemoStore' in p.read_text(errors='ignore') for p in (root/'Naxapa.Web').rglob('*.cs')))

# API coverage
api_dir=root/'Naxapa.Web/Controllers/Api'
api_text='\n'.join(p.read_text(errors='ignore') for p in api_dir.glob('*.cs'))
for marker in ['/api/v1','products','clients','orders','payment-methods','cash','tables','tabs','print-jobs','auth/token']:
    check(f'API cobre {marker}', (marker in api_text or marker in security) if marker!='auth/token' else ('Route("api/v1/auth")' in api_text and 'HttpPost("token")' in api_text))
check('API usa permissões','NaxapaPermission' in api_text)

# SQL critical structures
sql=text('NaxapaDB_Completo_v3.sql')
for marker in ['CREATE DATABASE NaxapaDB','CREATE TABLE cad.Estabelecimento','CREATE TABLE seg.Usuario','CREATE TABLE cad.Produto','CREATE TABLE op.Pedido','CREATE TABLE fin.Caixa','CREATE TABLE fin.CaixaSessao','CREATE TABLE fin.MovimentoCaixa','CREATE TABLE imp.TrabalhoImpressao','cad.sp_Produto_Listar','op.sp_Pedido_Criar','op.sp_Pedido_AdicionarItem','op.sp_Pedido_AdicionarOpcao','op.sp_Pedido_AlterarStatus','fin.sp_Pagamento_Registrar','fin.sp_Caixa_Abrir','fin.sp_Caixa_Fechar','imp.sp_TrabalhoImpressao_Enfileirar']:
    check(f'SQL contém {marker}',marker in sql)
patch=text('NaxapaDB_Patch_v3.sql')
for marker in ['cfg.SchemaVersion','cad.Insumo','cad.Receita','CAIXA.CADASTRAR','INSUMO.VISUALIZAR','RECEITA.VISUALIZAR']:
    check(f'Patch v3 contém {marker}',marker in patch)

# Order/printing functionality
workflow=text('Naxapa.Web/Services/OrderWorkflowService.cs')
renderer=text('Naxapa.PrintAgent/Services/PrintRenderer.cs')
check('Fila produção automática','EnqueueProductionAsync' in workflow and 'newStatus == "PENDENTE"' in workflow)
check('Comprovante automático','EnqueueReceiptAsync' in workflow and 'newStatus == "FINALIZADO"' in workflow)
check('Impressão idempotente','DeterministicGuid' in workflow and 'HashIdempotencia' in workflow)
check('Renderer produção','type=="PRODUCAO"' in renderer)
check('Renderer comprovante não fiscal','type=="COMPROVANTE"' in renderer and 'DOCUMENTO NAO FISCAL' in renderer)

# PDV real API flows including options
pdv_models=text('Naxapa.Pdv/Models/ApiModels.cs')
pdv_client=text('Naxapa.Pdv/Services/NaxapaApiClient.cs')
pdv_main=text('Naxapa.Pdv/Views/MainWindow.xaml.cs')
check('PDV opções modeladas','ProductOptionGroupDto' in pdv_models and 'CartOptionLine' in pdv_models)
check('PDV consulta detalhe produto','ProductAsync' in pdv_client)
check('PDV envia opções','AddOptionAsync' in pdv_client and 'AddOptionAsync(item.PedidoItemId' in pdv_main)
check('PDV pedidos reais','CreateOrderAsync' in pdv_main and 'ChangeStatusAsync' in pdv_main)
check('PDV pagamentos reais','PayAsync' in pdv_client)
check('PDV caixa real','OpenCashAsync' in pdv_client and 'MoveCashAsync' in pdv_client and 'CloseCashAsync' in pdv_client)
check('PDV mesas/comandas','TablesAsync' in pdv_client and 'TabsAsync' in pdv_client)

# Mobile real/cache flows
mobile='\n'.join(p.read_text(errors='ignore') for p in (root/'Naxapa.Mobile/lib').rglob('*.dart'))
check('Mobile autenticação API','/api/v1/auth/token' in mobile)
check('Mobile produtos API','/api/v1/products' in mobile)
check('Mobile pedidos API','/api/v1/orders' in mobile)
check('Mobile SQLite','sqflite' in text('Naxapa.Mobile/pubspec.yaml') and 'LocalDatabase' in mobile)
check('Mobile adicionais','OptionGroup' in mobile and '/options' in mobile and 'item.options' in mobile)
check('Mobile cache não é banco oficial','SQLite local' in text('Naxapa.Mobile/README.md'))
check('Preparação Android local HTTP','usesCleartextTraffic' in text('scripts/Prepare-Mobile.ps1'))

# Tests/build/deployment
build=text('scripts/Build-All.ps1')
check('Build script valida .NET 10',"StartsWith('10.')" in build)
check('Build script roda testes .NET','dotnet test' in build)
check('Build script roda Flutter analyze/test','flutter analyze' in build and 'flutter test' in build)
check('Build script publica PDV self-contained','--self-contained true' in build and 'Naxapa.Pdv' in build)
check('Build script publica PrintAgent self-contained','Naxapa.PrintAgent' in build and 'PublishSingleFile=true' in build)
check('Validação SQL disponível','THROW' in text('tests/ValidateDatabase.sql'))
check('Backup SQL disponível','BACKUP DATABASE' in text('scripts/Backup-NaxapaDB.ps1'))


# v3.4 requested incremental features
product_controller=text('Naxapa.Web/Controllers/ProdutosController.cs')
product_form=text('Naxapa.Web/Views/Produtos/_Form.cshtml')
setup_model=text('Naxapa.Web/Models/SecurityModels.cs')
setup_controller=text('Naxapa.Web/Controllers/SetupController.cs')
digital_controller=text('Naxapa.Web/Controllers/CardapioDigitalController.cs')
digital_view=text('Naxapa.Web/Views/CardapioDigital/Index.cshtml')
check('GTIN-13 service presente',(root/'Naxapa.Web/Services/Gtin13Service.cs').exists())
check('Gerador GTIN-13 exposto no produto','GerarGtin13' in product_controller and 'generateGtinButton' in product_form)
check('GTIN-13 valida dígito verificador','gtin13Service.IsValid' in product_controller)
check('Upload drag and drop produto','productImageDropzone' in product_form and (root/'Naxapa.Web/wwwroot/js/product-form.js').exists())
check('Upload continua usando storage seguro','SaveImageAsync(model.Imagem, "products"' in product_controller)
check('Setup exige CPF/CNPJ','public string Documento' in setup_model and 'BrazilianDocumentValidator.IsValidCpfOrCnpj' in setup_controller)
check('Setup grava documento no estabelecimento','establishment.Documento = documento' in setup_controller or 'Documento = documento' in setup_controller)
check('Cardápio digital público presente','[AllowAnonymous]' in digital_controller and '[Route("cardapio-digital")]' in digital_controller)
check('Cardápio digital lê produtos reais','db.Produtos.AsNoTracking()' in digital_controller and 'p.DisponivelCardapio' in digital_controller)
check('Cardápio digital mostra imagem e preço','ImagemUrl' in digital_view and 'product.Preco.ToString' in digital_view)
check('Nenhuma tabela nova exigida por v3.4', not (root/'Naxapa.Web/Database/NaxapaDB_Patch_v3_4.sql').exists())

failed=[x for x in checks if not x[1]]
report=[]
report.append('NAXAPA v3 - RELATÓRIO DE TESTES ESTÁTICOS')
report.append('='*52)
report.append(f'Total: {len(checks)} | Aprovados: {len(checks)-len(failed)} | Falhas: {len(failed)}')
report.append('')
for name,ok,detail in checks:
    report.append(f"[{'OK' if ok else 'FALHA'}] {name}" + (f' :: {detail}' if detail else ''))
(root/'RELATORIO_TESTES_ESTATICOS.txt').write_text('\n'.join(report),encoding='utf-8')
print(report[2])
for x in failed[:30]: print('FALHA:',x[0],x[2])
sys.exit(1 if failed else 0)
