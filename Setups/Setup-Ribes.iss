; Definizione del nome dell'installante prodotto e posto nella sottocartella "Bin"
#define SetupFile "Setup-Ribes"
#define MainExeFile "Ribes.OPENgov.Service.exe"
#define AppDetails "Ribes OPENgov Service"
#define DisplayName "OPENgov - Ribes"
#define ServiceName "OG_Ribes"

; Definizione della lingua del setup e inclusione dello script comune di OPENgov
#define LanguageIT
#include AddBackslash(SourcePath) + "Common.iss"

[Files]
; Impostare il nome del file di configurazione con i dati sull'accesso al database ed il nome-chiave del servizio.
#emit AddConfig("Ribes.OPENgov.Service.exe.config")

; Aggiungere qui di seguito i moduli da includere nel pacchetto di installazione
;#emit AddDll("Ribes.ServiceModules.AcquireCatasto.dll")
;#emit AddDll("Ribes.ServiceModules.AcquireComponenti.dll")
;#emit AddDll("Ribes.ServiceModules.AcquireMQ.dll")
;#emit AddDll("Ribes.ServiceModules.AcquireResidenti.dll")