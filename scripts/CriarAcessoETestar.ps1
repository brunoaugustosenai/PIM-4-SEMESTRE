param(
    [string]$BaseUrl = "http://localhost:5180",
    [string]$Estabelecimento = "NAXAPA",
    [string]$CodigoEstabelecimento = "MATRIZ",
    [string]$Documento = "11222333000181",
    [string]$Nome = "Administrador NAXAPA",
    [string]$Usuario = "admin",
    [string]$Email = "admin@naxapa.local",
    [string]$Senha
)

$ErrorActionPreference = 'Stop'

function Get-AntiForgeryToken {
    param([string]$Html)
    $m = [regex]::Match($Html, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"')
    if (-not $m.Success) {
        throw "Token antiforgery não encontrado na página."
    }
    return [System.Net.WebUtility]::HtmlDecode($m.Groups[1].Value)
}

function New-RandomPassword {
    $bytes = New-Object byte[] 18
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    $raw = [Convert]::ToBase64String($bytes).TrimEnd('=')
    return "Nx!" + $raw + "9a"
}

function Invoke-NaxapaWebRequest {
    param(
        [Parameter(Mandatory=$true)][string]$Uri,
        [string]$Method = 'Get',
        [hashtable]$Body,
        [Microsoft.PowerShell.Commands.WebRequestSession]$WebSession,
        [int]$MaximumRedirection = 10
    )
    $args = @{ Uri=$Uri; Method=$Method; MaximumRedirection=$MaximumRedirection }
    if ($Body) { $args.Body = $Body }
    if ($WebSession) { $args.WebSession = $WebSession }
    if ($PSVersionTable.PSVersion.Major -lt 6) { $args.UseBasicParsing = $true }
    Invoke-WebRequest @args
}

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host " NAXAPA - CRIAR ACESSO E SMOKE TEST" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl"

# 1. Health check
try {
    $health = Invoke-RestMethod -Uri "$BaseUrl/health" -Method Get -TimeoutSec 15
    if ($health.status -ne 'ok') { throw "Health check retornou estado inesperado." }
    Write-Host "[OK] Aplicação e banco respondendo." -ForegroundColor Green
}
catch {
    Write-Host "[FALHA] /health não respondeu corretamente." -ForegroundColor Red
    throw
}

$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

# 2. Verificar Setup
$setup = Invoke-NaxapaWebRequest -Uri "$BaseUrl/Setup" -WebSession $session -MaximumRedirection 10

if ($setup.StatusCode -eq 200 -and $setup.Content -match 'Criar administrador') {
    if ([string]::IsNullOrWhiteSpace($Senha)) {
        $Senha = New-RandomPassword
    }

    $token = Get-AntiForgeryToken $setup.Content
    $body = @{
        __RequestVerificationToken = $token
        Estabelecimento = $Estabelecimento
        CodigoEstabelecimento = $CodigoEstabelecimento
        Documento = $Documento
        Nome = $Nome
        UserName = $Usuario
        Email = $Email
        Password = $Senha
        ConfirmPassword = $Senha
    }

    $created = Invoke-NaxapaWebRequest -Uri "$BaseUrl/Setup" -Method Post -Body $body -WebSession $session -MaximumRedirection 10
    if ($created.StatusCode -ne 200) {
        throw "Criação do administrador retornou HTTP $($created.StatusCode)."
    }

    Write-Host "[OK] Primeiro administrador criado." -ForegroundColor Green
    Write-Host ""
    Write-Host "ACESSO CRIADO:" -ForegroundColor Yellow
    Write-Host "Usuário: $Usuario" -ForegroundColor Yellow
    Write-Host "E-mail : $Email" -ForegroundColor Yellow
    Write-Host "Senha  : $Senha" -ForegroundColor Yellow
    Write-Host ""
}
elseif ($setup.StatusCode -eq 200 -and $setup.Content -match 'NAXAPA - Entrar|<h1>Entrar</h1>') {
    Write-Host "[INFO] Já existe usuário no banco. O Setup está corretamente bloqueado." -ForegroundColor DarkYellow
    if ([string]::IsNullOrWhiteSpace($Senha)) {
        $secure = Read-Host "Informe a senha do usuário '$Usuario' para testar o login" -AsSecureString
        $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
        try { $Senha = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) }
        finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
    }
}
else {
    throw "Resposta inesperada de /Setup: HTTP $($setup.StatusCode)."
}

# 3. Testar login em uma sessão limpa
$loginSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$loginPage = Invoke-NaxapaWebRequest -Uri "$BaseUrl/Conta/Login" -WebSession $loginSession -MaximumRedirection 10
$loginToken = Get-AntiForgeryToken $loginPage.Content
$loginBody = @{
    __RequestVerificationToken = $loginToken
    Login = $Usuario
    Password = $Senha
    RememberMe = 'false'
    ReturnUrl = ''
}

$loginResult = Invoke-NaxapaWebRequest -Uri "$BaseUrl/Conta/Login" -Method Post -Body $loginBody -WebSession $loginSession -MaximumRedirection 10
if ($loginResult.StatusCode -ne 200) {
    throw "Login retornou HTTP $($loginResult.StatusCode)."
}
if ($loginResult.Content -match 'Usuário ou senha inválidos') {
    throw "Login rejeitado."
}
Write-Host "[OK] Login validado." -ForegroundColor Green

# 4. Testar páginas autenticadas essenciais
$rotas = @(
    '/',
    '/Produtos',
    '/Clientes',
    '/Cardapio',
    '/Pedidos',
    '/Caixa',
    '/Configuracoes'
)
foreach ($rota in $rotas) {
    $r = Invoke-NaxapaWebRequest -Uri ($BaseUrl + $rota) -WebSession $loginSession -MaximumRedirection 10
    if ($r.StatusCode -ne 200) {
        throw "Rota $rota falhou com HTTP $($r.StatusCode)."
    }
    Write-Host "[OK] $rota" -ForegroundColor Green
}

Write-Host ""
Write-Host "SMOKE TEST APROVADO." -ForegroundColor Green
Write-Host "O acesso foi criado/testado contra o NaxapaDB desta máquina." -ForegroundColor Green
