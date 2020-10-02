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
using Newtonsoft.Json;
using System.IO;
using Octopus.modules.dbModules;
using Octopus.modules.ConfigurationSettings;

namespace Octopus
{
    class Octopus
    {
        static void Main(string[] args)
        {
            List<DataTable> dataTableList = new List<DataTable>();
            List<TableElement> tableList;

            string prefix = ConfigurationManager.AppSettings.Get("prefix") + "_";
            //string suffix = "_" + ConfigurationManager.AppSettings.Get("suffix");
            
            if (prefix.Length == 1) // Without value
                prefix = null;

            /*
             * if (suffix.Length == 1) // Without value
             *   suffix = null;
             */

            Debug.WriteLine("Reading Config file . . .");
            tableList = ConfigurationTableList();

            if (tableList.Count() != 0)
            {
                Debug.WriteLine("Read Config file succesfully");

                foreach (TableElement table in tableList) //Convert the list of strings to Data Tables
                {
                    DataTable dataTable = new DataTable();
                    dataTable.TableName = table.Name;
                    dataTable.ExtendedProperties.Add("FromServer", table.FromServer);
                    dataTable.ExtendedProperties.Add("FromDatabase", table.FromDatabase);
                    dataTable.Prefix = prefix;
                    dataTableList.Add(dataTable);
                }

                Run(dataTableList);
            }
            else
            {
                Messages.WriteError("No tables specified in App.Config");
            }
            Console.Read();
        }

        /// <summary>
        /// Class we use for the configuration table list
        /// </summary>
        class TableElement
        { 
            public string Name { get; set; }
            public string FromDatabase { get; set; }
            public string FromServer { get; set; }
        }

        /// <summary>
        /// Reads from configuration list the section TableList, converts it to a List<string> and returns it
        /// </summary>
        /// <returns>List<string></returns>
        static List<TableElement> ConfigurationTableList()
        {
            var tableConfig = (TableConfig)ConfigurationManager.GetSection("TableListConfig");
            List<TableElement> tableList = new List<TableElement>();

            // Loop through each instance in the TableInstanceCollection
            foreach (TableInstanceElement instance in tableConfig.TableInstances)
            {
                TableElement tableElement = new TableElement();
                tableElement.Name = instance.Name;
                tableElement.FromDatabase = instance.Database;
                tableElement.FromServer = instance.Server;
                tableList.Add(tableElement);
            }

            return tableList;
        }

        /// <summary>
        /// Run called after Main recovering the config values
        /// </summary>
        /// <param name="dataTableList"></param>
        static void Run(List<DataTable> dataTableList)
        {
            string fromServer = ConfigurationManager.AppSettings.Get("fromServer");
            string toServer = ConfigurationManager.AppSettings.Get("toServer");

            if (string.IsNullOrEmpty(fromServer) || string.IsNullOrEmpty(toServer))
            {
                Messages.WriteError("The configuration of fromServer or toServer is empty");
            }
            else
            {
                (DataSource fromDataSource, DataSource toDataSource) = ReadDbDefinitions(fromServer, toServer);

                foreach (DataTable dataTable in dataTableList)
                {
                    //TODO check for SQL Injection
                    Console.WriteLine(dataTable.TableName);
                    fromDataSource.ReadTable(dataTable);
                    toDataSource.WriteTable(dataTable);
                    Console.WriteLine("==================================================="); //Little separator

                }
            }
            Console.WriteLine("DONE");
        }

        /// <summary>
        /// List composed of DbDefinitions used to run a foreach
        /// </summary>
        class DbDefinitionList
        {
            public List<DbDefinition> dbDefinitions { get; set; }

        }

        /// <summary>
        /// Class for the DbDefinition JSON
        /// </summary>
        class DbDefinition
        {
            public string name { get; set; }
            public bool fromServer { get; set; }
            public bool toServer { get; set; }
            public string className { get; set; }
            public string connectionString { get; set; }
        }

        /// <summary>
        /// Reads JSON Dbdefinitions and tries to instantiate the objects requested by App.config file
        /// </summary>
        /// <param name="fromServer"></param>
        /// <param name="toServer"></param>
        /// <returns></returns>
        public static (DataSource fromDataSource, DataSource toDataSource) ReadDbDefinitions(string fromServer, string toServer)
        {
            // read file into a string and deserialize JSON to a type
            DbDefinitionList dbList = JsonConvert.DeserializeObject<DbDefinitionList>(File.ReadAllText(@".\DbDefinitions.json"));
            DataSource fromDataSource = null, toDataSource = null;

            //Create array with the values so we can use it later to filter the dbDefinitionList
            string[] bdConfig = new string[] { fromServer, toServer };
            foreach (DbDefinition dbDefinition in dbList.dbDefinitions
                                                            .Where(x => bdConfig.Contains(x.name))
                                                            .ToList<DbDefinition>()
            )
            {
                if (dbDefinition.name == fromServer) // When matching the selected Datasource and has the value from DB true
                {
                    if (!dbDefinition.fromServer)
                    {
                        Messages.WriteError($"{fromServer} is not implemented yet as origin BD");
                        throw new NotImplementedException();
                    }

                    #region CheckConnectionString
                    //Check to control that the connection string is replenished if not throw error
                    try
                    {
                        string connectionString = ConfigurationManager.ConnectionStrings[dbDefinition.connectionString].ConnectionString;
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
                        fromDataSource = Activator.CreateInstance(objectType) as DataSource;
                }

                if (dbDefinition.name == toServer) // When matching the selected Datasource and has the value to DB true
                {
                    if (!dbDefinition.toServer)
                    {
                        Messages.WriteError($"{toServer} is not implemented yet as destiny BD");
                        throw new NotImplementedException();
                    }

                    #region CheckConnectionString
                    //Check to control that the connection string is replenished if not throw error
                    try
                    {
                        string connectionString = ConfigurationManager.ConnectionStrings[dbDefinition.connectionString].ConnectionString;
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
                        toDataSource = Activator.CreateInstance(objectType) as DataSource;
                }
            }


            if (fromDataSource is null || toDataSource is null) //If any datasource was not found for whatever reason, throw
            {
                Messages.WriteError($"{fromServer} or {toServer} module not found");
                throw new NotImplementedException();
            }

            return (fromDataSource, toDataSource);
        }

    }
}
