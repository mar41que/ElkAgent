$ServiceName = "ElkAgent"
if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
    Stop-Service -Name $ServiceName -Force
    sc.exe delete $ServiceName
    Write-Host "Service removed." -ForegroundColor Yellow
} else {
    Write-Host "Service not found." -ForegroundColor Red
}