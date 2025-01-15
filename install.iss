#define SourceDir "C:\Users\Kyle\Source\djinn"

[Setup]
AppId={{15B0BFB2-7A36-43D3-90C7-B714DCDC1F19}
AppName=Djinn
AppVersion=0.1.0
AppVerName=Djinn
AppPublisher=Kyle Kestell
AppPublisherURL=https://github.com/kkestell/djinn
AppSupportURL=https://github.com/kkestell/djinn
AppUpdatesURL=https://github.com/kkestell/djinn
DefaultDirName={autopf}\Djinn
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
LicenseFile={#SourceDir}\LICENSE
PrivilegesRequired=lowest
OutputDir={#SourceDir}\publish
OutputBaseFilename=Djinn_0.1.0_Setup
SetupIconFile={#SourceDir}\assets\icon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\Djinn.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "powershellshortcut"; Description: "Create desktop shortcut to PowerShell with Djinn in PATH (recommended)"; GroupDescription: "Optional shortcuts:"

[Files]
Source: "{#SourceDir}\publish\Djinn.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{userdesktop}\Djinn PowerShell"; Filename: "powershell.exe"; Parameters: "-NoExit -Command ""cd $HOME; $env:PATH = '{app}' + ';' + $env:PATH"""; Tasks: powershellshortcut
