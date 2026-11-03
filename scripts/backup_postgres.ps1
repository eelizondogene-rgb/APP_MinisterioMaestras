$pgDump = "C:\Program Files\PostgreSQL\18\bin\pg_dump.exe"
if (!(Test-Path $pgDump)) { Write-Host "No se encontró pg_dump en $pgDump" -ForegroundColor Red; exit 1 }
$secure = Read-Host "Contraseña de postgres" -AsSecureString
$ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
try { $env:PGPASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) }
finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
$folder = Join-Path $PSScriptRoot "respaldos"
New-Item -ItemType Directory -Force -Path $folder | Out-Null
$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$file = Join-Path $folder "NinosParaCristo_$stamp.backup"
& $pgDump -h localhost -p 5432 -U postgres -F c -d NinosParaCristo -f $file
Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
if ($LASTEXITCODE -eq 0) { Write-Host "Respaldo creado: $file" -ForegroundColor Green } else { Write-Host "Falló el respaldo." -ForegroundColor Red }
