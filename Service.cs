using System;
using System.Configuration;
using System.ServiceProcess;
using FutureFog.U4N.General;
using System.Windows.Forms;
using System.Threading;
using Ribes.OPENgov.Utilities;
using OPENgovDL;
using log4net;
using log4net.Config;

namespace Ribes.OPENgov.Service
{
    /// <summary>
    /// Rappresenta il modulo principale del servizio.
    /// </summary>
    public class Service : ServiceBase
    {
        #region Variabili di classe
        private volatile bool _stopping;
        private volatile bool _stopped;
        private int _runnings;
        private static readonly ILog Log = LogManager.GetLogger(typeof(Service));
        #endregion

        #region Avvio/arresto del servizio
        /// <summary>
        /// Avvia i moduli per fare il debug.
        /// </summary>
        private void StartForDebug()
        {
            OnStart(null);
            Application.EnableVisualStyles();
            MessageBox.Show("Service running in DEBUG mode... Press OK to stop.",
                ServiceName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            OnStop();
        }

        /// <summary>
        /// Avvio del servizio.
        /// </summary>
        /// <param name="args">Parametri del servizio.</param>
        protected override void OnStart(string[] args)
        {
            Global.Log.Write2(LogSeverity.Information, "Loading configuration...");
            Config.Load();

            try
            {
                new Thread(CheckCallback).Start();
                new Thread(CheckFilesToImport).Start();
            }
            catch (Exception ex)
            {
                Global.Log.Write2(LogSeverity.Critical, ex);
            }

            int bits = MiscTools.IsRunningOn64Bit ? 64 : 32;
            Global.Log.Write2(LogSeverity.Information, string.Format("Service ({0} bit) started.", bits));
        }

        /// <summary>
        /// Arresto del servizio.
        /// </summary>
        protected override void OnStop()
        {
            if (_stopping || _stopped)
                return;

            _stopping = true;
            Global.Log.Write2(LogSeverity.Information, "Stopping modules...");

            lock (Config.ModulesList)
            {
                foreach (Module module in Config.ModulesList)
                    module.Abort();
            }

            while (Thread.VolatileRead(ref _runnings) > 0)
                Thread.Sleep(100);

            lock (Config.ModulesList)
            {
                foreach (Module module in Config.ModulesList)
                    module.Dispose();
            }

            Global.Log.Write2(LogSeverity.Information, "Service stopped.");
            _stopped = true;
            _stopping = false;
        }

        /// <summary>
        /// Arresto del server.
        /// </summary>
        protected override void OnShutdown()
        {
            OnStop();
        }
        #endregion

        #region Avvio dell'applicazione
        /// <summary>
        /// Avvio dell'applicazione.
        /// </summary>
        /// <param name="args">Eventuali parametri da riga di comando.</param>
        /// <returns>Codice di ritorno usato dall'installer.</returns>
        [MTAThread]
        private static int Main(string[] args)
        {
            Global.Log = new Log();
            Global.Log.Start();

            try
            {
                string pathfileinfo;
                pathfileinfo = System.Configuration.ConfigurationManager.AppSettings["pathfileconflog4net"].ToString();
                System.IO.FileInfo fileconfiglog4net = new System.IO.FileInfo(pathfileinfo);
                XmlConfigurator.ConfigureAndWatch(fileconfiglog4net);

                if (!MiscTools.IsWindowsXpOrNewer)
                {
                    Global.Log.Write2(LogSeverity.Critical, "This program requires Windows XP or newer.");
                    return (int)SetupActionResult.Failure;
                }

                AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs e)
                    {
                        string message = (e.ExceptionObject is Exception) ?
                                             (e.ExceptionObject as Exception).Message : "Unspecified error.";
                        Global.Log.Write2(LogSeverity.Critical, message);
                    };

                SetupActionResult result = Setup.DoSetupAction(
                    args,
                    ConfigurationManager.AppSettings["ServiceName"],
                    ConfigurationManager.AppSettings["DisplayName"],
                    ConfigurationManager.AppSettings["Description"],
                    ConfigurationManager.AppSettings["DisplayName"]);

                if (result != SetupActionResult.NoAction)
                    return (int)result;

                using (Service service = new Service())
                {
                    service.ServiceName = ConfigurationManager.AppSettings["ServiceName"];
#if DEBUG
                    service.StartForDebug();
#else
                    ServiceBase.Run(service);
#endif
                }

                return (int)SetupActionResult.Success;
            }
            catch (Exception ex)
            {
                Global.Log.Write2(LogSeverity.Critical, ex);
                return (int)SetupActionResult.Failure;
            }
            finally
            {
                Global.Log.Stop();
                Global.Log.Dispose();
            }
        }
        #endregion

