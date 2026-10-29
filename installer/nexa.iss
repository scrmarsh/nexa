#ifndef PublishDir
  #error PublishDir must point to the approved installed-distribution publish output
#endif
#ifndef OutputDirPath
  #define OutputDirPath "..\verification\installer033"
#endif
#ifndef AppVersion
  #define AppVersion "0.0.33"
#endif
#ifdef TestRoot
  #define ProductId "{B80F06DA-5764-44B7-B632-68ACB4BE0330}"
  #define InstallDir TestRoot + "\app"
  #define DesktopDir TestRoot + "\desktop"
  #define MenuDir TestRoot + "\start-menu"
  #define DataDir TestRoot + "\data"
#else
  #define ProductId "{70F24EC8-70DA-4EF6-AF57-68ACB4BE0330}"
  #define InstallDir "{localappdata}\Programs\nexa"
  #define DesktopDir "{userdesktop}"
  #define MenuDir "{userprograms}\nexa"
  #define DataDir "{localappdata}\SessionDashboard"
#endif

[Setup]
AppId={{#ProductId}
AppName=nexa
AppVersion={#AppVersion}
AppVerName=nexa v{#AppVersion} alpha
AppPublisher=scrmarsh
AppPublisherURL=https://github.com/scrmarsh/nexa
AppSupportURL=https://github.com/scrmarsh/nexa/issues
AppUpdatesURL=https://github.com/scrmarsh/nexa/releases
DefaultDirName={#InstallDir}
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupArchitecture=x64
MinVersion=10.0
DisableProgramGroupPage=yes
DisableWelcomePage=no
WizardStyle=modern
SetupIconFile=..\assets\nexa-icon.ico
UninstallDisplayIcon={app}\Nexa.exe
UninstallDisplayName=nexa
OutputDir={#OutputDirPath}
OutputBaseFilename=NexaSetup
VersionInfoVersion={#AppVersion}.0
VersionInfoProductVersion={#AppVersion}.0
VersionInfoProductName=nexa installer
VersionInfoDescription=nexa Windows installer
VersionInfoCompany=scrmarsh
Compression=lzma2
SolidCompression=yes
CloseApplications=yes
CloseApplicationsFilter=Nexa.exe
RestartApplications=no
UsePreviousAppDir=yes
UsePreviousTasks=yes
SetupMutex=Local\nexa.Installer.033
ChangesAssociations=no

[Tasks]
Name: desktopicon; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: checkedonce
Name: oldcopies; Description: "Find and clean up verified old portable copies after backing them up"; GroupDescription: "Previous versions:"
#ifndef TestRoot
Name: fullscan; Description: "Also search local drives (may take several minutes)"; GroupDescription: "Previous versions:"; Flags: unchecked
#endif

[Files]
Source: "{#PublishDir}\Nexa.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{srcexe}"; DestDir: "{app}\setup-cache"; DestName: "NexaSetup.exe"; Flags: external ignoreversion; Check: CacheNeeded

[Icons]
Name: "{#MenuDir}\nexa"; Filename: "{app}\Nexa.exe"; WorkingDir: "{app}"; IconFilename: "{app}\Nexa.exe"
Name: "{#DesktopDir}\nexa"; Filename: "{app}\Nexa.exe"; WorkingDir: "{app}"; IconFilename: "{app}\Nexa.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Nexa.exe"; Description: "Launch nexa"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: files; Name: "{app}\installation.json"
Type: files; Name: "{app}\.nexa-update.lock"

[Code]
var
  PlanPath: String;
  DeleteUserData: Boolean;

function CacheNeeded: Boolean;
begin
  Result := CompareText(ExpandConstant('{srcexe}'), ExpandConstant('{app}\setup-cache\NexaSetup.exe')) <> 0;
end;

function DataRoot: String;
begin
  Result := ExpandConstant('{#DataDir}');
end;

function Quoted(Value: String): String;
begin
  Result := '"' + Value + '"';
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  Code: Integer;
  Args, Roots, Drive: String;
  I: Integer;
begin
  Result := '';
  ExtractTemporaryFile('Nexa.exe');
  PlanPath := ExpandConstant('{tmp}\migration-plan.json');
#ifdef TestRoot
  Roots := Quoted(ExpandConstant('{#TestRoot}\legacy'));
#else
  Roots := Quoted(ExpandConstant('{userdesktop}')) + ' ' + Quoted(ExpandConstant('{%USERPROFILE}\Downloads')) + ' ' + Quoted(ExpandConstant('{userdocs}'));
  if WizardIsTaskSelected('fullscan') then
    for I := Ord('C') to Ord('Z') do begin
      Drive := Chr(I) + ':\';
      if DirExists(Drive) then Roots := Roots + ' ' + Quoted(Drive);
    end;
#endif
  if ExpandConstant('{param:LEGACYSOURCE|}') <> '' then Roots := Roots + ' ' + Quoted(ExpandConstant('{param:LEGACYSOURCE}'));
  Args := '--installer-prepare ' + Quoted(PlanPath) + ' ' + Quoted(DataRoot) + ' ' + Quoted(ExpandConstant('{app}')) + ' ' + Roots;
  if not Exec(ExpandConstant('{tmp}\Nexa.exe'), Args, '', SW_HIDE, ewWaitUntilTerminated, Code) then
    Result := 'Could not start migration checks. Your saves and old copies are unchanged.'
  else if Code <> 0 then
    Result := 'Could not safely back up existing saves. Close nexa and try again. Details: ' + PlanPath + '.error';
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Code: Integer;
  Choice, Marker, JsonData: String;
begin
  if CurStep = ssPostInstall then begin
    JsonData := DataRoot;
    StringChangeEx(JsonData, '\', '\\', True);
    Marker := '{"AppId":"{#ProductId}","Scope":"CurrentUser","DataRoot":"' + JsonData + '"}';
    if not SaveStringToFile(ExpandConstant('{app}\installation.json'), Marker, False) then
      RaiseException('Could not write the installation identity. Rerun Setup to repair.');
    Choice := 'keep';
    if WizardIsTaskSelected('oldcopies') then Choice := 'remove';
    if not Exec(ExpandConstant('{app}\Nexa.exe'), '--installer-complete ' + Quoted(PlanPath) + ' ' + Choice, '', SW_HIDE, ewWaitUntilTerminated, Code) then
      RaiseException('Could not finish migration. Old copies were preserved.');
    if Code <> 0 then RaiseException('Migration did not finish. Old copies were preserved.');
    ForceDirectories(DataRoot + '\installation-reports');
    CopyFile(PlanPath, DataRoot + '\installation-reports\latest-migration.json', False);
  end;
end;

function InitializeUninstall: Boolean;
begin
  DeleteUserData := False;
  Result := True;
  if UninstallSilent then begin
    DeleteUserData := (ExpandConstant('{param:REMOVEUSERDATA|0}') = '1') and (ExpandConstant('{param:CONFIRMREMOVEDATA|NO}') = 'YES');
  end else begin
    DeleteUserData := MsgBox('Remove all local nexa data?' + #13#10 + #13#10 + 'This permanently removes Spaces, sessions, notes, settings, history, local recordings, transcripts and local account tokens. Connected cloud content is not deleted.' + #13#10 + #13#10 + 'Choose No to keep your Spaces, sessions, settings and history (recommended).', mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES;
    if DeleteUserData then
      Result := MsgBox('Permanently delete local nexa data from ' + DataRoot + '?', mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES;
  end;
end;

function FileAttributes(Name: String): LongWord;
  external 'GetFileAttributesW@kernel32.dll stdcall';

function RegularTree(Path: String): Boolean;
var
  Entry: TFindRec;
begin
  Result := False;
  if (FileAttributes(Path) and $400) <> 0 then Exit;
  Result := True;
  if FindFirst(AddBackslash(Path) + '*', Entry) then begin
    try
      repeat
        if (Entry.Name <> '.') and (Entry.Name <> '..') then begin
          if (Entry.Attributes and $400) <> 0 then begin Result := False; Exit; end;
          if (Entry.Attributes and $10) <> 0 then
            if not RegularTree(AddBackslash(Path) + Entry.Name) then begin Result := False; Exit; end;
        end;
      until not FindNext(Entry);
    finally FindClose(Entry); end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if (CurUninstallStep = usPostUninstall) and DeleteUserData and DirExists(DataRoot) then begin
    if (Length(DataRoot) < 20) or not RegularTree(DataRoot) then
      RaiseException('Local data was preserved because its folder contains links or an unexpected path. Inspect ' + DataRoot);
    if not DelTree(DataRoot, True, True, True) then
      RaiseException('Some local data could not be removed. Inspect ' + DataRoot);
  end;
end;
