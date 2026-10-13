; ============================================================
;  桌面栅栏 DesktopFences Inno Setup 安装脚本
;  需配合 build.ps1 使用：先 dotnet publish 到 ..\publish，再由本脚本打包
;  手动编译：用 Inno Setup Compiler 打开本文件并编译 (Build > Compile)
; ============================================================

#define MyAppName "桌面栅栏"
#define MyAppNameEn "DesktopFences"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "DesktopFences"
#define MyAppExeName "DesktopFences.exe"

[Setup]
; AppId 唯一标识本应用（如需发布请自行更换 GUID）
AppId={{C2F4A6B8-1D3E-4F5A-9B7C-8E0D1F2A3B4C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppNameEn}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; 安装与卸载需要写入 Program Files，故请求管理员权限
PrivilegesRequired=admin
OutputDir=Output
OutputBaseFilename=DesktopFencesSetup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
; 仅支持 64 位 Windows
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; 中文安装界面
ShowLanguageDialog=no

[Languages]
; 直接引用仓库内随附的简体中文语言文件（无需下载/无需放入 Inno Setup 安装目录）
Name: "chinesesimplified"; MessagesFile: "ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加任务:"; Flags: unchecked
Name: "autostart"; Description: "开机自动启动桌面栅栏"; GroupDescription: "附加任务:"

[Files]
; 发布目录下的所有文件（单文件发布时主要是 DesktopFences.exe）
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\卸载 {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; 勾选开机自启时写入当前用户的 Run 项（卸载时自动删除）
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
    ValueType: string; ValueName: "DesktopFences"; \
    ValueData: """{app}\{#MyAppExeName}"""; \
    Flags: uninsdeletevalue; Tasks: autostart
; 卸载时清理桌面右键菜单项（由程序运行时按需写入，这里确保卸载后不残留）
Root: HKCU; Subkey: "Software\Classes\DesktopBackground\Shell\DesktopFences"; \
    Flags: dontcreatekey uninsdeletekey

[Run]
; 安装完成后可选择立即运行
Filename: "{app}\{#MyAppExeName}"; Description: "立即运行 {#MyAppName}"; \
    Flags: nowait postinstall skipifsilent

[UninstallDelete]
; 卸载时清理用户配置目录
Type: filesandordirs; Name: "{userappdata}\DesktopFences"
