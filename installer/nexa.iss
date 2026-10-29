#ifndef PublishDir
  #error PublishDir must point to the approved installed-distribution publish output
#endif
#ifdef FixtureHelper
  #ifndef TestRoot
    #error A separate maintenance helper is permitted only for a test-root fixture
  #endif
  #define PrepareHelper "nexa-maintenance.exe"
#else
  #define PrepareHelper "Nexa.exe"
#endif
#ifndef OutputDirPath
  #define OutputDirPath "..\verification\installer035"
#endif
#ifndef AppVersion
  #define AppVersion "0.0.35"
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
  #define DataDir "{localappdata}\Nexa"
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
; Follow Windows appearance at launch, including controls, dialogs and title bars.
WizardStyle=modern dynamic polar includetitlebar hidebevels
WizardBackColor=#EFF3F8
WizardBackColorDynamicDark=#080B16
WizardImageBackColor=#EFF3F8
WizardImageBackColorDynamicDark=#080B16
WizardSmallImageBackColor=#EFF3F8
WizardSmallImageBackColorDynamicDark=#080B16
SetupIconFile=..\assets\nexa-icon.ico
WizardImageFile=..\assets\nexa-setup-classic.png
WizardImageFileDynamicDark=..\assets\nexa-setup-classic-dark.png
WizardSmallImageFile=..\assets\nexa-setup-small.png
WizardSmallImageFileDynamicDark=..\assets\nexa-setup-small-dark.png
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
SetupMutex=Local\nexa.Installer
ChangesAssociations=no

[Messages]
WelcomeLabel1=Welcome to the Nexa Setup Wizard
InstallingLabel=Installing Nexa

[LangOptions]
DialogFontName=Segoe UI
DialogFontSize=10
WelcomeFontName=Segoe UI
WelcomeFontSize=16

[Tasks]
Name: desktopicon; Description: "Create Desktop Shortcut"; GroupDescription: "Shortcuts:"
Name: startmenu; Description: "Add to Start Menu"; GroupDescription: "Shortcuts:"

[Files]
Source: "{#PublishDir}\Nexa.exe"; DestDir: "{app}"; Flags: ignoreversion; Check: PayloadNeeded
#ifdef FixtureHelper
Source: "{#FixtureHelper}"; DestDir: "{tmp}"; DestName: "nexa-maintenance.exe"; Flags: dontcopy
#endif
Source: "{srcexe}"; DestDir: "{app}\setup-cache"; DestName: "NexaSetup.exe"; Flags: external ignoreversion; Check: CacheNeeded

[Icons]
Name: "{#MenuDir}\nexa"; Filename: "{app}\Nexa.exe"; WorkingDir: "{app}"; IconFilename: "{app}\Nexa.exe"; Tasks: startmenu
Name: "{#DesktopDir}\nexa"; Filename: "{app}\Nexa.exe"; WorkingDir: "{app}"; IconFilename: "{app}\Nexa.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Nexa.exe"; Description: "Launch nexa"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: files; Name: "{app}\installation.json"
Type: files; Name: "{app}\installer-integrity.json"
Type: files; Name: "{app}\.nexa-update.lock"

[Code]
var
  PlanPath: String;
  DeleteUserData: Boolean;
  DiscoveryPage: TOutputMsgMemoWizardPage;
  DiscoveryMemo: TNewMemo;
  SearchProgress: TOutputProgressWizardPage;
  ModePage: TInputOptionWizardPage;
  Prepared, Preparing: Boolean;
  PreparationError: String;
  DesktopShortcutCheck, StartMenuShortcutCheck: TNewCheckBox;
  UpdatingShortcutChoices: Boolean;

procedure ShortcutChoiceChanged(Sender: TObject);
var Choices: String;
begin
  if UpdatingShortcutChoices then Exit;
  if DesktopShortcutCheck.Checked then Choices := 'desktopicon'
  else Choices := '!desktopicon';
  if StartMenuShortcutCheck.Checked then Choices := Choices + ',startmenu'
  else Choices := Choices + ',!startmenu';
  WizardSelectTasks(Choices);
end;

