#define IsService
#define DeleteDirs
#define AllowConfig
#define AppVersion GetFileVersion(AddBackslash(SourcePath) + "..\Bin\Release\" + MainExeFile)

#define AddDll(str sDll) "Source: ""..\Bin\Release\Modules\" + sDll + """; DestDir: ""{app}\Modules""; Flags: ignoreversion;"
#define AddConfig(str sConfig) "Source: ""..\Bin\Release\" + sConfig + """; DestDir: ""{app}""; Flags: ignoreversion;"

[Setup]
AppName={#DisplayName}
AppVerName={#DisplayName} {#AppVersion}
VersionInfoVersion={#AppVersion}
VersionInfoDescription={#AppDetails}
DefaultDirName={pf}\Ribes\{#DisplayName}
DefaultGroupName=Ribes\{#DisplayName}
UninstallDisplayIcon={app}\{#MainExeFile},0
OutputBaseFilename={#SetupFile}
MinVersion=0,5.01
AppId={#ServiceName}
SetupIconFile=..\Files\ApplicationIcon.ico
WizardImageFile=.\Left.bmp
WizardSmallImageFile=.\Top.bmp

[Files]
Source: "..\Bin\Release\*.exe"; DestDir: "{app}"; Flags: ignoreversion; Excludes: "*.vshost.exe"
Source: "..\Bin\Release\*.dll"; DestDir: "{app}"; Flags: ignoreversion
; Source: "..\..\RESOURCES\7z.*"; DestDir: "{app}\7-Zip"; Flags: ignoreversion

[Setup]
#include AddBackslash(SourcePath) + "Utilities.iss"

[Code]
function InitializeSetup(): Boolean;
begin
  Result := IsFramework2Installed(false);
end;
