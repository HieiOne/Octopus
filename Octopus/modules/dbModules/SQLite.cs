using Microsoft.Data.Sqlite;
using Octopus.modules.messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Octopus.modules.dbModules
{
    class SQLite : DataSource
    {
        private readonly SqliteConnection sqliteConnection;
        private SqliteDataReader dataReader;
        public Dictionary<string, Type> test = new Dictionary<string, Type>();

        public Dictionary<string, Type> SQLTypeToCShartpType = new Dictionary<string, Type>();
        public Dictionary<Type, string> CShartpTypeToSQLType = new Dictionary<Type, string>();

        public SQLite() //Conexión a BD SQLite
        {
            string connectionString = ConfigurationManager.ConnectionStrings["SQLiteConnectionString"].ConnectionString;

            if (string.IsNullOrEmpty(connectionString))
            {
                Messages.WriteError("SQLiteConnectionString not found, please specify it in the App.Config");
                throw new NotImplementedException();
            }
            sqliteConnection = new SqliteConnection(connectionString);
            GenerateTypeDictionaries(); //Generate type dictionaries for mapping types
        }

        public override void BeginTransaction()
        {
            throw new NotImplementedException();
        }

        public override void CloseReader()
        {
            dataReader.Close();
        }

        public override void CommitTransaction()
        {
            throw new NotImplementedException();
        }

        public override void Connect()
        {
            try
            {
                sqliteConnection.Open();
                Messages.WriteSuccess("Connected to SQLite succesfully");
            }
            catch (Exception e)
            {
                Messages.WriteError(e.Message);
                throw;
            }
        }

        public override void Disconnect()
        {
            try
            {
                sqliteConnection.Close();
                Messages.WriteSuccess("Disconnected from SQLite succesfully");
            }
            catch (Exception e)
            {
                Messages.WriteError(e.Message);
                throw;
            }
        }

        public override int ExecuteQuery()
        {
            throw new NotImplementedException();
        }

        public override void OpenReader(string query)
        {
            SqliteCommand readSqlite = new SqliteCommand(query, sqliteConnection);

            try
            {
                dataReader = readSqlite.ExecuteReader();
            }
            catch (SqliteException e) when (e.ErrorCode == -2147467259)
            {
                Messages.WriteError(e.Message);
                //return 0; //Error
            }
        }

        public override void OpenReader(string query, int limit)
        {
            throw new NotImplementedException();
        }

        public override void ReadTable(DataTable dataTable)
        {
            Connect(); // Connect to the DB

            GetSchemaTable(dataTable);

            Disconnect(); // Disconnects from the DB
        }

        /// <summary>
        /// Adds the dataschema to the datatable
        /// </summary>
        /// <param name="dataTable"></param>
        private void GetSchemaTable(DataTable dataTable)
        {
            string query = $"SELECT [cid],[name],[type],[notnull],[dflt_value],[pk] FROM PRAGMA_TABLE_INFO('{dataTable.TableName}')";
            OpenReader(query);

            if (dataReader.HasRows)
            {
                while (dataReader.Read())
                {
                    string dataType = dataReader.GetString(2);
                    string dimension = dataType.Split('(', ')')[1]; //We get value between parenthesis
                    int lenght = -1, precision = 0;

                    if (!string.IsNullOrEmpty(dimension))
                    {
                        dataType = dataType.Split('(', ')')[0];
                        if (dimension.Contains(",")) //Has precision
                        {
                            lenght = Convert.ToInt32(dimension.Split(',')[0]);
                            precision = Convert.ToInt32(dimension.Split(',')[1]);
                        }
                        else 
                        {
                            lenght = Convert.ToInt32(dimension);
                        }
                    }

                    DataColumn dataColumn = new DataColumn();

                    dataColumn.ColumnName = dataReader.GetString(1);
                    dataColumn.DataType = SQLTypeToCShartpType[dataType];
                    dataColumn.AllowDBNull = !dataReader.GetBoolean(3); //En SQL Lite el campo es NOT NULL entonces revertimos el valor
                    dataColumn.DefaultValue = dataReader.IsDBNull(4) ? null : dataReader.GetString(4);
                    dataColumn.Unique = dataReader.GetInt32(5) != 0 ? true : false;
                    dataColumn.ExtendedProperties.Add("SQL Type", dataReader.GetString(2));
                    dataColumn.ExtendedProperties.Add("Precision", precision);
                    dataColumn.MaxLength = lenght;

                    //TODO Column of primary keys
                    dataTable.Columns.Add(dataColumn);
                }
                Messages.WriteSuccess($"Generated the schema of the table {dataTable.TableName} succesfully");
            }
            else
            {
                Messages.WriteError($"The table {dataTable.TableName} has no columns or wasn't found");
                throw new NotImplementedException();
            }

            CloseReader();
        }

        /// <summary>
        /// Replenishes the dictionaries SQLTypeToCShartpType & CShartpTypeToSQLType
        /// </summary>
        public override void GenerateTypeDictionaries()
        {
            AddToDictionaries("INTEGER", typeof(Int32));
            AddToDictionaries("TEXT", typeof(string));
            AddToDictionaries("REAL", typeof(decimal));
            AddToDictionaries("BLOB", typeof(Byte[]));
            AddToDictionaries("DATETIME", typeof(DateTime));

            //Local function to add to both dictionaries
            void AddToDictionaries(string sqlType, Type cSharpType)
            {
                SQLTypeToCShartpType.Add(sqlType, cSharpType);
                CShartpTypeToSQLType.Add(cSharpType, sqlType);
            }
        }

    }
}
