﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Specialized;
using System.Diagnostics;
using Octopus.modules.messages;
using System.Data;
using Octopus.modules.dbModules;
using Octopus.modules.ConfigurationSettings;
using NDesk.Options;
using System.IO;
using Newtonsoft.Json;

namespace Octopus
{
    class Octopus
    {
        static int verbosity;

        static void Main(string[] args)
        {
            string configPath = null;
            bool show_help = false;

            var p = new OptionSet() {          
                { "c|config=", "the {NAME} of someone to greet.",
                    v => configPath = v },
                { "v", "increase debug message verbosity",
                  v => { if (v != null) ++verbosity; } },
                { "h|help",  "show this message and exit",
                  v => show_help = v != null },
            };

            List<string> extra;
            try
            {
                extra = p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("Octopus: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `Octopus --help' for more information.");
                return;
            }

            if (show_help)
            {
                ShowHelp(p);
                return;
            }

            #if DEBUG //If in debug increase the verbosity automatically
                verbosity++;
            #endif

            if (verbosity > 0)
            {
                //TODO active console or design verbosiy
                OctopusConfig.console_verbosity = verbosity;
            }

            if (string.IsNullOrEmpty(configPath))
            {
                OctopusConfig.LoadConfig();
                Messages.WriteQuestion("Using default config");
            }
            else
            {
                if (File.Exists(configPath))
                {
                    OctopusConfig.LoadConfig(configPath);
                    Messages.WriteQuestion("Using config.. " + configPath);
                }
                else
                {
                    Messages.WriteError("Config file: " + configPath + " doesn't exist");
                    return;
                }
            }

            OctopusHandler octopusHandler = new OctopusHandler();
            octopusHandler.Run();
        }

        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: Octopus [OPTIONS]");
            //Console.WriteLine("Greet a list of individuals with an optional message.");
            //Console.WriteLine("If no message is specified, a generic greeting is used.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }
    }

    public class OctopusHandler
    {
        Dictionary<string, DataSource> dataSources = new Dictionary<string, DataSource>();
        long MemoryMB;

        public void Run()
        {
            int proccessedTables = 0;
            int totalTables = OctopusConfig.dataTableList.Count;

            GenerateDataSource(); //Fills the dataSources Dictionary
            Messages.WriteSuccess("Start of process: " + DateTime.Now.ToString());

            foreach (DataTable dataTable in OctopusConfig.dataTableList)
            {
                string fromServer = dataTable.ExtendedProperties["FromServer"].ToString();
                string toServer = dataTable.ExtendedProperties["ToServer"].ToString();
                DataSource fromDataSource = null;
                DataSource toDataSource = null;

                if (dataSources.ContainsKey(fromServer))
                    fromDataSource = dataSources[fromServer];

                if (dataSources.ContainsKey(toServer))
                    toDataSource = dataSources[toServer];

                if (fromDataSource == null || toDataSource == null)
                {
                    Messages.WriteError($"{dataTable.TableName}'s origin or destiny dataSource is not correctly defined");
                    continue;
                }

                if (!(fromDataSource.fromServer))
                {
                    Messages.WriteError($"{fromServer} is not implemented yet as origin BD");
                    continue;
                }

                if (!(toDataSource.toServer))
                {
                    Messages.WriteError($"{toServer} is not implemented yet as destiny BD");
                    continue;
                }

                MemoryMB = GC.GetTotalMemory(true) / 1024 / 1024; // memory in megabytes, true = Collect garbage before measuring
                ProgressBar.WriteProgressBar(proccessedTables * 100 / totalTables, proccessedTables, totalTables, MemoryMB, true, dataTable.TableName);

                fromDataSource.ReadTable(dataTable);
                toDataSource.WriteTable(dataTable);
                CleanDataTable(dataTable); // Dispose of the used dataTable to clear memory

                proccessedTables++;
            }
            Messages.WriteSuccess("End of process: " + DateTime.Now.ToString());
        }

        public void GenerateDataSource()
        {
            OctopusFactory octopusFactory = new OctopusFactory();
            string JsonFileName = "DbDefinitions.json";
            string JsonPath = AppDomain.CurrentDomain.BaseDirectory + JsonFileName;

            DbDefinitionList dbDefinitions = octopusFactory.ReadJson(JsonPath);

            foreach (DbDefinition dbDefinition in dbDefinitions.dbDefinitions)
            {
                dataSources.Add(dbDefinition.name,octopusFactory.InstantiateDataSource(dbDefinition));
            }
        }

        public void CleanDataTable(DataTable dataTable) 
        {
            dataTable.PrimaryKey = null;
            dataTable.Rows.Clear();
            dataTable.Columns.Clear();
            dataTable.Clear();
            GC.Collect(); //Force collect
        }
    }

    public class OctopusFactory
    {
        /// <summary>
        /// Deserialize JSON and returns a DbDefinitionList
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public DbDefinitionList ReadJson(string file)
        {
            return JsonConvert.DeserializeObject<DbDefinitionList>(File.ReadAllText(file));
        }

