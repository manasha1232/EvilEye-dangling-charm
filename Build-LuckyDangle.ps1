$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
if (-not $scriptDir) { $scriptDir = "C:\Users\manas\.gemini\antigravity\scratch\dangling-charm" }

$csFile = Join-Path $scriptDir "LuckyDangle.cs"
$exeFile = Join-Path $scriptDir "LuckyDangle.exe"
$icoFile = Join-Path $scriptDir "LuckyDangle.ico"

$cscPaths = @(
    "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

$csc = $null
foreach ($path in $cscPaths) {
    if (Test-Path $path) {
        $csc = $path
        break
    }
}

if (-not $csc) {
    Write-Error "Could not find csc.exe compiler path."
    exit 1
}

$wpfDir = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF"
if (-not (Test-Path $wpfDir)) {
    $wpfDir = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\WPF"
}

Write-Host "Compiling WPF LuckyDangle.cs with custom icon..." -ForegroundColor Cyan

$references = "System.dll,System.Core.dll,System.Drawing.dll,System.Windows.Forms.dll,WindowsBase.dll,PresentationCore.dll,PresentationFramework.dll,System.Xaml.dll"

& $csc /target:winexe /lib:"$wpfDir" /win32icon:"$icoFile" /r:$references /out:"$exeFile" "$csFile"

if ($LASTEXITCODE -eq 0 -and (Test-Path $exeFile)) {
    Write-Host "Successfully compiled WPF LuckyDangle.exe with custom icon!" -ForegroundColor Green
} else {
    Write-Error "Compilation failed."
}
