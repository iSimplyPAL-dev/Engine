; "IsService" implementa la logica di installazione/disinstallazione del servizio
#ifdef IsService
  #ifndef MainExeFile
    #error E' necessario definire "MainExeFile" col nome dell'eseguibile principale
  #endif
#endif

; "AllowConfig" visualizza le opzioni di configurazione (dei settaggi e del file INI)
#ifdef AllowConfig
  #ifndef MainExeFile
    #error E' necessario definire "MainExeFile" col nome dell'eseguibile principale
  #endif
#endif

; "OwnService" indica che la gestione dell'installazione del servizio deve essere implementata
#ifdef OwnService
  #ifndef IsService
    #error E' necessario definire "IsService" se è stato definito "OwnService"
  #endif
#endif

; "PostInstall" indica una funzione da chiamre quando i file sono stati copiati (prima di installare il servizio)
#ifdef PostInstall
  #ifndef IsService
    #error E' necessario definire "IsService" se è stato definito "PostInstall"
  #endif
  #ifdef OwnService
    #error Non è possibile definire "PostInstall" se è stato definito "OwnService"
  #endif
#endif

; "LanguageEN" e "LanguageIT" specificano le lingue da utilizzare
#ifndef LanguageEN
  #ifndef LanguageIT
    #error E' necessario definire "LanguageEN" e/o "LanguageIT"
  #endif
#endif

#ifdef MainExeFile
  #define MainExePath "{app}\" + MainExeFile
#endif

[Setup]
AppPublisher=Ribes
AppPublisherURL=http://www.isimply.it/
AppCopyright=Copyright © Ribes 2013
DirExistsWarning=no
OutputDir=.\Bin
Compression=lzma/max
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64 ia64

[Languages]
#ifdef LanguageEN
Name: "en"; MessagesFile: "compiler:Default.isl"
#endif
#ifdef LanguageIT
Name: "it"; MessagesFile: "compiler:Languages\Italian.isl"
#endif

[CustomMessages]
#ifdef LanguageEN
en.NoFramework=The .NET Framework 2.0 is not installed. Please install it, then run this setup again.
en.RagAsmError=The assembly registration has failed. Please use the REGASM.EXE command from the .NET Framework.
en.ConfigureDB=Configure settings on DB
en.ConfigureMDL=Configure Modules
en.StartService=Start service now
en.StoppingService=Stopping service...
en.InstallingService=Installing service...
en.Uninstaller=Uninstall the application
en.ErrorInstallService=Error installing service. Would you like to abort installation?
#endif
#ifdef LanguageIT
it.NoFramework=Il .NET Framework 2.0 non è installato. Installarlo, quindi rilanciare quest'installazione.
it.RagAsmError=Non è stato possibile registrare l'assembly. Utilizzare il comando REGASM.EXE del .NET Framework.
it.ConfigureDB=Configura i settaggi su DB
it.ConfigureMDL=Configura i Moduli
it.StartService=Avvia il servizio adesso
it.StoppingService=Arresto del servizio in corso...
it.InstallingService=Installazione del servizio in corso...
it.Uninstaller=Disinstalla il programma
it.ErrorInstallService=Errore nell'installazione del servizio. Annullare l'installazione?
#endif

#ifdef MainExeFile
[Icons]
Name: "{group}\{cm:Uninstaller}"; Filename: "{uninstallexe}"
#endif

#ifdef DeleteDirs
[UninstallDelete]
Type: filesandordirs; Name: "{app}\Upgrader"
Type: filesandordirs; Name: "{app}\Log"
#endif

#ifdef IsService
[Run]
Filename: "{#MainExePath}"; Parameters: "/START"; Description: "{cm:StartService}"; Flags: postinstall nowait runhidden unchecked; Check: not CheckStartService
Filename: "{#MainExePath}"; Parameters: "/START"; Description: "{cm:StartService}"; Flags: postinstall nowait runhidden;           Check: CheckStartService

[UninstallRun]
Filename: "{#MainExePath}"; Parameters: "/STOP";      Flags: waituntilterminated runhidden skipifdoesntexist
Filename: "{#MainExePath}"; Parameters: "/UNINSTALL"; Flags: waituntilterminated runhidden skipifdoesntexist
#endif

#ifdef AllowConfig
[Icons]
Name: "{group}\{cm:ConfigureDB}";  Filename: "{#MainExePath}"; Parameters: "/CFG"
Name: "{group}\{cm:ConfigureMDL}"; Filename: "{#MainExePath}"; Parameters: "/MDL"

[Run]
Filename: "{#MainExePath}"; Parameters: "/CFG"; Description: "{cm:ConfigureDB}"; Flags: postinstall nowait skipifsilent unchecked
Filename: "{#MainExePath}"; Parameters: "/MDL"; Description: "{cm:ConfigureMDL}"; Flags: postinstall nowait skipifsilent unchecked
#endif

[Code]
// Dice se il .NET Framework 2.0 è installato
function IsFramework2Installed(bSilent: Boolean): Boolean;
var
  iRetVal: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v2.0.50727', 'Install', iRetVal);
  if (not Result) and (not bSilent) then begin
    MsgBox(CustomMessage('NoFramework'), mbCriticalError, MB_OK);
  end
