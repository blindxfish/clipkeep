; Inno Setup script for ClipForge — builds a per-user installer around the
; self-contained single-file publish in dist\portable.
;
; Build:  "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\ClipForge.iss
; Output: installer\ClipForge-Setup-<version>.exe

#define MyAppName "ClipForge"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "ClipForge"
#define MyAppExeName "ClipForge.exe"

[Setup]
; A stable GUID identifies the app for upgrades/uninstall — do not change it.
AppId={{7C2F6E31-9E4B-4C2A-9B7D-0F5A2C4E9A10}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
; Per-user install — no admin elevation required for a tray utility.
PrivilegesRequired=lowest
OutputDir=.
OutputBaseFilename=ClipForge-Setup-{#MyAppVersion}
SetupIconFile=..\src\ClipForge.App\Assets\ClipForge.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "..\dist\portable\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