        /// <summary>
        /// Instantiates data source from a database definition and returns it
        /// </summary>
        /// <param name="dbDefinition"></param>
        public DataSource InstantiateDataSource(DbDefinition dbDefinition)
        {
            DataSource dataSource = null;
            string objectToInstantiate = $"Octopus.modules.dbModules.{dbDefinition.className}, Octopus";
            string connectionString = OctopusConfig.GetConnectionString(dbDefinition.connectionString);
            var objectType = Type.GetType(objectToInstantiate);

            if (!(objectType is null))
            {
                if (string.IsNullOrEmpty(connectionString))
                {
                    Messages.WriteError($"{dbDefinition.name} connection string {dbDefinition.connectionString} is not set or is empty");
                }
                else
                {
                    dataSource = Activator.CreateInstance(objectType, connectionString) as DataSource;

                    dataSource.dataSourceName = dbDefinition.name;

                    if (dbDefinition.toServer)
                        dataSource.toServer = true;

                    if (dbDefinition.fromServer)
                        dataSource.fromServer = true;
                }
            }

            return dataSource;
        }
    }

    static class OctopusConfig 
    {
        //This class is destined to contain the config and have it accesible from everywhere
        public static int console_verbosity;
        public static List<DataTable> dataTableList = new List<DataTable>(); //List of tables to process
        public static Dictionary<string, string> connectionKeyValues = new Dictionary<string, string>();
        public static string prefix; //Default values
        public static string fromServer, toServer; //Default values
        public static string fromDB,toDB; //Default values
        public static string logPath; //Default values

        /// <summary>
        /// Returns the connectionString
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetConnectionString(string key)
        {
            string connectionString = null;

            if (OctopusConfig.connectionKeyValues.ContainsKey(key))
            {
                connectionString = OctopusConfig.connectionKeyValues[key];
            }

            return connectionString;
        }

        /// <summary>
        /// This method reads the config file if the path is given then it will read and load that config if not, it will read the default one
        /// </summary>
        /// <param name="path"></param>
        public static void LoadConfig(string path = null)
        {
            Configuration configuration = null;
            if (path != null) // We load the specified config file
            {
                var map = new ExeConfigurationFileMap
                {
                    ExeConfigFilename = path,
                    LocalUserConfigFilename = path,
                    RoamingUserConfigFilename = path
                };

                configuration = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
            }
            else //We load the default config file
            {
                configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            }

            #region AppSettingsValues
            var appSettings = configuration.GetSection("appSettings") as AppSettingsSection;
            if (appSettings != null)
            {
                prefix = appSettings.Settings["prefix"].Value + "_";
                if (prefix.Length == 1) //Without value
                    prefix = null;

                fromServer = appSettings.Settings["fromServer"].Value;
                toServer = appSettings.Settings["toServer"].Value;
                fromDB = appSettings.Settings["fromDB"].Value;
                toDB = appSettings.Settings["toDB"].Value;
                logPath = appSettings.Settings["LogPath"].Value;
            }
            if (string.IsNullOrEmpty(fromServer) || string.IsNullOrEmpty(toServer))
            {
                Messages.WriteError("The configuration of fromServer or toServer is empty");
                throw new NotImplementedException();
            }
            #endregion

            #region ReadingTables
            List<TableElement> tableList;
            tableList = TableElement.ConfigurationTableList(configuration);

            Debug.WriteLine("Reading Config file . . .");

            if (tableList.Count() != 0)
            {
                Debug.WriteLine("Read Config file succesfully");

                foreach (TableElement table in tableList) //Convert the list of strings to Data Tables
                {
                    DataTable dataTable = new DataTable();
                    dataTable.TableName = table.Name;
                    //In case of null we fill the default value
                    dataTable.ExtendedProperties.Add("FromServer", string.IsNullOrEmpty(table.FromServer) ? fromServer : table.FromServer);
                    dataTable.ExtendedProperties.Add("ToServer", string.IsNullOrEmpty(table.ToServer) ? toServer : table.ToServer);
                    dataTable.ExtendedProperties.Add("FromDatabase", string.IsNullOrEmpty(table.FromDatabase) ? fromDB : table.FromDatabase);
                    dataTable.Prefix = prefix;
                    dataTableList.Add(dataTable);
                }
            }
            else
            {
                Messages.WriteError("No tables specified in App.Config");
            }
            #endregion

            #region ConnectionStrings
            var connectionSettings = configuration.GetSection("connectionStrings") as ConnectionStringsSection;
            if (connectionSettings != null)
            {
                foreach (ConnectionStringSettings connectionStringSettings in connectionSettings.ConnectionStrings)
                {
                    connectionKeyValues.Add(connectionStringSettings.Name, connectionStringSettings.ConnectionString);
                }
            }
            #endregion
        }
    }
}