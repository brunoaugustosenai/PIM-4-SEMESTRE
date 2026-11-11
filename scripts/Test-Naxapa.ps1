param(
  [string]$BaseUrl = "http://localhost:5180",
  [string]$Login = $env:NAXAPA_TEST_LOGIN,
  [string]$Password = $env:NAXAPA_TEST_PASSWORD,
  [Nullable[long]]$EstabelecimentoId = $null
)
$ErrorActionPreference='Stop'
function Check($name,[scriptblock]$action){Write-Host "[TESTE] $name" -ForegroundColor Cyan;& $action;Write-Host "[OK] $name" -ForegroundColor Green}
Check 'Home responde' { $r=Invoke-WebRequest "$BaseUrl/Conta/Login" -UseBasicParsing; if($r.StatusCode-ne 200){throw "HTTP $($r.StatusCode)"} }
Check 'OpenAPI responde' { $r=Invoke-RestMethod "$BaseUrl/openapi/v1.json"; if(-not $r.paths){throw 'OpenAPI sem paths'} }
if([string]::IsNullOrWhiteSpace($Login)-or[string]::IsNullOrWhiteSpace($Password)){Write-Warning 'Credenciais não informadas. Testes autenticados foram ignorados. Defina NAXAPA_TEST_LOGIN e NAXAPA_TEST_PASSWORD.';exit 0}
$body=@{login=$Login;password=$Password;estabelecimentoId=$EstabelecimentoId}|ConvertTo-Json
$session=Invoke-RestMethod "$BaseUrl/api/v1/auth/token" -Method Post -ContentType 'application/json' -Body $body
if(-not $session.token){throw 'API não retornou token'}
$headers=@{Authorization="Bearer $($session.token)"}
Check 'Produtos autenticados' { $r=Invoke-RestMethod "$BaseUrl/api/v1/products?page=1&pageSize=5" -Headers $headers; if($null-eq$r.items){throw 'Resposta sem items'} }
Check 'Pedidos autenticados' { $r=Invoke-RestMethod "$BaseUrl/api/v1/orders?page=1&pageSize=5" -Headers $headers; if($null-eq$r.items){throw 'Resposta sem items'} }
Check 'Token pode ser revogado' { Invoke-RestMethod "$BaseUrl/api/v1/auth/revoke" -Method Post -Headers $headers | Out-Null }
Write-Host 'Smoke tests concluídos.' -ForegroundColor Green