end;

// Registra/deregistra un assembly usando il comando REGASM
function RegisterAssembly(sAssembly: String; bRegister: Boolean; bSilent: Boolean): Boolean;
var
  sNETPath: String;
  sCmdArgs: String;
  iExitCode: Integer;
begin
  Result := RegQueryStringValue(HKLM, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\Microsoft .NET Framework 2.0', 'InstallLocation', sNETPath);
  
  if Result then begin
    sNETPath := Trim(sNETPath);
    Result := (sNETPath <> '');
  end;

  if Result then begin
    if bRegister then begin
      sCmdArgs := ' /codebase';
    end else begin
      sCmdArgs := ' /unregister';
    end;

    sCmdArgs := sAssembly + sCmdArgs;
    Result := Exec(AddBackslash(RemoveQuotes(sNETPath)) + 'regasm.exe', sCmdArgs, '', SW_HIDE, ewWaitUntilTerminated, iExitCode);
  end;

  if (not Result) and bRegister and (not bSilent) then begin
    MsgBox(CustomMessage('RagAsmError'), mbCriticalError, MB_OK);
  end
end;

// Dice se il setup è in modalità silent
function IsSetupSilent(): Boolean;
var
  iParam: Integer;
  sParam: String;
begin
  Result := False
  for iParam := ParamCount() downto 0 do begin
    sParam := Uppercase(ParamStr(iParam));
    if (sParam = '/SILENT') or (sParam = '/VERYSILENT') then begin
      Result := True;
      exit;
    end
  end
end;

// Definisce le modalità di esecuzione come aggiornamento
type TUpgradeType = (utNotAnUpgrade, utNormalUpgrade, utSilentUpgrade);

// Dice se il setup è stato lanciato come aggiornamento
function IsAnUpgrade(): TUpgradeType;
var
  iParam: Integer;
begin
  Result := utNotAnUpgrade
  for iParam := ParamCount() downto 0 do begin
    case Uppercase(ParamStr(iParam)) of
      '/NORMALUPGRADE': begin
        Result := utNormalUpgrade;
        exit;
      end;
      
      '/SILENTUPGRADE': begin
        Result := utSilentUpgrade;
        exit;
      end
    end
  end
end;

#ifdef IsService
// Indica se è stato scelto di avviare il servizio al termine del setup (non è stato specificato il parametro 'NOSTARTSVC')
function CheckStartService(): Boolean;
var
  iParam: Integer;
begin
  Result := True;
  for iParam := ParamCount() downto 0 do begin
    if Uppercase(ParamStr(iParam)) = '/NOSTARTSVC' then begin
      Result := False;
      exit;
    end
  end
end;

// Avvia ('START'), arresta ('STOP'), installa ('INSTALL'), disinstalla ('UNINSTALL') o verifica se è installato ('QUERY') il servizio
function ControlService(sAction: String): Boolean;
var
  sFilePath: String;
  iExitCode: Integer;
begin
  Result := False;
  sFilePath := ExpandConstant('{#MainExePath}');
  if FileExists(sFilePath) then begin
    Exec(RemoveQuotes(sFilePath), '/' + Uppercase(sAction), '', SW_HIDE, ewWaitUntilTerminated, iExitCode);
    Result := (iExitCode = 0);
  end
end;

#ifndef OwnService
// Indica se l'installazione del servizio è andata male e si deve fare il rollback
var m_bDoRollback: Boolean;
  
// Configura il servizio durante l'installzione
procedure CurStepChanged(CurStep: TSetupStep);
begin
  case CurStep of
    ssInstall: begin
      m_bDoRollback := False;
      // Se il servizio è già installato lo arresto
      if ControlService('QUERY') then begin
        WizardForm.StatusLabel.Caption := CustomMessage('StoppingService');
        ControlService('STOP');
      end
    end;

    ssPostInstall: begin
#ifdef PostInstall
      {#PostInstall}();
#endif
      // Se il servizio non è installato lo installo
      if not ControlService('QUERY') then begin
        WizardForm.StatusLabel.Caption := CustomMessage('InstallingService');
        m_bDoRollback := not ControlService('INSTALL');
        if m_bDoRollback then begin
          m_bDoRollback := (IDYES = SuppressibleMsgBox(CustomMessage('ErrorInstallService'), mbCriticalError, MB_YESNO, MB_DEFBUTTON2));
        end
      end
    end
  end
end;

// Nasconde la pagina finale se si sono verificati degli errori
function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  if PageID = wpFinished then begin
    Result := m_bDoRollback;
  end
end;

// Fa il rollback dell'installazione se si sono verificati degli errori
procedure DeinitializeSetup();
var
  iExitCode: Integer;
begin
  if m_bDoRollback then begin
    Exec(RemoveQuotes(ExpandConstant('{uninstallexe}')), '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART', '', SW_HIDE, ewNoWait, iExitCode);
  end
end;
#endif
#endif
