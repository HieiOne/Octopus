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

namespace Octopus
{
    class Octopus
    {
        static void Main(string[] args)
        {
            List<DataTable> dataTableList = new List<DataTable>();
            List<string> tableList;

            string prefix = ConfigurationManager.AppSettings.Get("prefix") + "_";
            string suffix = "_" + ConfigurationManager.AppSettings.Get("suffix");
            
            if (prefix.Length == 1) // Without value
                prefix = null;

            if (suffix.Length == 1) // Without value
                suffix = null;

            Debug.WriteLine("Reading Config file . . .");
            tableList = ConfigurationTableList();

            if (tableList.Count() != 0)
            {
                Debug.WriteLine("Read Config file succesfully");

                foreach (string table in tableList) //Convert the list of strings to Data Tables
                {
                    DataTable dataTable = new DataTable();
                    dataTable.TableName = table;
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
        /// Reads from configuration list the section TableList, converts it to a List<string> and returns it
        /// </summary>
        /// <returns>List<string></returns>
        static List<string> ConfigurationTableList()
        {
            var applicationSettings = ConfigurationManager.GetSection("TableList") as NameValueCollection;
            List<string> tableList = new List<string>();

            if (applicationSettings.Count != 0)
            {
                foreach (var key in applicationSettings.AllKeys)
                {
                    tableList.Add(applicationSettings[key]);
                }
            }

            return tableList;
        }

        /// <summary>
        /// Run called after Main recovering the config values
        /// </summary>
        /// <param name="dataTableList"></param>
        static void Run(List<DataTable> dataTableList)
        {
            string fromDB = ConfigurationManager.AppSettings.Get("fromDB");
            string toDB = ConfigurationManager.AppSettings.Get("toDB");

            if (string.IsNullOrEmpty(fromDB) || string.IsNullOrEmpty(toDB))
            {
                Messages.WriteError("The configuration of fromDB or toDB is empty");
            }
            else
            {
                (DataSource fromDataSource, DataSource toDataSource) = ReadDbDefinitions(fromDB, toDB);

                fromDataSource.Connect();
                toDataSource.Connect();

                foreach (DataTable dataTable in dataTableList)
                {
                    Console.WriteLine(dataTable.TableName);
                }
            }
        }
        class DbDefinitionList
        {
            public List<DbDefinition> dbDefinitions { get; set; }

        }

        class DbDefinition
        {
            public string name { get; set; }
            public bool fromDB { get; set; }
            public bool toDB { get; set; }
            public string className { get; set; }
        }

        public static (DataSource fromDataSource, DataSource toDataSource) ReadDbDefinitions(string fromDB, string toDB)
        {
            // read file into a string and deserialize JSON to a type
            DbDefinitionList dbList = JsonConvert.DeserializeObject<DbDefinitionList>(File.ReadAllText(@".\DbDefinitions.json"));
            DataSource fromDataSource = null, toDataSource = null;


            //TODO Filter results to get only the two ones we want
            foreach (DbDefinition dbDefinition in dbList.dbDefinitions)
            {
                if (dbDefinition.name == fromDB) // When matching the selected Datasource and has the value from DB true
                {
                    if (!dbDefinition.fromDB)
                    {
                        throw new NotImplementedException();
                    }

                    string objectToInstantiate = $"Octopus.modules.dbModules.{dbDefinition.className}, Octopus";
                    var objectType = Type.GetType(objectToInstantiate);

                    if (!(objectType is null))
                        fromDataSource = Activator.CreateInstance(objectType) as DataSource;
                }

                if (dbDefinition.name == toDB) // When matching the selected Datasource and has the value to DB true
                {
                    if (!dbDefinition.toDB)
                    {
                        throw new NotImplementedException();
                    }

                    string objectToInstantiate = $"Octopus.modules.dbModules.{dbDefinition.className}, Octopus";
                    var objectType = Type.GetType(objectToInstantiate);

                    if (!(objectType is null))
                        toDataSource = Activator.CreateInstance(objectType) as DataSource;
                }
            }


            if (fromDataSource is null || toDataSource is null)
            {
                throw new NotImplementedException();
            }

            return (fromDataSource, toDataSource);
        }

    }
}
