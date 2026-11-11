#define MyAppName "AURUM CONECT"
#define MyAppVersion "1.0.0"
#define MyPublisher "Taller del Joyero · AURUM LAB"

[Setup]
AppId={{C8F4D8A1-5A2B-4D8E-9C31-7B7E2A4D9016}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyPublisher}
DefaultDirName={localappdata}\AurumLab\AurumConect\Rhino7
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
OutputDir=..\..\..\artifacts
OutputBaseFilename=AURUM-CONECT-Setup-Rhino7-{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName={#MyAppName} para Rhino 7
UninstallDisplayIcon={app}\AurumConect.rhp
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Files]
Source: "..\package\AurumConect.rhp"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\package\Microsoft.Web.WebView2.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\package\Microsoft.Web.WebView2.WinForms.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\package\WebView2Loader.dll"; DestDir: "{app}"; Flags: ignoreversion

[Registry]
Root: HKCU64; Subkey: "Software\McNeel\Rhinoceros\7.0\Plug-Ins\{#MyPluginGuid}"; ValueType: string; ValueName: "Name"; ValueData: "AURUM CONECT"; Flags: uninsdeletekey
Root: HKCU64; Subkey: "Software\McNeel\Rhinoceros\7.0\Plug-Ins\{#MyPluginGuid}"; ValueType: string; ValueName: "FileName"; ValueData: "{app}\AurumConect.rhp"; Flags: uninsdeletekey
Root: HKCU64; Subkey: "Software\McNeel\Rhinoceros\7.0\Plug-Ins\{#MyPluginGuid}"; ValueType: dword; ValueName: "LoadMode"; ValueData: "2"; Flags: uninsdeletekey

[Code]
function Rhino7Installed(): Boolean;
var
  InstallPath: String;
begin
  Result := False;
  if RegQueryStringValue(HKLM64, 'SOFTWARE\McNeel\Rhinoceros\7.0\Install', 'InstallPath', InstallPath) then
    Result := FileExists(InstallPath + '\System\Rhino.exe');
  if (not Result) and RegQueryStringValue(HKCU64, 'SOFTWARE\McNeel\Rhinoceros\7.0\Install', 'InstallPath', InstallPath) then
    Result := FileExists(InstallPath + '\System\Rhino.exe');
end;

function InitializeSetup(): Boolean;
begin
  Result := Rhino7Installed();
  if not Result then
    MsgBox('No se encontró una instalación compatible de Rhino 7 de 64 bits. Instala Rhino 7 y vuelve a ejecutar AURUM CONECT.', mbError, MB_OK);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    MsgBox('AURUM CONECT se instaló y registró en Rhino 7. Cierra y vuelve a abrir Rhino para que cargue el complemento. Después ejecuta el comando AurumConect.', mbInformation, MB_OK);
end;
