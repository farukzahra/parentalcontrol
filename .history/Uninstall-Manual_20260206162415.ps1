# Desinstalador Manual

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Controle Parental - Desinstalacao" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verificar admin
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERRO: Este script precisa ser executado como Administrador!" -ForegroundColor Red
    Read-Host "Pressione ENTER para sair"
    exit 1
}

$confirm = Read-Host "Deseja realmente desinstalar o Controle Parental? (S/N)"
if ($confirm -ne "S" -and $confirm -ne "s") {
    Write-Host "Desinstalacao cancelada." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "[1/4] Parando servico..." -ForegroundColor Yellow
Stop-Service -Name ParentalControlService -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
Write-Host "      OK" -ForegroundColor Green
Write-Host ""

Write-Host "[2/4] Removendo servico..." -ForegroundColor Yellow
sc.exe delete ParentalControlService
Start-Sleep -Seconds 1
Write-Host "      OK" -ForegroundColor Green
Write-Host ""

Write-Host "[3/4] Removendo arquivos..." -ForegroundColor Yellow
Remove-Item -Path "$env:ProgramFiles\ParentalControl" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "      OK" -ForegroundColor Green
Write-Host ""

Write-Host "[4/4] Removendo atalhos..." -ForegroundColor Yellow
Remove-Item -Path "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\Controle Parental" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "      OK" -ForegroundColor Green
Write-Host ""

$keepConfig = Read-Host "Deseja manter as configuracoes? (S/N)"
if ($keepConfig -ne "S" -and $keepConfig -ne "s") {
    Write-Host ""
    Write-Host "Removendo configuracoes do Registry..." -ForegroundColor Yellow
    Remove-Item -Path "HKLM:\SOFTWARE\ParentalControl" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "      OK" -ForegroundColor Green
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  DESINSTALACAO CONCLUIDA!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

Read-Host "Pressione ENTER para sair"
