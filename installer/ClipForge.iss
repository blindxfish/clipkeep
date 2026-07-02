; Inno Setup script for ClipForge — builds a per-user installer around a
; self-contained single-file publish.
;
; Build (per architecture) — pass the target arch and the published exe:
;   ISCC /DMyArch=x64   /DMySourceExe="..\dist\portable-x64\ClipForge.exe"   installer\ClipForge.iss
;   ISCC /DMyArch=x86   /DMySourceExe="..\dist\portable-x86\ClipForge.exe"   installer\ClipForge.iss
;   ISCC /DMyArch=arm64 /DMySourceExe="..\dist\portable-arm64\ClipForge.exe" installer\ClipForge.iss
; Output: installer\ClipForge-Setup-<version>-<arch>.exe

#define MyAppName "ClipForge"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "Nixon Software Solutions"
#define MyAppUrl "https://nixonsolutions.org/"
#define MyAppExeName "ClipForge.exe"

; Defaults so the script still compiles without /D overrides (x64).
#ifndef MyArch
  #define MyArch "x64"
#endif
#ifndef MySourceExe
  #define MySourceExe "..\dist\portable-x64\ClipForge.exe"
#endif

[Setup]
; A stable GUID identifies the app for upgrades/uninstall — do not change it.
AppId={{7C2F6E31-9E4B-4C2A-9B7D-0F5A2C4E9A10}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppUrl}
AppSupportURL={#MyAppUrl}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
; Per-user install — no admin elevation required for a tray utility.
PrivilegesRequired=lowest
OutputDir=.
OutputBaseFilename=ClipForge-Setup-{#MyAppVersion}-{#MyArch}
SetupIconFile=..\src\ClipForge.App\Assets\ClipForge.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
#if MyArch == "x64"
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#elif MyArch == "arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#else
; x86 — 32-bit install, runs on any Windows edition.
#endif

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#MySourceExe}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
