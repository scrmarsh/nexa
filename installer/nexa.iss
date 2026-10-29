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
  #define AppVersion "0.1.1"
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
AppVerName=nexa v{#AppVersion} beta
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
Name: "{#MenuDir}\nexa"; Filename: "{app}\Nexa.exe"; WorkingDir: "{app}"; IconFilename: "{app}\Nexa.exe"; Tasks: startmenu; Check: AllowShortcuts
Name: "{#DesktopDir}\nexa"; Filename: "{app}\Nexa.exe"; WorkingDir: "{app}"; IconFilename: "{app}\Nexa.exe"; Tasks: desktopicon; Check: AllowShortcuts

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
  CleanupButton: TNewButton;
  CleanupBusy: Boolean;
  BlueTrack, BlueFill, InstallBlueTrack, InstallBlueFill: TPanel;

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

procedure SetBlueProgress(Current, Total: Integer);
begin
  if Total < 1 then Total := 1;
  if Current > Total then Current := Total;
  BlueFill.Width := (BlueTrack.Width * Current) div Total;
  SearchProgress.SetProgress(Current, Total);
end;

procedure PreparationLog(const S: String; const Error, FirstLine: Boolean);
var Rest: String; Separator, Current, Total: Integer;
begin
  if Copy(S, 1, 9) = 'PROGRESS|' then begin
    Rest := Copy(S, 10, Length(S)); Separator := Pos('|', Rest);
    Current := StrToIntDef(Copy(Rest, 1, Separator - 1), 0); Delete(Rest, 1, Separator);
    Separator := Pos('|', Rest); Total := StrToIntDef(Copy(Rest, 1, Separator - 1), 1); Delete(Rest, 1, Separator);
    SetBlueProgress(Current, Total);
    SearchProgress.SetText(Rest, 'Preserving user data; checking real installation work.');
  end else begin
    SearchProgress.SetText(S, 'Preserving Spaces, profiles, recordings and settings.');
    DiscoveryMemo.Lines.Add(S);
  end;
  Log(S);
end;

function ReviewCleanup(const Report: String; ConfirmRemoval: Boolean): Boolean;
var Form: TSetupForm; Memo: TNewMemo; Accept, Cancel: TNewButton; Hint: TNewStaticText;
begin
  Form := CreateCustomForm(ScaleX(580), ScaleY(410), False, False);
  try
    Form.Caption := 'Nexa legacy cleanup';
    Hint := TNewStaticText.Create(Form); Hint.Parent := Form;
    Hint.SetBounds(ScaleX(20), ScaleY(16), ScaleX(540), ScaleY(44)); Hint.WordWrap := True;
    Hint.Caption := 'Review identified locations and verified results. User data and the active installation are preserved.';
    Memo := TNewMemo.Create(Form); Memo.Parent := Form;
    Memo.SetBounds(ScaleX(20), ScaleY(68), ScaleX(540), ScaleY(278));
    Memo.ReadOnly := True; Memo.ScrollBars := ssVertical; Memo.WordWrap := True; Memo.Text := Report;
    Cancel := TNewButton.Create(Form); Cancel.Parent := Form;
    Cancel.SetBounds(ScaleX(430), ScaleY(364), ScaleX(130), ScaleY(30)); Cancel.Caption := 'Return to Setup'; Cancel.ModalResult := mrCancel; Cancel.Cancel := True; Cancel.Default := True;
    if ConfirmRemoval then begin
      Accept := TNewButton.Create(Form); Accept.Parent := Form;
      Accept.SetBounds(ScaleX(268), ScaleY(364), ScaleX(150), ScaleY(30)); Accept.Caption := 'Remove eligible'; Accept.ModalResult := mrOk;
    end;
    Result := Form.ShowModal = mrOk;
  finally Form.Free; end;
end;

function RunLegacyCleanup(OnlyScan: Boolean): Boolean;
var Code: Integer; Args, Roots, Mode, Inventory: String; Report: AnsiString;
begin
  Result := False;
  ExtractTemporaryFile('{#PrepareHelper}');
  Inventory := ExpandConstant('{tmp}\legacy-cleanup.json');
#ifdef TestRoot
  ForceDirectories(ExpandConstant('{#TestRoot}'));
  SaveStringToFile(ExpandConstant('{#TestRoot}\.nexa-installer-fixture'), 'Isolated test installer only', False);
  Roots := Quoted(ExpandConstant('{#TestRoot}\legacy'));
#else
  Roots := Quoted(ExpandConstant('{userdesktop}')) + ' ' + Quoted(ExpandConstant('{%USERPROFILE}\Downloads')) + ' ' + Quoted(ExpandConstant('{userdocs}\nexa')) + ' ' + Quoted(ExpandConstant('{localappdata}\Programs\nexa')) + ' ' + Quoted(ExpandConstant('{autopf}\nexa'));
#endif
  Mode := 'clean';
  if OnlyScan then Mode := 'cleanup'
  else if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#ProductId}_is1', 'InstallLocation', Args) then Mode := 'update';
  Args := '--installer-legacy-scan ' + Quoted(Inventory) + ' ' + Quoted(DataRoot) + ' ' + Quoted(ExpandConstant('{app}')) + ' ' + Mode;
#ifdef TestRoot
  Args := Args + ' fixture ' + Roots;
#else
  Args := Args + ' production ' + Roots;
#endif
  SearchProgress.SetText('Scanning for old Nexa versions', 'Identifying trusted obsolete installations and preserving unknown files.');
  SetBlueProgress(0, 1); SearchProgress.Show;
  try
    if not ExecAndLogOutput(ExpandConstant('{tmp}\{#PrepareHelper}'), Args, '', SW_HIDE, ewWaitUntilTerminated, Code, @PreparationLog) or (Code <> 0) then Exit;
    if LoadStringFromFile(Inventory + '.report.txt', Report) then DiscoveryMemo.Lines.Add(Report);
  finally SearchProgress.Hide; end;
  if OnlyScan then begin
    if not ReviewCleanup(Report, True) then begin Result := True; Exit; end;
  end;
  SearchProgress.SetText('Removing old Nexa versions', 'Recovery copies are verified before obsolete binaries are removed.');
  SetBlueProgress(0, 1); SearchProgress.Show;
  try
    Result := ExecAndLogOutput(ExpandConstant('{tmp}\{#PrepareHelper}'), '--installer-legacy-remove ' + Quoted(Inventory), '', SW_HIDE, ewWaitUntilTerminated, Code, @PreparationLog) and (Code = 0);
    if LoadStringFromFile(Inventory + '.report.txt', Report) then DiscoveryMemo.Lines.Add(Report);
  finally SearchProgress.Hide; end;
  if OnlyScan then ReviewCleanup(Report, False);
end;

procedure CleanupClick(Sender: TObject);
begin
  if CleanupBusy then Exit;
  CleanupBusy := True; CleanupButton.Enabled := False; WizardForm.NextButton.Enabled := False;
  try
    if not RunLegacyCleanup(True) then MsgBox('Cleanup could not finish. Close running Nexa copies and check access to the reported locations, then scan again. No new application was installed.', mbError, MB_OK);
  finally CleanupBusy := False; CleanupButton.Enabled := True; WizardForm.NextButton.Enabled := True; end;
end;

function AllowShortcuts: Boolean;
begin Result := ModePage.SelectedValueIndex = 0; end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := (ModePage.SelectedValueIndex = 1) and ((PageID = wpSelectDir) or (PageID = wpSelectTasks) or (PageID = wpReady) or (PageID = DiscoveryPage.ID));
  if Result then Log('Repair Files: optional page skipped: ' + IntToStr(PageID));
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
  if ModePage.SelectedValueIndex <> 1 then begin
    if not RunLegacyCleanup(False) then begin
      PreparationError := 'Legacy cleanup could not complete. Close old Nexa copies, resolve the reported access problem, and try again. Setup has not installed new application files.';
      Preparing := False; Exit;
    end;
  end;
  ExtractTemporaryFile('{#PrepareHelper}');
  PlanPath := ExpandConstant('{tmp}\migration-plan.json');
  if ModePage.SelectedValueIndex = 1 then begin
    if not Exec(ExpandConstant('{tmp}\{#PrepareHelper}'), '--installer-repair-validate ' + Quoted(ExpandConstant('{app}')), '', SW_HIDE, ewWaitUntilTerminated, Code) or (Code <> 0) then begin
      PreparationError := 'Repair requires a valid registered Nexa installation. Choose Install or update to select a new location.';
      Preparing := False; Exit;
    end;
  end;
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
  SetBlueProgress(0, 1);
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
  finally if Prepared then SetBlueProgress(1, 1); SearchProgress.Hide; Preparing := False; end;
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
  if ExpandConstant('{param:MAINTENANCE|}') = 'repair' then ModePage.SelectedValueIndex := 1;
  ModePage.CheckListBox.ItemEnabled[1] := RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#ProductId}_is1', 'InstallLocation', Registered);
  DiscoveryPage := CreateOutputMsgMemoPage(wpSelectDir, 'Searching for Existing Nexa Installations', 'Review discovered locations and integrity before installing.', 'Trusted obsolete versions are cleaned up; user data and ambiguous copies are preserved.', '');
  // A plain read-only memo lets the active Windows-matched style supply all text colors.
  // RichEdit retains black inline character formatting when text is appended at runtime.
  DiscoveryMemo := TNewMemo.Create(WizardForm);
  DiscoveryMemo.Parent := DiscoveryPage.Surface;
  DiscoveryMemo.SetBounds(DiscoveryPage.RichEditViewer.Left, DiscoveryPage.RichEditViewer.Top, DiscoveryPage.RichEditViewer.Width, DiscoveryPage.RichEditViewer.Height);
  DiscoveryMemo.ReadOnly := True;
  DiscoveryMemo.ScrollBars := ssVertical;
  DiscoveryMemo.WordWrap := True;
  DiscoveryPage.RichEditViewer.Visible := False;
  SearchProgress := CreateOutputProgressPage('Nexa maintenance', 'Scanning, cleaning and verifying installation files');
  SearchProgress.ProgressBar.Visible := False;
  BlueTrack := TPanel.Create(WizardForm); BlueTrack.Parent := SearchProgress.Surface;
  BlueTrack.SetBounds(SearchProgress.ProgressBar.Left, SearchProgress.ProgressBar.Top, SearchProgress.ProgressBar.Width, ScaleY(12));
  BlueTrack.StyleElements := []; BlueTrack.Color := StrToColor('#30364A'); BlueTrack.BevelOuter := bvNone;
  BlueFill := TPanel.Create(WizardForm); BlueFill.Parent := BlueTrack; BlueFill.SetBounds(0, 0, 0, BlueTrack.Height);
  BlueFill.StyleElements := []; BlueFill.Color := StrToColor('#5B7CFF'); BlueFill.BevelOuter := bvNone;
  WizardForm.ProgressGauge.Visible := False;
  InstallBlueTrack := TPanel.Create(WizardForm); InstallBlueTrack.Parent := WizardForm.InstallingPage;
  InstallBlueTrack.SetBounds(WizardForm.ProgressGauge.Left, WizardForm.ProgressGauge.Top, WizardForm.ProgressGauge.Width, WizardForm.ProgressGauge.Height);
  InstallBlueTrack.StyleElements := []; InstallBlueTrack.Color := StrToColor('#30364A'); InstallBlueTrack.BevelOuter := bvNone;
  InstallBlueFill := TPanel.Create(WizardForm); InstallBlueFill.Parent := InstallBlueTrack; InstallBlueFill.SetBounds(0, 0, 0, InstallBlueTrack.Height);
  InstallBlueFill.StyleElements := []; InstallBlueFill.Color := StrToColor('#5B7CFF'); InstallBlueFill.BevelOuter := bvNone;
  CleanupButton := TNewButton.Create(WizardForm); CleanupButton.Parent := WizardForm.WelcomePage;
  CleanupButton.SetBounds(WizardForm.WelcomeLabel2.Left, WizardForm.WelcomeLabel2.Top + ScaleY(150), WizardForm.WelcomeLabel2.Width, ScaleY(36));
  CleanupButton.Caption := 'Scan PC for Old Nexa Versions'; CleanupButton.OnClick := @CleanupClick;
  WizardForm.WelcomeLabel2.Caption := WizardForm.WelcomeLabel2.Caption + #13#10 + #13#10 + 'Find and remove old Nexa Alpha, Beta, and legacy installations from your computer.';
  WizardForm.WelcomeLabel2.Height := ScaleY(142);
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
    if AllowShortcuts and not WizardIsTaskSelected('desktopicon') then RemoveOwnedShortcut(ExpandConstant('{#DesktopDir}\nexa.lnk'));
    if AllowShortcuts and not WizardIsTaskSelected('startmenu') then RemoveOwnedShortcut(ExpandConstant('{#MenuDir}\nexa.lnk'));
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

procedure CurInstallProgressChanged(CurProgress, MaxProgress: Integer);
begin
  if MaxProgress > 0 then InstallBlueFill.Width := (InstallBlueTrack.Width * CurProgress) div MaxProgress;
end;
