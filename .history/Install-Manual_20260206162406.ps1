# Instalador Manual (sem Inno Setup)
# Use este script se não quiser instalar o Inno Setup

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Controle Parental - Instalacao Manual" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verificar admin
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERRO: Este script precisa ser executado como Administrador!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Clique com botao direito no PowerShell e selecione 'Executar como Administrador'" -ForegroundColor Yellow
    Read-Host "Pressione ENTER para sair"
    exit 1
}

Write-Host "Verificacoes:" -ForegroundColor Cyan
Write-Host "  [OK] Executando como Administrador" -ForegroundColor Green
Write-Host ""

# 1. Compilar
Write-Host "[1/4] Compilando aplicacao..." -ForegroundColor Yellow
dotnet publish ParentalControl.Service\ParentalControl.Service.csproj -c Release -o "$env:ProgramFiles\ParentalControl\Service"
dotnet publish ParentalControl.ConfigApp\ParentalControl.ConfigApp.csproj -c Release -o "$env:ProgramFiles\ParentalControl\ConfigApp"
Write-Host "      OK" -ForegroundColor Green
Write-Host ""

# 2. Parar e remover serviço existente
Write-Host "[2/4] Verificando servico existente..." -ForegroundColor Yellow
$service = Get-Service -Name ParentalControlService -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "      Parando servico existente..." -ForegroundColor Gray
    Stop-Service -Name ParentalControlService -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    Write-Host "      Removendo servico existente..." -ForegroundColor Gray
    sc.exe delete ParentalControlService
    Start-Sleep -Seconds 1
}
Write-Host "      OK" -ForegroundColor Green
Write-Host ""

# 3. Instalar serviço
Write-Host "[3/4] Instalando servico Windows..." -ForegroundColor Yellow
$servicePath = "$env:ProgramFiles\ParentalControl\Service\ParentalControl.Service.exe"
sc.exe create ParentalControlService binPath= "`"$servicePath`"" start= auto displayname= "Controle Parental"
sc.exe description ParentalControlService "Monitora tempo de uso do computador e aplica limites configurados"
sc.exe failure ParentalControlService reset= 86400 actions= restart/5000/restart/10000/restart/30000

# Configurar permissões (usuários comuns não podem parar)
sc.exe sdset ParentalControlService "D:(D;;CCLCSWLORC;;;AU)(A;;CCLCSWRPWPDTLOCRRC;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)"

Write-Host "      OK" -ForegroundColor Green
Write-Host ""

# 4. Iniciar serviço
Write-Host "[4/4] Iniciando servico..." -ForegroundColor Yellow
Start-Service -Name ParentalControlService
Start-Sleep -Seconds 2

$service = Get-Service -Name ParentalControlService
if ($service.Status -eq 'Running') {
    Write-Host "      OK - Servico rodando" -ForegroundColor Green
} else {
    Write-Host "      AVISO: Servico instalado mas nao iniciou" -ForegroundColor Yellow
}
Write-Host ""

# 5. Criar atalhos
Write-Host "[5/5] Criando atalhos..." -ForegroundColor Yellow
$WshShell = New-Object -ComObject WScript.Shell

# Menu Iniciar
$startMenuPath = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\Controle Parental"
New-Item -ItemType Directory -Path $startMenuPath -Force | Out-Null
$shortcut = $WshShell.CreateShortcut("$startMenuPath\Controle Parental.lnk")
$shortcut.TargetPath = "$env:ProgramFiles\ParentalControl\ConfigApp\ParentalControl.ConfigApp.exe"
$shortcut.WorkingDirectory = "$env:ProgramFiles\ParentalControl\ConfigApp"
$shortcut.Save()

Write-Host "      OK" -ForegroundColor Green
Write-Host ""

# Sucesso!
Write-Host "========================================" -ForegroundColor Green
Write-Host "  INSTALACAO CONCLUIDA!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "O servico esta rodando em segundo plano." -ForegroundColor Cyan
Write-Host "Procure por 'Controle Parental' no Menu Iniciar para configurar." -ForegroundColor Cyan
Write-Host ""
Write-Host "Arquivos instalados em:" -ForegroundColor Gray
Write-Host "  $env:ProgramFiles\ParentalControl\" -ForegroundColor White
Write-Host ""

Read-Host "Pressione ENTER para sair"
