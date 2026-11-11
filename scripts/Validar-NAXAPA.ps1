param(
    [switch]$ComSmokeTest,
    [string]$BaseUrl = "http://localhost:5180"
)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    Write-Host "== NAXAPA: validação completa ==" -ForegroundColor Cyan
    $version=& dotnet --version
    if(-not $version.StartsWith('10.')){ throw "É necessário .NET 10 SDK. Encontrado: $version" }

    Remove-Item -Recurse -Force .\Naxapa.Web\bin,.\Naxapa.Web\obj,.\Naxapa.Web.Tests\bin,.\Naxapa.Web.Tests\obj -ErrorAction SilentlyContinue
    dotnet restore .\NAXAPA.sln
    if($LASTEXITCODE -ne 0){ throw 'dotnet restore falhou.' }

    dotnet build .\Naxapa.Web\Naxapa.Web.csproj -c Debug --no-restore
    if($LASTEXITCODE -ne 0){ throw 'Build da Gestão/API falhou.' }

    dotnet test .\Naxapa.Web.Tests\Naxapa.Web.Tests.csproj -c Debug
    if($LASTEXITCODE -ne 0){ throw 'Testes .NET falharam.' }

    python .\tests\static_validate.py
    if($LASTEXITCODE -ne 0){ throw 'Validação estática falhou.' }

    if($ComSmokeTest){
        & .\scripts\Test-Naxapa.ps1 -BaseUrl $BaseUrl
        if($LASTEXITCODE -ne 0){ throw 'Smoke test falhou.' }
    }

    Write-Host "VALIDAÇÃO APROVADA." -ForegroundColor Green
}
finally { Pop-Location }
