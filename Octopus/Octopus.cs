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
            bool protectSection = false;
            bool unprotectSection = false;
            string connectionStringName = null;
            int batchSize = 10000; //Default value .- It should be configured accordingly to your available memory ram

            var p = new OptionSet() {
                { "c|config=", "Indicates which config file will be used (default App.config)",
                    v => configPath = v },
                { "t|testString=", "Tests all dataSource that might have the connection string specified in the parameter, returns true if any connects succesfully",
                    v => connectionStringName = v },
                { "b|batchSize=", "Indicates how many rows will be processed per batch (default 10000)",
                    v => batchSize = Convert.ToInt32(v) },
                { "p|protectSection", "Protects connection string section",
                    v => protectSection = v != null },
                { "u|unprotectSection", "Unprotects connection string section to add new ones",
                    v => unprotectSection = v != null },
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


            OctopusConfig.batchSize = batchSize;
            
            if (verbosity > 0)
            {
                //TODO active console or design verbosiy
                OctopusConfig.console_verbosity = verbosity;
            }

            if (protectSection)
            {
                ConfigurationFile configurationFile = new ConfigurationFile(configPath);
                configurationFile.EncryptConnectionString();
                return;
            }

            if (unprotectSection)
            {
                ConfigurationFile configurationFile = new ConfigurationFile(configPath);
                configurationFile.DecryptConnectionString(true);
                return;
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

            if (!(string.IsNullOrEmpty(connectionStringName)))
            {
                OctopusHandler.TestConnectionString(connectionStringName);
                return;
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
        readonly Dictionary<string, DataSource> dataSources = new Dictionary<string, DataSource>();
        long MemoryMB;

        public OctopusHandler() 
        {
            OctopusFactory octopusFactory = new OctopusFactory();
            dataSources = octopusFactory.GenerateDataSource();
        }

        public void Run()
        {
            int proccessedTables = 0;
            int totalTables = OctopusConfig.dataTableList.Count;

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

                ProcessTable(dataTable, fromDataSource, toDataSource);
                CleanDataTable(dataTable); // Dispose of the used dataTable to clear memory

                proccessedTables++;
            }

            DisconnectAll();
            Messages.WriteSuccess("End of process: " + DateTime.Now.ToString());
        }

        /// <summary>
        /// Moves the data from the origin to the destination
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="originSource"></param>
        /// <param name="destinationSource"></param>
        public void ProcessTable(DataTable dataTable, DataSource originSource, DataSource destinationSource)
        {
            if (!(originSource.IsConnected()))
                originSource.Connect();

            if (!(destinationSource.IsConnected()))
                destinationSource.Connect();

            //originSource
            originSource.AddSchema(dataTable);
            originSource.SelectAll(dataTable.TableName);

            destinationSource.BeginTransaction(); //TTSBegin, we create everything or nothing per dataTable
            destinationSource.DropTable($"{dataTable.Prefix}{dataTable.TableName}");
            destinationSource.CreateTable(dataTable); //TODO Check if table has changes and update instead of dropping and creating.

            while (originSource.AddRows(dataTable) > 0) //As long as the returned rows is more than 0
            {
                if (dataTable.Rows.Count > 0) //If it has any rows
                    destinationSource.InsertRows(dataTable);

                dataTable.Rows.Clear(); //Clear the already processed rows
            }

            destinationSource.CommitTransaction(); //TTSCommit, we create everything or nothing
            Messages.WriteSuccess("Commited Changes");
        }

        /// <summary>
        /// Disconnects all dataSources that were opened
        /// </summary>
        public void DisconnectAll()
        {
            foreach (DataSource dataSource in dataSources.Values)
            {
                if(dataSource != null && dataSource.IsConnected())
                    dataSource.Disconnect();
            }
        }

        public void CleanDataTable(DataTable dataTable) 
        {
            dataTable.PrimaryKey = null;
            dataTable.Rows.Clear();
            dataTable.Columns.Clear();
            dataTable.Clear();
            dataTable.Reset();
            GC.Collect(); //Force collect
        }

        /// <summary>
        /// Tests all dataSource that might have the connection string specified in the parameter, returns true if any connects succesfully
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static bool TestConnectionString(string connectionStringName)
        {
            bool testStatus = false;
            OctopusHandler octopusHandler = new OctopusHandler(); //We call the construct of the class
            List<DataSource> dataSources = new List<DataSource>();

            try
            {
                dataSources = octopusHandler.dataSources.Values.Where(x => x.connectionStringName == connectionStringName).ToList();
            }
            catch (Exception)
            {
                Messages.WriteError($"Connection with {connectionStringName} failed");
                return false;
            }

            if (dataSources.Count == 0)
            {
                Messages.WriteError($"Connection string -> {connectionStringName} <- was not configured in any Db definition, please correct it and run again.");
            }
            else
            {
                foreach (DataSource dataSource in dataSources)
                {
                    dataSource.Connect();

                    //As long as it connects to any dataSource we return true
                    if (dataSource.IsConnected())
                    {
                        Messages.WriteSuccess($"Connection to {dataSource.dataSourceName} was succesful");
                        testStatus = true;
                    }
                    else
                        Messages.WriteError($"Connection to {dataSource.dataSourceName} failed");
                }
            }

            return testStatus;
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
                    dataSource.connectionStringName = dbDefinition.connectionString;

                    if (dbDefinition.toServer)
                        dataSource.toServer = true;

                    if (dbDefinition.fromServer)
                        dataSource.fromServer = true;
                }
            }

            return dataSource;
        }

        /// <summary>
        /// Returns a dictionary of dataSources already instantiated
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, DataSource> GenerateDataSource()
        {
            Dictionary<string, DataSource> dataSources = new Dictionary<string, DataSource>();

            string JsonFileName = "DbDefinitions.json";
            string JsonPath = AppDomain.CurrentDomain.BaseDirectory + JsonFileName;

            DbDefinitionList dbDefinitions = ReadJson(JsonPath);

            foreach (DbDefinition dbDefinition in dbDefinitions.dbDefinitions)
            {
                dataSources.Add(dbDefinition.name, InstantiateDataSource(dbDefinition));
            }

            return dataSources;
        }
    }

    static class OctopusConfig 
    {
        //This class is destined to contain the config and have it accesible from everywhere
        public static int batchSize;
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
            ConfigurationFile configurationFile = new ConfigurationFile(path);
            configurationFile.DecryptConnectionString();
            Configuration configuration = configurationFile.configuration;

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

            configurationFile.EncryptConnectionString();
        }
    }
}