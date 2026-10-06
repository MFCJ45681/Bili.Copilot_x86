; Bili Copilot x86 (32-bit) Installer - Inno Setup Script
; Wraps signed MSIX: imports dev cert, then Add-AppxPackage

#define AppName "哔哩助理 Bili Copilot"
#define AppVersion "0.0.6"
#define AppPublisher "Richasy"
#define OutputDir "F:\Users\Administrator\Desktop\WorkBuddy\installer-output"
#define MsixDir "F:\Users\Administrator\Desktop\WorkBuddy\msix-output\BiliCopilot.UI_0.0.6.0_x86_Publish_Test"
#define MsixName "BiliCopilot.UI_0.0.6.0_x86_Publish.msix"
#define CerFile "F:\Users\Administrator\Desktop\WorkBuddy\Bili.Copilot\scripts\cert\BiliCopilot.cer"
#define Aumid "Richasy.BiliCopilot.Dev_g7ew4fxhv1f46!App"

[Setup]
AppId={{3F8A2C71-9E4D-4B6A-A1C5-7D2E8F0B3A96}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\BiliCopilot
DefaultGroupName={#AppName}
OutputDir={#OutputDir}
OutputBaseFilename=BiliCopilot_x86_Setup
SetupIconFile=F:\Users\Administrator\Desktop\WorkBuddy\Bili.Copilot\src\Desktop\BiliCopilot.UI\Assets\logo.ico
Compression=lzma2/max
SolidCompression=yes
ArchitecturesAllowed=x86compatible x64compatible
WizardStyle=modern
PrivilegesRequired=admin
UninstallDisplayName={#AppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#MsixDir}\{#MsixName}"; DestDir: "{tmp}"; Flags: deleteafterinstall
Source: "{#CerFile}"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{autodesktop}\{#AppName}"; Filename: "explorer.exe"; Parameters: "shell:AppsFolder\{#Aumid}"; Tasks: desktopicon

[Run]
Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -Command ""Import-Certificate -FilePath '{tmp}\BiliCopilot.cer' -CertStoreLocation Cert:\LocalMachine\TrustedPeople | Out-Null"""; Flags: runhidden waituntilterminated; StatusMsg: "Installing certificate..."
Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -Command ""$n='Richasy.BiliCopilot.Dev'; Get-AppxPackage -Name $n -ErrorAction SilentlyContinue | Remove-AppxPackage -ErrorAction SilentlyContinue; Add-AppxPackage -Path '{tmp}\{#MsixName}' -ForceUpdateFromAnyVersion"""; Flags: runhidden waituntilterminated; StatusMsg: "Installing Bili Copilot..."
Filename: "explorer.exe"; Parameters: "shell:AppsFolder\{#Aumid}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent shellexec

[UninstallRun]
Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -Command ""Get-AppxPackage -Name 'Richasy.BiliCopilot.Dev' | Remove-AppxPackage"""; Flags: runhidden waituntilterminated; RunOnceId: "RemoveAppx"

[Code]
function InitializeUninstall(): Boolean;
begin
  Result := True;
end;
