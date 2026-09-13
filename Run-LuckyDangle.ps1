$scriptDir = "C:\Users\manas\.gemini\antigravity\scratch\dangling-charm"
$exeFile = Join-Path $scriptDir "LuckyDangle.exe"
$buildScript = Join-Path $scriptDir "Build-LuckyDangle.ps1"

if (-not (Test-Path $exeFile)) {
    Write-Host "Building executable..." -ForegroundColor Yellow
    & powershell -ExecutionPolicy Bypass -File "$buildScript"
}

if (Test-Path $exeFile) {
    Write-Host "Launching Lucky Dangle..." -ForegroundColor Green
    Start-Process "$exeFile"
} else {
    Write-Error "Executable not found."
}