procedure CreateShortcutChoices;
begin
  // Native, focusable checkboxes expose each checked state to accessibility tools.
  DesktopShortcutCheck := TNewCheckBox.Create(WizardForm);
  DesktopShortcutCheck.Parent := WizardForm.SelectTasksPage;
  DesktopShortcutCheck.SetBounds(WizardForm.TasksList.Left, WizardForm.TasksList.Top + ScaleY(8), WizardForm.TasksList.Width, ScaleY(32));
  DesktopShortcutCheck.Caption := 'Add to Desktop';
  DesktopShortcutCheck.OnClick := @ShortcutChoiceChanged;
  StartMenuShortcutCheck := TNewCheckBox.Create(WizardForm);
  StartMenuShortcutCheck.Parent := WizardForm.SelectTasksPage;
  StartMenuShortcutCheck.SetBounds(WizardForm.TasksList.Left, DesktopShortcutCheck.Top + ScaleY(44), WizardForm.TasksList.Width, ScaleY(32));
  StartMenuShortcutCheck.Caption := 'Add to Start Menu';
  StartMenuShortcutCheck.OnClick := @ShortcutChoiceChanged;
  WizardForm.TasksList.Visible := False;
end;

function PayloadNeeded: Boolean;
begin
  Result := True;
  if FileExists(ExpandConstant('{app}\Nexa.exe')) then
    try Result := CompareText(GetSHA256OfFile(ExpandConstant('{app}\Nexa.exe')), '{#GetSHA256OfFile(PublishDir + "\Nexa.exe")}') <> 0;
    except Result := True; end;
end;

function CacheNeeded: Boolean;
begin
  Result := CompareText(ExpandConstant('{srcexe}'), ExpandConstant('{app}\setup-cache\NexaSetup.exe')) <> 0;
  if Result and FileExists(ExpandConstant('{app}\setup-cache\NexaSetup.exe')) then
    try Result := CompareText(GetSHA256OfFile(ExpandConstant('{srcexe}')), GetSHA256OfFile(ExpandConstant('{app}\setup-cache\NexaSetup.exe'))) <> 0;
    except Result := True; end;
end;

function DataRoot: String;
begin
  Result := ExpandConstant('{#DataDir}');
end;

function Quoted(Value: String): String;
begin
  Result := '"' + Value + '"';
end;

procedure PreparationLog(const S: String; const Error, FirstLine: Boolean);
begin
  SearchProgress.SetText(S, 'Checking bounded nexa locations; no full-PC search.');
  DiscoveryMemo.Lines.Add(S);
  Log(S);
end;

procedure PrepareInstallation;
var
  Code: Integer;
  Args, Roots, Registered: String;
  Report: AnsiString;
begin
  if Preparing then exit;
  Preparing := True;
  PreparationError := ''; Prepared := False;
  ExtractTemporaryFile('{#PrepareHelper}');
  PlanPath := ExpandConstant('{tmp}\migration-plan.json');
#ifdef TestRoot
  Roots := Quoted(ExpandConstant('{#TestRoot}\legacy'));
#else
  Roots := Quoted(ExpandConstant('{userdesktop}')) + ' ' + Quoted(ExpandConstant('{%USERPROFILE}\Downloads')) + ' ' + Quoted(ExpandConstant('{userdocs}\nexa')) + ' ' + Quoted(ExpandConstant('{localappdata}\Programs\nexa')) + ' ' + Quoted(ExpandConstant('{autopf}\nexa'));
  if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#ProductId}_is1', 'InstallLocation', Registered) then
    Roots := Roots + ' ' + Quoted(Registered);
#endif
  if ExpandConstant('{param:LEGACYSOURCE|}') <> '' then Roots := Roots + ' ' + Quoted(ExpandConstant('{param:LEGACYSOURCE}'));
  Args := '--installer-prepare ' + Quoted(PlanPath) + ' ' + Quoted(DataRoot) + ' ' + Quoted(ExpandConstant('{app}')) + ' ' + Roots;
#ifdef TestRoot
  ForceDirectories(ExpandConstant('{#TestRoot}'));
  SaveStringToFile(ExpandConstant('{#TestRoot}\.nexa-installer-fixture'), 'Isolated test installer only', False);
  StringChangeEx(Args, '--installer-prepare ', '--installer-prepare-fixture ', False);
