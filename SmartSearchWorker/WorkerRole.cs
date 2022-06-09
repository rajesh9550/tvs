using System;
using System.Net;
using System.Threading;
using System.Data;
using System.Linq;
using System.IO;

using CMS.Base;
using CMS.Core;
using CMS.EventLog;
using CMS.Helpers;
using CMS.DataEngine;
using CMS.Search;
using CMS.WebFarmSync;

using Microsoft.WindowsAzure.ServiceRuntime;

using PathHelper = CMS.AzureStorage.PathHelper;

namespace SmartSearchWorker
{
    /// <summary>
    /// Worker role designed to process smart search tasks
    /// </summary>
    public class WorkerRole : RoleEntryPoint
    {
        #region "Constants"

        /// <summary>
        /// Path to Smart search worker role log.
        /// </summary>
        private const string LOG_PATH = "App_Data/CMSModules/SmartSearch/workerlog.txt";

        #endregion


        #region "Public methods"

        /// <summary>
        /// Main method of worker role.
        /// </summary>
        public override void Run()
        {
            try
            {
                LogToFile("Starting worker role.");
                LogToFile("Connection string used: " + ConnectionHelper.ConnectionString);
                LogToFile("Storage provider used for Smart Search folder: " + CMS.IO.StorageHelper.GetStorageProvider("App_Data/CMSModules/SmartSearch").Name);

                if (!SmartSearchEnabled())
                {
                    while (!SmartSearchEnabled())
                    {
                        Thread.Sleep(AzureHelper.SEARCH_INIT_SLEEP);
                    }

                    while (true)
                    {
                        RoleEnvironment.RequestRecycle();
                    }
                }

                LogToFile("Initializing CMS application.");

                CMSApplication.WaitForDatabaseAvailable.Value = true;
                CMSApplication.Init();

                // Ensure that Search module is initialized and ready to use
                while (!ModuleEntryManager.Modules.Any(m => (m.Name == ModuleName.SEARCH) && (m.Module != null) && (m.Module.Initialized == true)))
                {
                    LogToFile("Initialization not successful, Search module not ready");
                    RoleEnvironment.RequestRecycle();
                }

                WebFarmContext.InstanceIsHiddenWebFarmServer = true;

                LogToFile("Starting to process search tasks.");
                
                // Infinite loop for processing tasks
                while (true)
                {
                    try
                    {
                        // Clear cache and all the hashtables
                        CacheHelper.ClearCache(null, logTask: false);
                        ModuleManager.ClearHashtables(false);

                        SearchTaskInfoProvider.ProcessTasks(false, true);
                        Thread.Sleep(AzureHelper.SEARCH_PROCESS_SLEEP);
                    }
                    catch (Exception ex)
                    {
                        // Catch all exceptions and log them
                        LogException(ex);

                        var policy = new LoggingPolicy(TimeSpan.FromMinutes(15));
                        EventLogProvider.LogException("SmartSearchWorker", "Run", ex, loggingPolicy: policy);
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }


        /// <summary>
        /// On start event.
        /// </summary>
        public override bool OnStart()
        {
            try
            {
                CMSApplication.PreInit();

#pragma warning disable BH1014 // Do not use System.IO
                // Delete the log file if needed
                if (File.Exists(LOG_PATH))
                {
                    File.Delete(LOG_PATH);
                }
#pragma warning restore BH1014 // Do not use System.IO

                // Set the maximum number of concurrent connections 
                ServicePointManager.DefaultConnectionLimit = 12;

                // Get path for Temp
                LocalResource temp = RoleEnvironment.GetLocalResource("AzureTemp");
                PathHelper.TempPath = temp.RootPath;

                // Get path for Cache
                LocalResource cache = RoleEnvironment.GetLocalResource("AzureCache");
                PathHelper.CachePath = cache.RootPath;

                SystemEvents.RestartRequired.Execute += (sender, args) => RoleEnvironment.RequestRecycle();

                // Provide the configuration to the engine
                CMSAppSettings.GetApplicationSettings += GetApplicationSettings;
                CMSConnectionStrings.GetConnectionString += GetApplicationSettings;

                // Set Azure deployment
                AzureHelper.DeploymentID = RoleEnvironment.DeploymentId;
            }
            catch (Exception ex)
            {
                LogException(ex);
            }

            return base.OnStart();
        }

        #endregion


        #region "Private methods"

        /// <summary>
        /// Reads settings from service configuration file. 
        /// </summary>
        /// <param name="key">Setting key.</param>    
        private string GetApplicationSettings(string key)
        {
            try
            {
                return RoleEnvironment.GetConfigurationSettingValue(key);
            }
            catch
            {
                // Setting key was not found
                return null;
            }
        }


        /// <summary>
        /// Returns whether is smart search module enabled.
        /// </summary>
        private bool SmartSearchEnabled()
        {
            try
            {
                DataSet ds = ConnectionHelper.ExecuteQuery("SELECT KeyValue FROM CMS_SettingsKey WHERE KeyName = 'CMSSearchIndexingEnabled'", null, QueryTypeEnum.SQLQuery);
                return DataHelper.GetBoolValue(ds.Tables[0].Rows[0], "KeyValue");
            }
            catch (Exception ex)
            {
                LogException(ex);
                return false;
            }
        }


        /// <summary>
        /// Logs given exception.
        /// </summary>
        /// <param name="ex">Exception</param>
        private void LogException(Exception ex)
        {
            LogToFile(String.Format("Message: {0}, stack trace: {1}, type: {2}", ex.Message, ex.StackTrace, ex.GetType()));
        }


        /// <summary>
        /// Logs given message to Smart search worker role log.
        /// </summary>
        /// <param name="message">Message that is going to be logged.</param>
        private void LogToFile(string message)
        {
#pragma warning disable BH1014 // Do not use System.IO
            var dir = Path.GetDirectoryName(LOG_PATH);
            Directory.CreateDirectory(dir);
            File.AppendAllText(LOG_PATH, String.Format("{0}: {1}{2}", DateTime.Now, message, Environment.NewLine));
#pragma warning restore BH1014 // Do not use System.IO
        }

        #endregion
    }
}
