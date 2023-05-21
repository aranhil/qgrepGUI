; -- 64Bit.iss --
; Demonstrates installation of a program built for the x64 (a.k.a. AMD64)
; architecture.
; To successfully run this installation and the program it installs,
; you must have a "x64" edition of Windows.

; SEE THE DOCUMENTATION FOR DETAILS ON CREATING .ISS SCRIPT FILES!

[Setup]
AppName=qgrep GUI
AppVersion=2.6
WizardStyle=modern
DefaultDirName={autopf}\qgrepGUI
DefaultGroupName=qgrep GUI
UninstallDisplayIcon={app}\qgrepGUI.exe
Compression=lzma2
SolidCompression=yes
OutputDir=..\build\Installer
OutputBaseFilename=qgrepGUIInstaller
; "ArchitecturesAllowed=x64" specifies that Setup cannot run on
; anything but x64.
ArchitecturesAllowed=x64
; "ArchitecturesInstallIn64BitMode=x64" requests that the install be
; done in "64-bit mode" on x64, meaning it should use the native
; 64-bit Program Files directory and the 64-bit view of the registry.
ArchitecturesInstallIn64BitMode=x64

[Files]
Source: "..\build\Release_x64\qgrepGUI_Standalone\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs

[Icons]
Name: "{group}\qgrep GUI"; Filename: "{app}\qgrepGUI.exe"
