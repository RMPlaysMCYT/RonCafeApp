#define MyAppName "RonCafe Launcher"
#define MyAppVersion "1.3.0"
#define MyAppPublisher "Ronnel Mitra"

[Setup]
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={autopf}\RonCafe Launcher
DefaultGroupName=RonCafe Launcher
OutputDir=installer
OutputBaseFilename=RonCafeLauncherSetup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
UninstallDisplayIcon={app}\RonCafeApp.exe
SetupIconFile=Assets\icon.ico

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\RonCafe Launcher"; Filename: "{app}\RonCafeApp.exe"
Name: "{autodesktop}\RonCafe Launcher"; Filename: "{app}\RonCafeApp.exe"

[Run]
Filename: "{app}\RonCafeApp.exe"; Description: "Launch RonCafe Launcher"; Flags: nowait postinstall skipifsilent