#endif
  SearchProgress.SetText('Checking registered installation and saved data', 'Preparing verified backups');
  SearchProgress.SetProgress(0, 0);
  SearchProgress.Show;
  try
    if not ExecAndLogOutput(ExpandConstant('{tmp}\{#PrepareHelper}'), Args, '', SW_HIDE, ewWaitUntilTerminated, Code, @PreparationLog) then
      PreparationError := 'Could not start installation checks. Existing saves and portable copies are unchanged.'
    else if Code <> 0 then begin
      LoadStringFromFile(PlanPath + '.error', Report);
      PreparationError := 'Preparation failed. No installation will start. ' + Report;
    end;
    Prepared := PreparationError = '';
    if Prepared then begin
      if LoadStringFromFile(PlanPath + '.report.txt', Report) then DiscoveryMemo.Lines.Add(Report);
      if FileExists(ExpandConstant('{app}\Nexa.exe')) then begin
        if PayloadNeeded then DiscoveryMemo.Lines.Add('Installed application is missing expected bytes or differs from this package. Setup will replace it from the embedded verified payload.')
        else DiscoveryMemo.Lines.Add('Application integrity verified. Existing correct application bytes will be kept.');
      end else DiscoveryMemo.Lines.Add('Application payload is missing. Setup will install the embedded verified payload.');
    end else begin DiscoveryMemo.Lines.Add(PreparationError); Log(PreparationError); end;
  finally SearchProgress.Hide; Preparing := False; end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  if not Prepared then PrepareInstallation;
  Result := PreparationError;
end;

procedure InitializeWizard;
var Registered: String;
begin
  CreateShortcutChoices;
  ModePage := CreateInputOptionPage(wpWelcome, 'Install or repair nexa', 'Choose how Setup should maintain this installation.', 'Repair preserves Spaces, recordings, history, settings and connections.', True, False);
  ModePage.Add('Install or update nexa');
  ModePage.Add('Repair Files');
  ModePage.SelectedValueIndex := 0;
  ModePage.CheckListBox.ItemEnabled[1] := RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#ProductId}_is1', 'InstallLocation', Registered);
  DiscoveryPage := CreateOutputMsgMemoPage(wpSelectTasks, 'Searching for Existing Nexa Installations', 'Review discovered locations and integrity before installing.', 'Only bounded known locations are inspected. Portable copies are preserved.', '');
  // A plain read-only memo lets the active Windows-matched style supply all text colors.
  // RichEdit retains black inline character formatting when text is appended at runtime.
  DiscoveryMemo := TNewMemo.Create(WizardForm);
  DiscoveryMemo.Parent := DiscoveryPage.Surface;
  DiscoveryMemo.SetBounds(DiscoveryPage.RichEditViewer.Left, DiscoveryPage.RichEditViewer.Top, DiscoveryPage.RichEditViewer.Width, DiscoveryPage.RichEditViewer.Height);
  DiscoveryMemo.ReadOnly := True;
  DiscoveryMemo.ScrollBars := ssVertical;
  DiscoveryMemo.WordWrap := True;
  DiscoveryPage.RichEditViewer.Visible := False;
  SearchProgress := CreateOutputProgressPage('Searching for Existing Nexa Installations', 'Checking installations and verifying backups');
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpSelectTasks then begin
    UpdatingShortcutChoices := True;
    DesktopShortcutCheck.Checked := WizardIsTaskSelected('desktopicon');
    StartMenuShortcutCheck.Checked := WizardIsTaskSelected('startmenu');
    UpdatingShortcutChoices := False;
  end;
  if (CurPageID = wpSelectDir) or (CurPageID = wpSelectTasks) then Prepared := False;
  if (CurPageID = DiscoveryPage.ID) and not Preparing and not Prepared then begin
    WizardForm.NextButton.Enabled := False;
    PrepareInstallation;
    WizardForm.NextButton.Enabled := Prepared;
    if ModePage.SelectedValueIndex = 1 then WizardForm.NextButton.Caption := 'Repair Files';
  end;
end;

procedure RemoveOwnedShortcut(const Link: String);
var Shell, Shortcut: Variant;
begin
  if not FileExists(Link) then exit;
  try
    Shell := CreateOleObject('WScript.Shell'); Shortcut := Shell.CreateShortcut(Link);
    if CompareText(Shortcut.TargetPath, ExpandConstant('{app}\Nexa.exe')) = 0 then
      if not DeleteFile(Link) then RaiseException('Could not remove the deselected nexa shortcut: ' + Link);
  except Log('Shortcut preserved: ' + Link + ': ' + GetExceptionMessage); end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Code: Integer;
  Choice, Marker, JsonData, Failure: String;
