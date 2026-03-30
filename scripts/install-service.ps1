# Запустить как Администратор!
$ServiceName = "ElkAgent"
$ExePath = "$PSScriptRoot\..\src\ElkAgent\bin\Release\net8.0-windows\win-x64\publish\ElkAgent.exe"

if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
    Write-Host "Stopping existing service..."
    Stop-Service -Name $ServiceName -Force
    sc.exe delete $ServiceName
    Start-Sleep -Seconds 2
}

Write-Host "Installing $ServiceName as Windows Service..."
New-Service -Name $ServiceName `
            -BinaryPathName $ExePath `
            -DisplayName "ELK Agent" `
            -Description "Collects and sends Windows events to ELK Stack" `
            -StartupType Automatic

Start-Service -Name $ServiceName
Write-Host "Service installed and started!" -ForegroundColor Green
Get-Service -Name $ServiceName