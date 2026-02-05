# Script de Instalacao do Servico de Controle Parental
# Execute este script como Administrador

Write-Host "Instalando Servico de Controle Parental..." -ForegroundColor Cyan
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
        Write-Host "Executavel encontrado: $path" -ForegroundColor Green
        break
    }
}

if (-not $exePath) {
    Write-Host "Erro: Executavel nao encontrado em nenhum dos locais:" -ForegroundColor Red
    foreach ($path in $possiblePaths) {
        Write-Host "   $path" -ForegroundColor Yellow
    }
    Write-Host ""
    Write-Host "Execute primeiro: dotnet build ParentalControl.Service\ParentalControl.Service.csproj" -ForegroundColor Yellow
    Read-Host "Pressione ENTER para sair"
    exit 1
}

Write-Host ""

# Verificar se ja existe
$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

if ($existingService) {
    Write-Host "Servico ja existe. Parando e removendo..." -ForegroundColor Yellow
    
    # Parar se estiver rodando
    if ($existingService.Status -eq 'Running') {
        Stop-Service -Name $serviceName -Force
        Start-Sleep -Seconds 2
    }
    
    # Remover servico
    sc.exe delete $serviceName
    Start-Sleep -Seconds 2
    Write-Host "Servico antigo removido" -ForegroundColor Green
    Write-Host ""
}

# Instalar novo servico
Write-Host "Instalando servico..." -ForegroundColor Cyan

$createResult = New-Service -Name $serviceName -BinaryPathName $exePath -DisplayName $serviceDisplayName -Description $serviceDescription -StartupType Automatic

if ($?) {
    Write-Host "Servico instalado com sucesso!" -ForegroundColor Green
} else {
    Write-Host "Erro ao instalar servico" -ForegroundColor Red
    Read-Host "Pressione ENTER para sair"
    exit 1
}

Write-Host ""

# Configurar recuperacao automatica em caso de falha
Write-Host "Configurando recuperacao automatica..." -ForegroundColor Cyan

sc.exe failure $serviceName reset= 86400 actions= restart/5000/restart/10000/restart/30000

Write-Host "Recuperacao automatica configurada" -ForegroundColor Green
Write-Host ""

# Iniciar servico
Write-Host "Iniciando servico..." -ForegroundColor Cyan

Start-Service -Name $serviceName

Start-Sleep -Seconds 3

$service = Get-Service -Name $serviceName

if ($service.Status -eq 'Running') {
    Write-Host "Servico iniciado com sucesso!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Status do Servico:" -ForegroundColor Cyan
    Write-Host "   Nome: $($service.Name)" -ForegroundColor White
    Write-Host "   Status: $($service.Status)" -ForegroundColor Green
    Write-Host "   Tipo de Inicio: Automatico" -ForegroundColor White
    Write-Host ""
    Write-Host "O servico agora esta monitorando o tempo de uso!" -ForegroundColor Green
} else {
    Write-Host "Aviso: Servico instalado mas nao foi iniciado automaticamente" -ForegroundColor Yellow
    Write-Host "Status: $($service.Status)" -ForegroundColor Yellow
}

Write-Host ""
Read-Host "Pressione ENTER para fechar"
