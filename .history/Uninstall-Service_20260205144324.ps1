# Script de Desinstalação do Serviço de Controle Parental
# Execute este script como Administrador

Write-Host "🗑️  Desinstalando Serviço de Controle Parental..." -ForegroundColor Cyan
Write-Host ""

$serviceName = "ParentalControlService"

# Verificar se existe
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

if (-not $service) {
    Write-Host "⚠️  Serviço não está instalado" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Pressione qualquer tecla para sair..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 0
}

Write-Host "📊 Serviço encontrado:" -ForegroundColor Cyan
Write-Host "   Nome: $($service.Name)" -ForegroundColor White
Write-Host "   Status: $($service.Status)" -ForegroundColor White
Write-Host ""

# Parar se estiver rodando
if ($service.Status -eq 'Running') {
    Write-Host "⏹️  Parando serviço..." -ForegroundColor Cyan
    Stop-Service -Name $serviceName -Force
    Start-Sleep -Seconds 2
    Write-Host "✅ Serviço parado" -ForegroundColor Green
    Write-Host ""
}

# Remover serviço
Write-Host "🗑️  Removendo serviço..." -ForegroundColor Cyan

sc.exe delete $serviceName

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Serviço removido com sucesso!" -ForegroundColor Green
} else {
    Write-Host "❌ Erro ao remover serviço (código: $LASTEXITCODE)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Pressione qualquer tecla para sair..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
