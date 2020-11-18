using System;
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

            Run(OctopusConfig.dataTableList);
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

        /// <summary>
        /// Run called after Main recovering the config values
        /// </summary>
        /// <param name="dataTableList"></param>
        static void Run(List<DataTable> dataTableList)
        {            
            string fromServer = OctopusConfig.fromServer;
            string toServer = OctopusConfig.toServer;
            long lMemoryMB;
            Messages.WriteSuccess("Start of process: " + DateTime.Now.ToString());

            if (string.IsNullOrEmpty(fromServer) || string.IsNullOrEmpty(toServer))
            {
                Messages.WriteError("The configuration of fromServer or toServer is empty");
            }
            else
            {
                List<DataSource> FromDataSources = new List<DataSource>();
                List<DataSource> ToDataSources = new List<DataSource>();

                (FromDataSources, ToDataSources) = ReadDbDefinitions(dataTableList.Select(x => x.ExtendedProperties["FromServer"].ToString()).Distinct().ToList()
                                                                                         ,dataTableList.Select(x => x.ExtendedProperties["ToServer"].ToString()).Distinct().ToList()
                                                                                         ,dataTableList);

                int processedTableCount = 0;
                foreach (DataTable dataTable in dataTableList)
                {
                    //TODO check for SQL Injection
                    //Messages.WriteQuestion(dataTable.TableName);
                    lMemoryMB = GC.GetTotalMemory(true/* true = Collect garbage before measuring */) / 1024 / 1024; // memory in megabytes
                    ProgressBar.WriteProgressBar(processedTableCount*100/dataTableList.Count,processedTableCount,dataTableList.Count, lMemoryMB, true, dataTable.TableName);
                    FromDataSources[Convert.ToInt32(dataTable.ExtendedProperties["FromServerIndex"].ToString())].ReadTable(dataTable);
                    ToDataSources[Convert.ToInt32(dataTable.ExtendedProperties["ToServerIndex"].ToString())].WriteTable(dataTable);
                    Messages.WriteQuestion("==================================================="); //Little separator

                    /* Dispose of the used dataTable to clear memory */
                    dataTable.PrimaryKey = null;
                    dataTable.Rows.Clear();
                    dataTable.Columns.Clear();
                    dataTable.Clear();
                    GC.Collect(); //Force collect
                    processedTableCount++;
                }
            }

            Messages.WriteSuccess("End of process: " + DateTime.Now.ToString());
            Messages.WriteSuccess("DONE");
        }

        /// <summary>
        /// Reads JSON Dbdefinitions and tries to instantiate the objects requested by App.config file
        /// </summary>
        /// <param name="fromServer"></param>
        /// <param name="toServer"></param>
        /// <returns></returns>
        public static (List<DataSource> fromDataSource, List<DataSource> toDataSource) ReadDbDefinitions(List<string> fromServer, List<string> toServer, List<DataTable> dataTableList)
        {
            // read file into a string and deserialize JSON to a type
            DbDefinitionList dbList = DbDefinitionList.JSONDbDefinitions(@".\DbDefinitions.json");
            List<DataSource> fromDataSource = new List<DataSource>();
            List<DataSource> toDataSource = new List<DataSource>();

            //Create array with the values so we can use it later to filter the dbDefinitionList

            //Generate from Server
            #region GenerateFromServer
            foreach (DbDefinition dbDefinition in dbList.dbDefinitions
                                                            .Where(x => fromServer.Contains(x.name)) //Limit from the string list
                                                            .ToList<DbDefinition>()
            )
            {
                string connectionString;

                if (!dbDefinition.fromServer)
                {
                    Messages.WriteError($"{fromServer} is not implemented yet as origin BD");
                    throw new NotImplementedException();
                }

                #region CheckConnectionString
                //Check to control that the connection string is replenished if not throw error
                try
                {
                    connectionString = OctopusConfig.connectionKeyValues[dbDefinition.connectionString];
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        Messages.WriteError($"{fromServer} connection string {dbDefinition.connectionString} is not set or is empty");
                        throw new NotImplementedException();
                    }
                }
                catch (Exception)
                {
                    Messages.WriteError($"{fromServer} connection string {dbDefinition.connectionString} is not set or is empty");
                    throw;
                }
                #endregion

                string objectToInstantiate = $"Octopus.modules.dbModules.{dbDefinition.className}, Octopus";
                var objectType = Type.GetType(objectToInstantiate);

                if (!(objectType is null))
                {
                    fromDataSource.Add(Activator.CreateInstance(objectType, connectionString) as DataSource);

                    //For each datatable that the fromServer name coincides with the processed fromServer we add the latest index
                    foreach (DataTable dataTable in dataTableList.Where(x => x.ExtendedProperties["FromServer"].ToString() == dbDefinition.name))
                    {
                        dataTable.ExtendedProperties.Add("FromServerIndex", fromDataSource.Count-1); //Minus one because the count starts from 0
                    }

                }
                
            }
            #endregion

            //Generate to Server
            #region GenerateToServer
            foreach (DbDefinition dbDefinition in dbList.dbDefinitions
                                                .Where(x => toServer.Contains(x.name)) //Limit from the string list
                                                .ToList<DbDefinition>()
            )
            {
                string connectionString;

                if (!dbDefinition.toServer)
                {
                    Messages.WriteError($"{toServer} is not implemented yet as destiny BD");
                    throw new NotImplementedException();
                }

                #region CheckConnectionString
                //Check to control that the connection string is replenished if not throw error
                try
                {
                    connectionString = OctopusConfig.connectionKeyValues[dbDefinition.connectionString];
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        Messages.WriteError($"{toServer} connection string {dbDefinition.connectionString} is not set or is empty");
                        throw new NotImplementedException();
                    }
                }
                catch (Exception)
                {
                    Messages.WriteError($"{toServer} connection string {dbDefinition.connectionString} is not set or is empty");
                    throw;
                }
                #endregion

                string objectToInstantiate = $"Octopus.modules.dbModules.{dbDefinition.className}, Octopus";
                var objectType = Type.GetType(objectToInstantiate);

                if (!(objectType is null))
                {
                    toDataSource.Add(Activator.CreateInstance(objectType, connectionString) as DataSource);
                    //For each datatable that the toServer name coincides with the processed toServer we add the latest index
                    foreach (DataTable dataTable in dataTableList.Where(x => x.ExtendedProperties["ToServer"].ToString() == dbDefinition.name))
                    {
                        dataTable.ExtendedProperties.Add("ToServerIndex", toDataSource.Count-1); //Minus one because the count starts from 0
                    }
                }


            }
            #endregion

            if (fromDataSource.Count == 0 || toDataSource.Count == 0) //If any datasource was not found for whatever reason, throw
            {
                Messages.WriteError($"{fromServer} or {toServer} module not found");
                throw new NotImplementedException();
            }

            bool error = false; // We do it this way so we can present all of the errors to the user at once

            //Check every table has an index to a datasource
            foreach (DataTable dataTable in dataTableList)
            { 
                if (!(dataTable.ExtendedProperties.ContainsKey("FromServerIndex")))
                {
                    Messages.WriteError($"The server {dataTable.ExtendedProperties["FromServer"].ToString()} couldn't be found for table {dataTable.TableName}");
                    error = true;
                }
                if (!(dataTable.ExtendedProperties.ContainsKey("ToServerIndex")))
                {
                    Messages.WriteError($"The server {dataTable.ExtendedProperties["ToServer"].ToString()} couldn't be found for table {dataTable.TableName}");
                    error = true;
                }
            }

            if (error) 
            {
                Console.Read();
                throw new NotImplementedException();
            }

            return (fromDataSource, toDataSource);
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