begin
  if CurStep = ssPostInstall then begin
    try
    WizardForm.StatusLabel.Caption := 'Verifying application files and Windows registration';
    if CompareText(GetSHA256OfFile(ExpandConstant('{app}\Nexa.exe')), '{#GetSHA256OfFile(PublishDir + "\Nexa.exe")}') <> 0 then
      RaiseException('Installed application checksum differs from the trusted payload. Rerun Setup.');
    JsonData := DataRoot;
    StringChangeEx(JsonData, '\', '\\', True);
    Marker := '{"AppId":"{#ProductId}","Scope":"CurrentUser","DataRoot":"' + JsonData + '"}';
    if not SaveStringToFile(ExpandConstant('{app}\installation.json'), Marker, False) then
      RaiseException('Could not write the installation identity. Rerun Setup to repair.');
    if not SaveStringToFile(ExpandConstant('{app}\installer-integrity.json'), '{"Schema":1,"Version":"{#AppVersion}","PayloadSHA256":"{#GetSHA256OfFile(PublishDir + "\Nexa.exe")}","SetupSHA256":"' + GetSHA256OfFile(ExpandConstant('{srcexe}')) + '"}', False) then
      RaiseException('Could not save installer integrity information.');
    Choice := 'keep';
    // Portable executables are always preserved; removal requires a separate explicit workflow.
    if not Exec(ExpandConstant('{app}\Nexa.exe'), '--installer-complete ' + Quoted(PlanPath) + ' ' + Choice, '', SW_HIDE, ewWaitUntilTerminated, Code) then
      RaiseException('Could not finish migration. Old copies were preserved.');
    if Code <> 0 then RaiseException('Migration did not finish. Old copies were preserved.');
    ForceDirectories(DataRoot + '\installation-reports');
    CopyFile(PlanPath, DataRoot + '\installation-reports\latest-migration.json', False);
    if not WizardIsTaskSelected('desktopicon') then RemoveOwnedShortcut(ExpandConstant('{#DesktopDir}\nexa.lnk'));
    if not WizardIsTaskSelected('startmenu') then RemoveOwnedShortcut(ExpandConstant('{#MenuDir}\nexa.lnk'));
    WizardForm.StatusLabel.Caption := 'Application integrity, registration and shortcuts verified';
    except
      Failure := GetExceptionMessage;
      Exec(ExpandConstant('{tmp}\{#PrepareHelper}'), '--installer-rollback ' + Quoted(PlanPath), '', SW_HIDE, ewWaitUntilTerminated, Code);
      RaiseException(Failure + ' Previous executable restoration was requested; inspect the installation report before retrying.');
    end;
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

function NativeFileAttributes(Name: String): LongWord;
  external 'GetFileAttributesW@kernel32.dll stdcall';

function ExtendedPath(Name: String): String;
begin
  if Copy(Name, 1, 4) <> '\\?\' then begin
    if Copy(Name, 1, 2) = '\\' then Name := '\\?\UNC\' + Copy(Name, 3, Length(Name))
    else Name := '\\?\' + Name;
  end;
  Result := Name;
end;

function FileAttributes(Name: String): LongWord;
begin
  Result := NativeFileAttributes(ExtendedPath(Name));
end;

function NativeDeleteFile(Name: String): Boolean;
  external 'DeleteFileW@kernel32.dll stdcall';
function NativeRemoveDirectory(Name: String): Boolean;
  external 'RemoveDirectoryW@kernel32.dll stdcall';
function NativeSetAttributes(Name: String; Attributes: LongWord): Boolean;
  external 'SetFileAttributesW@kernel32.dll stdcall';

function RemoveRegularTree(Path: String): Boolean;
var
  Entry: TFindRec;
  Child: String;
  Attributes: LongWord;
begin
  Result := False;
  if (FileAttributes(Path) and $400) <> 0 then Exit;
  if FindFirst(AddBackslash(ExtendedPath(Path)) + '*', Entry) then begin
    try
      repeat
        if (Entry.Name <> '.') and (Entry.Name <> '..') then begin
          Child := AddBackslash(Path) + Entry.Name;
          Attributes := FileAttributes(Child);
          if (Attributes and $400) <> 0 then Exit;
          if (Attributes and $10) <> 0 then begin
            if not RemoveRegularTree(Child) then Exit;
          end else begin
            if (Attributes and 1) <> 0 then
              if not NativeSetAttributes(ExtendedPath(Child), Attributes and $FFFFFFFE) then Exit;
            if not NativeDeleteFile(ExtendedPath(Child)) then begin Log('Could not remove confirmed private file: ' + Child); Exit; end;
          end;
        end;
      until not FindNext(Entry);
    finally FindClose(Entry); end;
  end;
  Result := NativeRemoveDirectory(ExtendedPath(Path));
end;

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
    if not RemoveRegularTree(DataRoot) then
      RaiseException('Some local data could not be removed. Inspect ' + DataRoot);
  end;
end;
