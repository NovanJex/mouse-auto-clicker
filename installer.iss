; 鼠标连点器 — Inno Setup 安装脚本
; 框架依赖 + LZMA2 极限压缩 + 自动安装 .NET 运行时

#define MyAppName "鼠标连点器"
#define MyAppVersion "1.2.2"
#define MyAppPublisher "AutoClicker"
#define MyAppExeName "鼠标连点器.exe"

[Setup]
AppId={{B28A52E4-BDDD-4024-889D-4EDF47CA05EC}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppCopyright={#MyAppPublisher}
VersionInfoVersion={#MyAppVersion}
DefaultDirName={commonpf}\{#MyAppName}
DefaultGroupName={#MyAppName}
UninstallDisplayName={#MyAppName}

OutputDir=dist
OutputBaseFilename=MouseAutoClicker_Setup_v{#MyAppVersion}

Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes
LZMANumBlockThreads=4

WizardStyle=modern
SetupIconFile=src\AutoClicker.App\Resources\Icons\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=commandline dialog
DisableDirPage=no
DirExistsWarning=yes

[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加快捷方式:"

[Files]
Source: "publish\installer-src\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\卸载 {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动 {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
var
  NeedsRuntime: Boolean;
  DownloadPage: TDownloadWizardPage;

function CheckRuntimeOnDisk: Boolean;
var
  FindRec: TFindRec;
  RuntimeDir: String;
begin
  Result := False;
  RuntimeDir := ExpandConstant('{commonpf}\dotnet\shared\Microsoft.WindowsDesktop.App');
  if DirExists(RuntimeDir) and FindFirst(RuntimeDir + '\8.*', FindRec) then
  begin
    Result := True;
    FindClose(FindRec);
    Exit;
  end;
  if RegKeyExists(HKLM32, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App') then
    Result := True;
end;

function IsDotNet8DesktopRuntimeInstalled: Boolean;
begin
  Result := CheckRuntimeOnDisk;
end;

procedure InitializeWizard;
begin
  DownloadPage := CreateDownloadPage('安装 .NET 8.0 Desktop Runtime 组件', '正在下载 .NET 8.0 Desktop Runtime 组件，请稍候...', nil);
  DownloadPage.ShowBaseNameInsteadOfUrl := True;
end;

function InitializeSetup: Boolean;
var
  MsgResult: Integer;
begin
  if not IsDotNet8DesktopRuntimeInstalled then
  begin
    MsgResult := MsgBox(
      '此应用需要 .NET 8.0 Desktop Runtime 组件才能运行。' + #13#10 + #13#10 +
      '安装程序将自动下载并安装 .NET 8.0 Desktop Runtime 组件（约 55MB），' + #13#10 +
      '整个过程可能需要几分钟，请确保网络连接正常。' + #13#10 + #13#10 +
      '是否继续？',
      mbConfirmation, MB_YESNO);
    if MsgResult = IDYES then
    begin
      NeedsRuntime := True;
      Result := True;
    end
    else
      Result := False;
  end
  else
    Result := True;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  RuntimePath: String;
  ErrorCode: Integer;
  Error: String;
begin
  if CurPageID = wpReady then
  begin
    if not NeedsRuntime then
    begin
      Result := True;
      Exit;
    end;

    // 带进度条的下载
    DownloadPage.Clear;
    DownloadPage.Add('https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe', 'dotnet-runtime-8-installer.exe', '');
    DownloadPage.Show;
    try
      try
        DownloadPage.Download;
      except
        if not DownloadPage.AbortedByUser then
        begin
          Error := AddPeriod(Format('%s: %s', [DownloadPage.LastBaseNameOrUrl, GetExceptionMessage]));
          SuppressibleMsgBox(Error, mbCriticalError, MB_OK, IDOK);
        end;
        Result := False;
        Exit;
      end;

      // 下载完成，更新页面文案为安装状态
      DownloadPage.SetText('安装 .NET 8.0 Desktop Runtime 组件', '正在安装 .NET 8.0 Desktop Runtime 组件，请稍候...');
      DownloadPage.SetProgress(0, 0); // 切换为不确定进度（跑马灯）

      // 静默安装
      RuntimePath := ExpandConstant('{tmp}\dotnet-runtime-8-installer.exe');
      if not Exec(RuntimePath, '/install /quiet /norestart', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode) then
      begin
        SuppressibleMsgBox('无法启动 .NET 8.0 Desktop Runtime 组件安装程序。', mbCriticalError, MB_OK, IDOK);
        Result := False;
        Exit;
      end;

      if ErrorCode <> 0 then
      begin
        SuppressibleMsgBox('.NET 8.0 Desktop Runtime 组件安装失败（错误代码: ' + IntToStr(ErrorCode) + '）。', mbCriticalError, MB_OK, IDOK);
        Result := False;
        Exit;
      end;

      // 验证
      if not CheckRuntimeOnDisk then
      begin
        SuppressibleMsgBox('.NET 8.0 Desktop Runtime 组件安装未能完成，请检查网络连接后重试。', mbCriticalError, MB_OK, IDOK);
        Result := False;
        Exit;
      end;

      Result := True;
    finally
      DownloadPage.Hide;
    end;
  end
  else
    Result := True;
end;
