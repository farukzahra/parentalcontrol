# Script de Instalação do Serviço de Controle Parental
# Execute este script como Administrador

Write-Host "🛡️ Instalando Serviço de Controle Parental..." -ForegroundColor Cyan
Write-Host ""

$serviceName = "ParentalControlService"
$serviceDisplayName = "Controle Parental"
$serviceDescription = "Monitora tempo de uso do computador e bloqueia ao atingir limite"

# Tentar diferentes caminhos
$possiblePaths = @(
    "$PSScriptRoot\ParentalControl.Service\bin\Publish\ParentalControl.Service.exe",
    "$PSScriptRoot\ParentalControl.Service\bin\Release\net8.0\ParentalControl.Service.exe",
    "$PSScriptRoot\ParentalControl.Service\bin\Debug\net8.0\ParentalControl.Service.exe"
)

$exePath = $null
foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        $exePath = $path
        break
    }
}

if (-not $exePath) {
    Write-Host "❌ Erro: Executável não encontrado em nenhum dos locais:" -ForegroundColor Red
    foreach ($path in $possiblePaths) {
        Write-Host "   $path" -ForegroundColor Yellow
    }
    Write-Host ""
    Write-Host "Execute primeiro: dotnet build ParentalControl.Service\ParentalControl.Service.csproj" -ForegroundColor Yellow
    Write-Host "Pressione qualquer tecla para sair..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}

Write-Host "📁 Executável encontrado: $exePath" -ForegroundColor Green
Write-Host ""

# Verificar se já existe
$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

if ($existingService) {
    Write-Host "⚠️  Serviço já existe. Parando e removendo..." -ForegroundColor Yellow
    
    # Parar se estiver rodando
    if ($existingService.Status -eq 'Running') {
        Stop-Service -Name $serviceName -Force
        Start-Sleep -Seconds 2
    }
    
    # Remover serviço
    sc.exe delete $serviceName
    Start-Sleep -Seconds 2
    Write-Host "✅ Serviço antigo removido" -ForegroundColor Green
    Write-Host ""
}

# Instalar novo serviço
Write-Host "📦 Instalando serviço..." -ForegroundColor Cyan

$createResult = New-Service `
    -Name $serviceName `
    -BinaryPathName $exePath `
    -DisplayName $serviceDisplayName `
    -Description $serviceDescription `
    -StartupType Automatic

if ($?) {
    Write-Host "✅ Serviço instalado com sucesso!" -ForegroundColor Green
} else {
    Write-Host "❌ Erro ao instalar serviço" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Configurar recuperação automática em caso de falha
Write-Host "⚙️  Configurando recuperação automática..." -ForegroundColor Cyan

sc.exe failure $serviceName reset= 86400 actions= restart/5000/restart/10000/restart/30000

Write-Host "✅ Recuperação automática configurada" -ForegroundColor Green
Write-Host ""

# Iniciar serviço
Write-Host "▶️  Iniciando serviço..." -ForegroundColor Cyan

Start-Service -Name $serviceName

Start-Sleep -Seconds 3

$service = Get-Service -Name $serviceName

if ($service.Status -eq 'Running') {
    Write-Host "✅ Serviço iniciado com sucesso!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 Status do Serviço:" -ForegroundColor Cyan
    Write-Host "   Nome: $($service.Name)" -ForegroundColor White
    Write-Host "   Status: $($service.Status)" -ForegroundColor Green
    Write-Host "   Inicialização: Automática" -ForegroundColor White
    Write-Host ""
    Write-Host "🎉 Instalação concluída!" -ForegroundColor Green
    Write-Host ""
    Write-Host "💡 Próximos passos:" -ForegroundColor Yellow
    Write-Host "   1. O serviço está monitorando desde agora" -ForegroundColor White
    Write-Host "   2. Abra o aplicativo de configuração para ver o status" -ForegroundColor White
    Write-Host "   3. O tempo começará a contar desde este momento" -ForegroundColor White
} else {
    Write-Host "❌ Erro: Serviço instalado mas não iniciou" -ForegroundColor Red
    Write-Host "   Status: $($service.Status)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "💡 Verifique os logs em Event Viewer:" -ForegroundColor Yellow
    Write-Host "   Applications and Services Logs > ParentalControl" -ForegroundColor White
}

Write-Host ""
Write-Host "Pressione qualquer tecla para sair..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
