# Script para compilar e criar o instalador
# Requer Inno Setup instalado

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Controle Parental - Build Installer  " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. Limpar builds anteriores
Write-Host "[1/5] Limpando builds anteriores..." -ForegroundColor Yellow
Remove-Item -Path ".\ParentalControl.Service\bin\Release" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path ".\ParentalControl.ConfigApp\bin\Release" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path ".\Installer" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "      OK" -ForegroundColor Green
Write-Host ""

# 2. Compilar o Service
Write-Host "[2/5] Compilando Windows Service..." -ForegroundColor Yellow
dotnet publish ParentalControl.Service\ParentalControl.Service.csproj -c Release -r win-x64 --self-contained false
if ($LASTEXITCODE -ne 0) {
    Write-Host "      ERRO ao compilar o Service!" -ForegroundColor Red
    Read-Host "Pressione ENTER para sair"
    exit 1
}
Write-Host "      OK" -ForegroundColor Green
Write-Host ""

# 3. Compilar o ConfigApp
Write-Host "[3/5] Compilando aplicativo de configuracao..." -ForegroundColor Yellow
dotnet publish ParentalControl.ConfigApp\ParentalControl.ConfigApp.csproj -c Release -r win-x64 --self-contained false
if ($LASTEXITCODE -ne 0) {
    Write-Host "      ERRO ao compilar o ConfigApp!" -ForegroundColor Red
    Read-Host "Pressione ENTER para sair"
    exit 1
}
Write-Host "      OK" -ForegroundColor Green
Write-Host ""

# 4. Verificar Inno Setup
Write-Host "[4/5] Verificando Inno Setup..." -ForegroundColor Yellow
$isccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $isccPath)) {
    Write-Host "      Inno Setup nao encontrado!" -ForegroundColor Red
    Write-Host ""
    Write-Host "      Baixe e instale o Inno Setup 6:" -ForegroundColor Yellow
    Write-Host "      https://jrsoftware.org/isdl.php" -ForegroundColor Cyan
    Write-Host ""
    Read-Host "Pressione ENTER para abrir o site e sair"
    Start-Process "https://jrsoftware.org/isdl.php"
    exit 1
}
Write-Host "      OK" -ForegroundColor Green
Write-Host ""

# 5. Criar instalador com Inno Setup
Write-Host "[5/5] Criando instalador..." -ForegroundColor Yellow
& $isccPath "Setup.iss"
if ($LASTEXITCODE -ne 0) {
    Write-Host "      ERRO ao criar instalador!" -ForegroundColor Red
    Read-Host "Pressione ENTER para sair"
    exit 1
}
Write-Host "      OK" -ForegroundColor Green
Write-Host ""

# Sucesso!
Write-Host "========================================" -ForegroundColor Green
Write-Host "  INSTALADOR CRIADO COM SUCESSO!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Arquivo gerado:" -ForegroundColor Cyan
$installerFile = Get-ChildItem -Path ".\Installer\*.exe" | Select-Object -First 1
Write-Host "  $($installerFile.FullName)" -ForegroundColor White
Write-Host ""
Write-Host "Tamanho: $([math]::Round($installerFile.Length / 1MB, 2)) MB" -ForegroundColor Gray
Write-Host ""

Read-Host "Pressione ENTER para sair"
