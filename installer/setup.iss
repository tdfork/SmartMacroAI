; SmartMacroAI Installer Script - Inno Setup
; Created by Phạm Duy – Giải pháp tự động hóa thông minh.

#define MyAppName "SmartMacroAI"
#define MyAppVersion "1.5.7"
#define MyAppPublisher "Phạm Duy"
#define MyAppURL "https://github.com/phamduy/SmartMacroAI"
#define MyAppExeName "SmartMacroAI.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=..\LICENSE.txt
OutputDir=..\release
OutputBaseFilename=SmartMacroAI-v{#MyAppVersion}-Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=..\Assets\logo.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "Start with Windows"; GroupDescription: "Additional options:"

[Files]
Source: "..\publish\SmartMacroAI-v1.5.7\SmartMacroAI.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\SmartMacroAI-v1.5.7\SmartMacroAI.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\SmartMacroAI-v1.5.7\.playwright\*"; DestDir: "{app}\.playwright"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: playwright

[Components]
Name: "main"; Description: "SmartMacroAI Core (required)"; Types: full compact custom; Flags: fixed
Name: "playwright"; Description: "Web Automation Support (Playwright browsers)"; Types: full

[Types]
Name: "full"; Description: "Full installation"
Name: "compact"; Description: "Compact installation (no web automation)"
Name: "custom"; Description: "Custom installation"; Flags: iscustom

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "SmartMacroAI"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\.playwright"
Type: filesandordirs; Name: "{app}\logs"
Type: filesandordirs; Name: "{app}\macros"
