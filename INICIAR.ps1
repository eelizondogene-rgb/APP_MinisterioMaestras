Write-Host "Restaurando paquetes..." -ForegroundColor Cyan
dotnet restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Compilando..." -ForegroundColor Cyan
dotnet build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Iniciando Niños para Cristo en http://localhost:5021" -ForegroundColor Green
dotnet run --project NinosParaCristo.API
