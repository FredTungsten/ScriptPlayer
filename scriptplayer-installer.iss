#define Configuration GetEnv('CONFIGURATION')
#if Configuration == ""
#define Configuration "Release"
#endif

#define Version GetEnv('appveyor_build_version')
#if Version == ""
#define Version "x.x.x.x"
#endif

[Setup]
AppName=ScriptPlayer
AppVersion={#Version}
AppPublisher=FredTungsten
AppPublisherURL=github.com/FredTungsten/ScriptPlayer
AppId={{6F331F84-6C70-4E9F-AAFF-8527663E68A5}
;SetupIconFile=ScriptPlayer\Resources\scriptplayer-icon-1.ico
;WizardImageFile=ScriptPlayer\Resources\scriptplayer-logo-1.bmp
;WizardSmallImageFile=ScriptPlayer\Resources\scriptplayer-logo-1.bmp
DefaultDirName={pf}\ScriptPlayer
DefaultGroupName=ScriptPlayer
UninstallDisplayIcon={app}\ScriptPlayer.exe
Compression=lzma2
SolidCompression=yes
OutputBaseFilename=scriptplayer-installer
OutputDir=.\installer
;LicenseFile=LICENSE

[Files]
Source: "ScriptPlayer\ScriptPlayer\bin\{#Configuration}\ScriptPlayer.exe"; DestDir: "{app}"
Source: "ScriptPlayer\ScriptPlayer\bin\{#Configuration}\*.dll"; DestDir: "{app}"
Source: "ScriptPlayer\ScriptPlayer\bin\{#Configuration}\*.config"; DestDir: "{app}"
;Source: "Readme.md"; DestDir: "{app}"; DestName: "Readme.txt"; Flags: isreadme
;Source: "LICENSE"; DestDir: "{app}"; DestName: "License.txt"

[Icons]
Name: "{commonprograms}\ScriptPlayer"; Filename: "{app}\ScriptPlayer.exe"

; Windows 10 15063 Patch BLE security sadness
[Registry]
Root: HKLM; Subkey: "SOFTWARE\Classes\AppID\{{6F331F84-6C70-4E9F-AAFF-8527663E68A5}"; ValueType: binary; ValueName: "AccessPermission"; ValueData: 01 00 04 80 14 00 00 00 24 00 00 00 00 00 00 00 34 00 00 00 01 02 00 00 00 00 00 05 20 00 00 00 20 02 00 00 01 02 00 00 00 00 00 05 20 00 00 00 20 02 00 00 02 00 88 00 06 00 00 00 00 00 14 00 07 00 00 00 01 01 00 00 00 00 00 05 0a 00 00 00 00 00 14 00 03 00 00 00 01 01 00 00 00 00 00 05 12 00 00 00 00 00 18 00 07 00 00 00 01 02 00 00 00 00 00 05 20 00 00 00 20 02 00 00 00 00 18 00 03 00 00 00 01 02 00 00 00 00 00 0f 02 00 00 00 01 00 00 00 00 00 14 00 03 00 00 00 01 01 00 00 00 00 00 05 13 00 00 00 00 00 14 00 03 00 00 00 01 01 00 00 00 00 00 05 14 00 00 00; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Classes\AppID\ScriptPlayer.exe"; ValueType:string; ValueName: "AppID"; ValueData: "{{6F331F84-6C70-4E9F-AAFF-8527663E68A5}"; Flags: uninsdeletekey

[Code]

// Uninstall on install code taken from https://stackoverflow.com/a/2099805/4040754
////////////////////////////////////////////////////////////////////
function GetUninstallString(): String;
var
  sUnInstPath: String;
  sUnInstallString: String;
begin
  sUnInstPath := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#emit SetupSetting("AppId")}_is1');
  sUnInstallString := '';
  if not RegQueryStringValue(HKLM, sUnInstPath, 'UninstallString', sUnInstallString) then
    RegQueryStringValue(HKCU, sUnInstPath, 'UninstallString', sUnInstallString);
  Result := sUnInstallString;
end;


/////////////////////////////////////////////////////////////////////
function IsUpgrade(): Boolean;
begin
  Result := (GetUninstallString() <> '');
end;


/////////////////////////////////////////////////////////////////////
function UnInstallOldVersion(): Integer;
var
  sUnInstallString: String;
  iResultCode: Integer;
begin
// Return Values:
// 1 - uninstall string is empty
// 2 - error executing the UnInstallString
// 3 - successfully executed the UnInstallString

  // default return value
  Result := 0;

  // get the uninstall string of the old app
  sUnInstallString := GetUninstallString();
  if sUnInstallString <> '' then begin
    sUnInstallString := RemoveQuotes(sUnInstallString);
    if Exec(sUnInstallString, '/SILENT /NORESTART /SUPPRESSMSGBOXES','', SW_HIDE, ewWaitUntilTerminated, iResultCode) then
      Result := 3
    else
      Result := 2;
  end else
    Result := 1;
end;

/////////////////////////////////////////////////////////////////////
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep=ssInstall) then
  begin
    if (IsUpgrade()) then
    begin
      UnInstallOldVersion();
    end;
  end;
end;
