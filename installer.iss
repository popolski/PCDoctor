#define MyAppName "PCDoctor"
#define MyAppVersion "1.0"
#define MyAppPublisher "Hugues Dubois"
#define MyAppExeName "PCDoctor.exe"
#define MyAppIcon "PCDoctor\Assets\pcdoctor.ico"
#define PublishDir "PCDoctor\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=C:\Users\hugue\Desktop
OutputBaseFilename=PCDoctor_Setup_v{#MyAppVersion}
SetupIconFile={#MyAppIcon}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
MinVersion=10.0.17763
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName} {#MyAppVersion}

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "Créer un raccourci sur le Bureau"; GroupDescription: "Raccourcis :"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\Désinstaller {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{userdesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Lancer {#MyAppName}"; Flags: nowait postinstall skipifsilent runascurrentuser

[Code]
var
  DownloadPage: TDownloadWizardPage;
  NeedDotNet8: Boolean;

{ Détecte .NET 8 Desktop Runtime via le dossier d'installation }
function IsDotNet8Installed(): Boolean;
var
  FindRec: TFindRec;
begin
  Result := False;
  if FindFirst(ExpandConstant('{pf}') + '\dotnet\shared\Microsoft.WindowsDesktop.App\8.*', FindRec) then
  begin
    FindClose(FindRec);
    Result := True;
  end;
end;

function OnDownloadProgress(const Url, Filename: String; const Progress, ProgressMax: Int64): Boolean;
begin
  Result := True;
  if DownloadPage <> nil then
    DownloadPage.SetProgress(Progress, ProgressMax);
end;

procedure InitializeWizard;
begin
  NeedDotNet8 := not IsDotNet8Installed();

  if NeedDotNet8 then
  begin
    DownloadPage := CreateDownloadPage(
      'Téléchargement des pré-requis',
      'Installation du Runtime .NET 8 Desktop nécessaire...',
      @OnDownloadProgress
    );
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  ResultCode: Integer;
  SetupFile: String;
begin
  Result := True;

  if (CurPageID = wpReady) and NeedDotNet8 then
  begin
    if MsgBox(
      'PCDoctor nécessite le Runtime .NET 8 Desktop, qui n''est pas installé sur cet ordinateur.' + #13#10 + #13#10 +
      'Cliquez Oui pour le télécharger et l''installer automatiquement (environ 55 Mo),' + #13#10 +
      'ou Non pour annuler et l''installer manuellement.',
      mbConfirmation, MB_YESNO) = IDNO then
    begin
      Result := False;
      Exit;
    end;

    DownloadPage.Clear;
    DownloadPage.Add(
      'https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe',
      'dotnet8-desktop.exe',
      ''
    );
    DownloadPage.Show;
    try
      try
        DownloadPage.Download;
        SetupFile := ExpandConstant('{tmp}\dotnet8-desktop.exe');
        DownloadPage.SetText('Installation en cours...', '');

        if not Exec(SetupFile, '/install /quiet /norestart', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
        begin
          MsgBox('Échec du lancement de l''installeur .NET 8.' + #13#10 +
                 'Installez-le manuellement depuis : https://aka.ms/dotnet/8', mbError, MB_OK);
          Result := False;
        end else if ResultCode <> 0 then begin
          MsgBox('L''installation de .NET 8 a échoué (code ' + IntToStr(ResultCode) + ').' + #13#10 +
                 'Installez-le manuellement depuis : https://aka.ms/dotnet/8', mbError, MB_OK);
          Result := False;
        end else begin
          NeedDotNet8 := False;
        end;
      except
        MsgBox('Téléchargement échoué. Vérifiez votre connexion internet.' + #13#10 +
               'Installez .NET 8 manuellement depuis : https://aka.ms/dotnet/8', mbError, MB_OK);
        Result := False;
      end;
    finally
      DownloadPage.Hide;
    end;
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
end;
