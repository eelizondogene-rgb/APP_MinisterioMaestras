param([Parameter(Mandatory=$true)][string]$Archivo)
$pgRestore = "C:\Program Files\PostgreSQL\18\bin\pg_restore.exe"
if (!(Test-Path $pgRestore)) { Write-Host "No se encontró pg_restore en $pgRestore" -ForegroundColor Red; exit 1 }
if (!(Test-Path $Archivo)) { Write-Host "No existe: $Archivo" -ForegroundColor Red; exit 1 }
$secure = Read-Host "Contraseña de postgres" -AsSecureString
$ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
try { $env:PGPASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) }
finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
& $pgRestore -h localhost -p 5432 -U postgres -d NinosParaCristo --clean --if-exists $Archivo
Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
