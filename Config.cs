using System;
using System.Collections.Generic;
using System.IO;
using Ribes.OPENgov.Utilities;
using FutureFog.U4N.General;
using Module = Ribes.OPENgov.Utilities.Module;

namespace Ribes.OPENgov.Service
{
    /// <summary>
    /// Gestisce la configurazione su file XML.
    /// </summary>
    public static class Config
    {
        #region Variabili e proprietà
        private static string _modulesFolder;
        private static Module[] _modulesList = { };
        private static readonly Queue<Setting> SettingsQueue = new Queue<Setting>();
        //private static readonly IniFile NextRunsIni;

        /// <summary>
        /// Costruttore statico.
        /// </summary>
        static Config()
        {
            //string file = (Global.GetIniValue("SETTINGS", "NextRunsIni", null) ?? string.Empty).Trim();
            //if(file.Length > 0)
            //    NextRunsIni = new IniFile(file, "NEXT_RUNS");
        }

        /// <summary>
        /// Restituisce la lista dei moduli caricati.
        /// </summary>
        public static Module[] ModulesList
        {
            get { return _modulesList; }
        }

        /// <summary>
        /// Restituisce la coda dei settaggi da creare.
        /// </summary>
        public static Queue<Setting> SettingsList
        {
            get { return SettingsQueue; }
        }
        #endregion

        #region Lettura dati
        /// <summary>
        /// Carica la configurazione dal file XML (*.cfg).
        /// </summary>
        /// <returns>True = OK; false = errore.</returns>
        public static void Load()
        {
            try
            {
                _modulesFolder = MiscTools.AppendFolderSeparator(MiscTools.GetApplicationFolder(true) + "Modules");
                if(!Directory.Exists(_modulesFolder))
                    Directory.CreateDirectory(_modulesFolder);

#pragma warning disable 618
                AppDomain.CurrentDomain.AppendPrivatePath("Modules");
#pragma warning restore 618

                Setting sets=new Setting();
                SettingsQueue.Clear();
                foreach (Setting setting in sets.LoadAll())
                    SettingsQueue.Enqueue(setting);

                //XmlDocument xmlDoc = new XmlDocument();
                //xmlDoc.Load(MiscTools.GetApplicationFullPath(false) + ".xml");

                //_SettingsQueue.Clear();
                //foreach(XmlNode setting in xmlDoc.SelectNodes("/service/settings/setting"))
                //{
                //    string name = ReadAttribute(setting, "name");
                //    if(!string.IsNullOrEmpty(name))
                //    {
                //        _SettingsQueue.Enqueue(new Setting(
                //            name,
                //            ReadAttribute(setting, "value"),
                //            ReadAttribute(setting, "description")));
                //    }
                //}

                //XmlNodeList modules = xmlDoc.SelectNodes("/service/modules/module");
                //_modulesList = new CfgModule[modules.Count];
                //for(int i = 0; i < modules.Count; i++)
                //    _modulesList[i] = new CfgModule(modules[i]);

                //foreach (XmlNode node in xmlDoc.SelectNodes("/service/modules/module"))
                //    new Module(node, _modulesFolder);

                Module module = new Module {ModuleFolder = _modulesFolder};
                _modulesList = module.LoadAll();

                //_modulesList = new Module[modules.Count];
                //for (int i = 0; i < modules.Count; i++)
                //    _modulesList[i] = new CfgModule(modules[i]);


            }
            catch(Exception ex)
            {
                Global.Log.Write2(LogSeverity.Critical, ex);
            }
        }

        ///// <summary>
        ///// Legge il valore di un attributo.
        ///// </summary>
        ///// <param name="node">Il nodo XML.</param>
        ///// <param name="name">Il nome dell'attributo.</param>
        ///// <returns>Il valore dell'attributo.</returns>
        //private static string ReadAttribute(XmlNode node, string name)
        //{
        //    foreach(XmlAttribute attribute in node.Attributes)
        //    {
        //        if(string.Compare(attribute.Name, name, true) == 0)
        //            return attribute.Value;
        //    }
        //    return null;
        //}
        #endregion

        //#region Gestione file INI
        ///// <summary>
        ///// Ritorna 
        ///// </summary>
        ///// <param name="moduleName">Il nome del modulo.</param>
        ///// <returns>La data di prossima esecuzione del modulo.</returns>
        //private static DateTime GetNextRun(string moduleName)
        //{
        //    try
        //    {
        //        string keyName = MiscTools.EscapeText(moduleName, true);
        //        string nextRun;

        //        if(NextRunsIni == null)
        //            nextRun = Global.GetIniValue("NEXT_RUNS", keyName, null);
        //        else
        //            lock(NextRunsIni) nextRun = NextRunsIni.GetValue(keyName, null);

        //        if(!string.IsNullOrEmpty(nextRun))
        //            return Global.DateTimeFromString(nextRun);
        //    }
        //    catch(Exception ex)
        //    {
        //        Global.Log.Write2(LogSeverity.Warning, ex, moduleName);
        //    }

        //    return DateTime.Now;
        //}

        ///// <summary>
        ///// Imposta la data di prossima esecuzione di un modulo.
        ///// </summary>
        ///// <param name="moduleName">Il nome del modulo.</param>
        ///// <param name="nextRun">La data di prossima esecuzione del modulo.</param>
        //private static void SetNextRun(string moduleName, DateTime nextRun)
        //{
        //    try
        //    {
        //        string keyName = MiscTools.EscapeText(moduleName, true);
        //        string runDate = Global.DateTimeToString(nextRun);
        //        bool result;

