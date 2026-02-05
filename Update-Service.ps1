# Script para atualizar o servico
Write-Host "Parando servico..." -ForegroundColor Cyan
Stop-Service -Name ParentalControlService -Force

Write-Host "Aguardando..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

Write-Host "Copiando novos arquivos..." -ForegroundColor Cyan
$source = "c:\repo\parentalControl\ParentalControl.Service\bin\Release\net8.0\"
$serviceExe = Get-Service -Name ParentalControlService | Select-Object -ExpandProperty Name
$serviceInfo = Get-WmiObject -Class Win32_Service -Filter "Name='ParentalControlService'"
$dest = Split-Path $serviceInfo.PathName.Trim('"')

Write-Host "Origem: $source" -ForegroundColor Gray
Write-Host "Destino: $dest" -ForegroundColor Gray

Copy-Item -Path "$source*" -Destination $dest -Recurse -Force

Write-Host "Iniciando servico..." -ForegroundColor Cyan
Start-Service -Name ParentalControlService

Start-Sleep -Seconds 2

$service = Get-Service -Name ParentalControlService
if ($service.Status -eq 'Running') {
    Write-Host "Servico atualizado e iniciado com sucesso!" -ForegroundColor Green
} else {
    Write-Host "Erro: Servico nao iniciou. Status: $($service.Status)" -ForegroundColor Red
}

Read-Host "Pressione ENTER para sair"
