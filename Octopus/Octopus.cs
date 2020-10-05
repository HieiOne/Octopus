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
                    dataTable.ExtendedProperties.Add("ToServer", table.ToServer);
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
            //Console.Read();
        }

        /// <summary>
        /// Class we use for the configuration table list
        /// </summary>
        class TableElement
        { 
            public string Name { get; set; }
            public string FromDatabase { get; set; }
            public string FromServer { get; set; }
            public string ToServer { get; set; }

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
                tableElement.FromServer = instance.FromServer;
                tableElement.ToServer = instance.ToServer;
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
                List<DataSource> FromDataSources = new List<DataSource>();
                List<DataSource> ToDataSources = new List<DataSource>();

                (FromDataSources, ToDataSources) = ReadDbDefinitions(dataTableList.Select(x => x.ExtendedProperties["FromServer"].ToString()).Distinct().ToList()
                                                                                         ,dataTableList.Select(x => x.ExtendedProperties["ToServer"].ToString()).Distinct().ToList()
                                                                                         ,dataTableList);
                foreach (DataTable dataTable in dataTableList)
                {
                    //TODO check for SQL Injection
                    Console.WriteLine(dataTable.TableName);
                    FromDataSources[Convert.ToInt32(dataTable.ExtendedProperties["FromServerIndex"].ToString())].ReadTable(dataTable);
                    ToDataSources[Convert.ToInt32(dataTable.ExtendedProperties["ToServerIndex"].ToString())].WriteTable(dataTable);
                    Console.WriteLine("==================================================="); //Little separator

                }
            }
            Messages.WriteSuccess("DONE");
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
        public static (List<DataSource> fromDataSource, List<DataSource> toDataSource) ReadDbDefinitions(List<string> fromServer, List<string> toServer, List<DataTable> dataTableList)
        {
            // read file into a string and deserialize JSON to a type
            DbDefinitionList dbList = JsonConvert.DeserializeObject<DbDefinitionList>(File.ReadAllText(@".\DbDefinitions.json"));
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
                {
                    fromDataSource.Add(Activator.CreateInstance(objectType, dbDefinition.connectionString) as DataSource);

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
                {
                    toDataSource.Add(Activator.CreateInstance(objectType,dbDefinition.connectionString) as DataSource);
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
}