        //        if(NextRunsIni == null)
        //            result = Global.SetIniValue("NEXT_RUNS", keyName, runDate);
        //        else
        //            lock(NextRunsIni) result = NextRunsIni.SetValue(keyName, runDate);

        //        if(!result)
        //            throw new Exception(string.Format("Cannot write next execution date: '{0}'.", runDate));
        //    }
        //    catch(Exception ex)
        //    {
        //        Global.Log.Write2(LogSeverity.Critical, ex, moduleName);
        //    }
        //}
        //#endregion

        //#region Classe "CfgModule"
        ///// <summary>
        ///// Contiene un modulo da caricare.
        ///// </summary>
        //public class CfgModule : IDisposable
        //{
        //    private volatile bool _running;
        //    private string _Name;
        //    private ThreadPriority _Priority;
        //    private ServiceModule _module;
        //    private DateTime _nextRun;
        //    private Module _dbModule;

        //    /// <summary>
        //    /// Costruttore.
        //    /// </summary>
        //    /// <param name="node">Il nodo XML da cui prelevare i dati.</param>
        //    public CfgModule(XmlNode node)
        //    {
        //        _Name = ReadAttribute(node, "name");
        //        if(string.IsNullOrEmpty(_Name))
        //            throw new Exception("The name of a module has not been specified.");

        //        for(int i = 0; i < _modulesList.Length; i++)
        //        {
        //            if(_modulesList[i] == null)
        //                break;

        //            if(string.Compare(_Name, _modulesList[i]._Name, true) == 0)
        //                throw new Exception(string.Format("More than one modules have name: '{0}'.", _Name));
        //        }

        //        string priority = ReadAttribute(node, "priority");
        //        priority = (priority == null) ? string.Empty : priority.ToLower();
        //        switch(priority)
        //        {
        //            case "high":
        //                _Priority = ThreadPriority.AboveNormal;
        //                break;

        //            case "low":
        //                _Priority = ThreadPriority.BelowNormal;
        //                break;

        //            default:
        //                _Priority = ThreadPriority.Normal;
        //                break;
        //        }

        //        _module = null;
        //        try
        //        {
        //            string[] instance = { ReadAttribute(node, "assembly"), ReadAttribute(node, "class") };

        //            if(string.IsNullOrEmpty(instance[0]))
        //                throw new Exception("The file of the assembly has not been specified.");

        //            if(string.IsNullOrEmpty(instance[1]))
        //                throw new Exception("The class to instantiate has not been specified.");

        //            Assembly assembly = Assembly.LoadFile(_modulesFolder + instance[0]);
        //            _module = (ServiceModule)assembly.CreateInstance(instance[1], true);
        //            if(_module == null)
        //                throw new Exception(string.Format("Class not found: '{0}'.", instance[1]));

        //            _dbModule = new Module(_Name);
        //            if (!_dbModule.Load())
        //            {
        //                _dbModule.Name = _Name;
        //                _dbModule.Assembly = instance[0];
        //                _dbModule.Class = instance[1];
        //                _dbModule.Priority = priority;
        //                _dbModule.NextRun = DateTime.MaxValue;
        //            }
        //            else
        //                _dbModule.Priority = priority;
        //            _dbModule.Save();
        //        }
        //        catch(Exception ex)
        //        {
        //            Global.Log.Write2(LogSeverity.Critical, ex, _Name);
        //        }

        //        _nextRun = GetNextRun(_Name);
        //    }

        //    /// <summary>
        //    /// Indica se questo modulo può e deve essere eseguito.
        //    /// </summary>
        //    public bool Ready
        //    {
        //        get { return (!_running) && (_module != null) && (DateTime.Now >= _nextRun); }
        //    }

        //    /// <summary>
        //    /// Esegue le operazioni del modulo.
        //    /// </summary>
        //    public void Run()
        //    {
        //        if(_running)
        //        {
        //            Global.Log.Write2(LogSeverity.Critical, "Module already running.", _Name);
        //            return;
        //        }

        //        _running = true;

        //        try
        //        {
        //            if(Thread.CurrentThread.Priority != _Priority)
        //                Thread.CurrentThread.Priority = _Priority;

        //            DateTime? nextRun = _module.Run();
        //            if(nextRun.HasValue)
        //                _nextRun = nextRun.Value;
        //        }
        //        catch(Exception ex)
        //        {
        //            Global.Log.Write2(LogSeverity.Critical, ex, _Name);
        //        }

        //        SetNextRun(_Name, _nextRun);
        //        _running = false;
        //    }

        //    /// <summary>
        //    /// Notifica al modulo di fermare l'esecuzione.
        //    /// </summary>
        //    public void Abort()
        //    {
        //        if(_running)
        //        {
        //            try
        //            {
        //                _module.Abort();
        //            }
        //            catch(Exception ex)
        //            {
        //                Global.Log.Write2(LogSeverity.Critical, ex, _Name);
        //            }
        //        }
        //    }

        //    /// <summary>
        //    /// Rilascia le risorse utilizzate.
        //    /// </summary>
        //    public void Dispose()
        //    {
        //        if(_module is IDisposable)
        //        {
        //            try
        //            {
        //                (_module as IDisposable).Dispose();
        //            }
        //            catch(Exception ex)
        //            {
        //                Global.Log.Write2(LogSeverity.Critical, ex, _Name);
        //            }
        //        }

        //        if(_module != null)
        //            _module = null;
        //    }
        //}
        //#endregion
    }
}