; Instalador do Controle Parental
; Requer Inno Setup 6.x (https://jrsoftware.org/isinfo.php)

#define MyAppName "Controle Parental"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "farukzahra"
#define MyAppURL "https://github.com/farukzahra/parentalcontrol"
#define MyAppExeName "ParentalControl.ConfigApp.exe"
#define MyServiceName "ParentalControlService"
#define MyServiceExe "ParentalControl.Service.exe"

[Setup]
AppId={{8B9C3D4E-5F6A-4B7C-8D9E-0F1A2B3C4D5E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=LICENSE
OutputDir=Installer
OutputBaseFilename=ParentalControl-Setup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
UninstallDisplayIcon={app}\Service\{#MyServiceExe}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "portugues"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na Área de Trabalho"; GroupDescription: "Atalhos:"

[Files]
; Serviço
Source: "ParentalControl.Service\bin\Release\net8.0\*"; DestDir: "{app}\Service"; Flags: ignoreversion recursesubdirs createallsubdirs
; Aplicativo de Configuração
Source: "ParentalControl.ConfigApp\bin\Release\net8.0-windows\*"; DestDir: "{app}\ConfigApp"; Flags: ignoreversion recursesubdirs createallsubdirs
; Biblioteca Core (já incluída nos outputs acima, mas garantir)
Source: "LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\ConfigApp\{#MyAppExeName}"; WorkingDir: "{app}\ConfigApp"
Name: "{group}\Desinstalar {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\ConfigApp\{#MyAppExeName}"; WorkingDir: "{app}\ConfigApp"; Tasks: desktopicon

[Run]
; Instalar o serviço Windows
Filename: "sc.exe"; Parameters: "create {#MyServiceName} binPath= ""{app}\Service\{#MyServiceExe}"" start= auto displayname= ""Controle Parental"""; Flags: runhidden
Filename: "sc.exe"; Parameters: "description {#MyServiceName} ""Monitora tempo de uso do computador e aplica limites configurados"""; Flags: runhidden
; Configurar recuperação automática do serviço
Filename: "sc.exe"; Parameters: "failure {#MyServiceName} reset= 86400 actions= restart/5000/restart/10000/restart/30000"; Flags: runhidden
; Iniciar o serviço
Filename: "sc.exe"; Parameters: "start {#MyServiceName}"; Flags: runhidden
; Perguntar se quer abrir o aplicativo
Filename: "{app}\ConfigApp\{#MyAppExeName}"; Description: "Abrir {#MyAppName}"; Flags: nowait postinstall skipifsilent runasoriginaluser

[UninstallRun]
; Parar o serviço
Filename: "sc.exe"; Parameters: "stop {#MyServiceName}"; Flags: runhidden
; Aguardar um pouco
Filename: "{cmd}"; Parameters: "/c timeout /t 3"; Flags: runhidden
; Remover o serviço
Filename: "sc.exe"; Parameters: "delete {#MyServiceName}"; Flags: runhidden

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Registry]
; Limpar configurações ao desinstalar (opcional, comentado por padrão)
; Root: HKLM; Subkey: "SOFTWARE\ParentalControl"; Flags: deletekey uninsdeletekey

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
  
  // Verificar se é Windows 10/11
  if GetWindowsVersion < $0A00 then
  begin
    MsgBox('Este software requer Windows 10 ou superior.', mbError, MB_OK);
    Result := False;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Result := '';
  
  // Verificar se o serviço já existe e parar
  Exec('sc.exe', 'query ' + '{#MyServiceName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if ResultCode = 0 then
  begin
    // Serviço existe, parar
    Exec('sc.exe', 'stop ' + '{#MyServiceName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(2000);
    // Remover serviço antigo
    Exec('sc.exe', 'delete ' + '{#MyServiceName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(1000);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    // Configurar permissões do serviço para que usuários comuns não possam parar
    // SDDL: Deny SERVICE_STOP for Users, Allow everything for Admins
    Exec('sc.exe', 'sdset {#MyServiceName} D:(D;;CCLCSWLORC;;;AU)(A;;CCLCSWRPWPDTLOCRRC;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    // Limpar dados da sessão (opcional)
    RegDeleteKeyIncludingSubkeys(HKLM, 'SOFTWARE\ParentalControl\SessionData');
  end;
end;