        #region Callback dei thread
        /// <summary>
        /// Controlla quali moduli devono essere eseguiti.
        /// </summary>
        private void CheckCallback()
        {
            Interlocked.Increment(ref _runnings);

            while ((!_stopping) && (Config.SettingsList.Count > 0))
            {
                Setting[] settings = { Config.SettingsList.Peek(), new Setting(Config.SettingsList.Dequeue().Name) };
                if (settings[1].Load() && string.IsNullOrEmpty(settings[1].Name))
                    settings[0].Save();
            }

            for (; !_stopping; Thread.Sleep(60000))
            {
                lock (Config.ModulesList)
                {
                    foreach (Module module in Config.ModulesList)
                    {
                        if (_stopping)
                            break;

                        if (module.Ready)
                        {
                            try
                            {
                                Global.Log.Write2(LogSeverity.Debug, "CheckCallback::devo far partire il modulo: " + module.Name);
                                new Thread(RunCallback).Start(module);
                            }
                            catch (Exception ex)
                            {
                                Global.Log.Write2(LogSeverity.Critical, ex);
                            }
                        }
                    }
                }
            }

            Interlocked.Decrement(ref _runnings);
        }
        /// <summary>
        /// Controlla la presenza di file per attivare l'importazione
        /// </summary>
        private void CheckFilesToImport()
        {
            try
            {
                Interlocked.Increment(ref _runnings);
                while ((!_stopping) && (Config.SettingsList.Count > 0))
                {
                    Setting[] settings = { Config.SettingsList.Peek(), new Setting(Config.SettingsList.Dequeue().Name) };
                    if (settings[1].Load() && string.IsNullOrEmpty(settings[1].Name))
                        settings[0].Save();
                }
                    int millisecondsToSleep = (int)(new DateTime(DateTime.Now.AddDays(1).Year, DateTime.Now.AddDays(1).Month, DateTime.Now.AddDays(1).Day, 1, 0, 0) - DateTime.Now).TotalMilliseconds;

                millisecondsToSleep = 90000;
                for (; !_stopping; Thread.Sleep(millisecondsToSleep))
                {
                    ImportFiles();
                }

            }
            catch (Exception ex)
            {
                Log.Debug("CheckFilesToImport::errore::", ex);
            }
            finally
            {
                Interlocked.Decrement(ref _runnings);
            }
        }
        private void ImportFiles()
        {
            try
            {
                string RepositoryFilesToImport = (ConfigurationManager.AppSettings["RepositoryFilesToImport"] != null ? ConfigurationManager.AppSettings["RepositoryFilesToImport"] : string.Empty);
                if (RepositoryFilesToImport != string.Empty)
                {
                    Enti[] ListEnti = new Enti(new DBConfig().DBType, new DBConfig().ConnectionStringGENERAL).LoadAll();
                    foreach (Enti myEnte in ListEnti)
                    {
                        try
                        {
                            System.IO.DirectoryInfo d = new System.IO.DirectoryInfo(RepositoryFilesToImport + myEnte.CodEnte);
                            System.IO.FileInfo[] Files = d.GetFiles("*.txt");
                            foreach (System.IO.FileInfo file in Files)
                            {                      
                                          
                                AnagFile anagfile = new AnagFile(new DBConfig().DBType, new DBConfig().ConnectionStringANAGRAFICA);
                                string NameFileToImport = file.Name.Replace(file.Extension, string.Empty) + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + file.Extension;
                                anagfile.FileMIMEType = "text/plain";
                                anagfile.Ente = myEnte.CodEnte;
                                anagfile.PostedFile = null;//System.IO.File.ReadAllBytes(file.FullName);
                                anagfile.PathFile = RepositoryFilesToImport + "DaAcquisire\\" + myEnte.CodEnte + "\\" + NameFileToImport;
                                anagfile.FileName = NameFileToImport;
                                anagfile.IdAnagFileType = GetFileType(file.Name);
                                if (anagfile.Save())
                                {
                                    Module.SetNextRun(GetModuleToRun(anagfile.IdAnagFileType), DateTime.Now);
                                    if (System.IO.File.Exists(RepositoryFilesToImport + "Acquisiti\\" + myEnte.CodEnte + "\\" + NameFileToImport))
                                        System.IO.File.Delete(RepositoryFilesToImport + "Acquisiti\\" + myEnte.CodEnte + "\\" + NameFileToImport);
                                    System.IO.File.Move(file.FullName, RepositoryFilesToImport + "DaAcquisire\\" + myEnte.CodEnte + "\\" + NameFileToImport);
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            Log.Debug("ImportFiles.RepositoryFilesToImport::errore::", err);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug("ImportFiles::errore::", ex);
            }
        }
        private int GetFileType(string fileName)
        {
            switch (fileName)
            {
                case "DGARIBEA.txt":
                    return 1;
                case "ANAGRAFE.txt":
                    return 101;
                case "STRADARIO.txt":
                    return 102;
                case "RIDUZIONI.txt":
                    return 121;
                case "DICH_8852.txt":
                    return 110;
                case "VERSAMENTI_8852.txt":
                    return 111;
                case "TIPO_UTILIZZO.txt":
                    return 112;
                case "TIPO_POSSESSO.txt":
                    return 113;
                case "TIPO_RENDITA.txt":
                    return 114;
                case "DICH_0434.txt":
                    return 120;
                case "RID_DICH_0434.txt":
                    return 121;
                case "ESE_DICH_0434.txt":
                    return 122;
                case "AVVISI_0434.txt":
                    return 123;
                case "RATE_0434.txt":
                    return 124;
                case "PAGAMENTI_0434.txt":
                    return 125;
                case "CATEGORIA_TIA.txt":
                    return 126;
                case "DICH_0453.txt":
                    return 130;
                case "AGEVOLAZIONI_0453.txt":
                    return 131;
                case "AVVISI_0453.txt":
                    return 132;
                case "RATE_0453.txt":
                    return 133;
                case "PAGAMENTI_0453.txt":
                    return 134;
                case "DICH_9763.txt":
                    return 140;
                case "AGEVOLAZIONI_9763.txt":
                    return 141;
                case "AVVISI_9763.txt":
                    return 142;
                case "RATE_9763.txt":
                    return 143;
                case "PAGAMENTI_9763.txt":
                    return 144;
            }
            return -1;
        }
        private string GetModuleToRun(int fileType)
        {
            switch (fileType)
            {
                case 1:
                    return "AcquireResidenti";
                case 2:
                case 3:
                    return "AcquireCatasto";
                case 4:
                    return "AcquireSoggetti";
                case 5:
                    return "AcquireTitoli";
                case 6:
                    return "AcquireCompraVendita";
                case 7:
                case 8:
                    return "AcquireDOCFA";
                case 101:
                case 102:
                case 110:
                case 111:
                case 112:
                case 113:
                case 114:
                case 120:
                case 121:
                case 123:
                case 124:
                case 125:
                case 126:
                    return "AcquireDichiarazioni";
            }
            return null;
        }

        /// <summary>
        /// Esegue un modulo in modo asincrono.
        /// </summary>
        /// <param name="module">Il modulo che deve essere eseguito.</param>
        private void RunCallback(object module)
        {
            Interlocked.Increment(ref _runnings);
            (module as Module).Run();
            Interlocked.Decrement(ref _runnings);
        }
        #endregion
    }
}