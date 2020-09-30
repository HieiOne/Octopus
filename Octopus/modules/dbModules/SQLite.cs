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

        public SQLite() //Construct, creates the connection string and generates types from SQL to C#
        {
            //TODO Check the configuration exists previously and show error
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

        /// <summary>
        /// This class must call other methods to create the Schema (Columns + Keys) of the table and add the datarows
        /// </summary>
        /// <param name="dataTable"></param>
        public override void ReadTable(DataTable dataTable)
        {
            Connect(); // Connect to the DB

            GetSchemaTable(dataTable);
            GetRowsTable(dataTable);

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
                    string dimension,dataType = dataReader.GetString(2);
                    int lenght = -1, precision = 0;

                    // We use this try to catch the IndexOutOfRangeException because of splitting and not stop the program
                    try
                    {
                        dimension = dataType.Split('(', ')')[1]; //We get value between parenthesis to get lenght
                        dataType = dataType.Split('(', ')')[0];
                        if (dimension.Contains(",")) //If the value is separated by commas it must be a REAL with precision
                        {
                            lenght = Convert.ToInt32(dimension.Split(',')[0]);
                            precision = Convert.ToInt32(dimension.Split(',')[1]);
                        }
                        else
                        {
                            lenght = Convert.ToInt32(dimension);
                        }
                    }
                    catch (IndexOutOfRangeException)
                    {
                        //We do nothing we just keep everything as is
                        //Since Split fails because there's no "(" and ")" it doesn't have lenght nor precision and every will have their default values
                        //DataType it's kept as is since it already is a full-self type (without lenght or precision, usually a DATETIME)
                        //throw;
                    }

                    DataColumn dataColumn = new DataColumn();

                    dataColumn.ColumnName = dataReader.GetString(1);
                    dataColumn.DataType = SQLTypeToCShartpType[dataType];
                    dataColumn.AllowDBNull = !dataReader.GetBoolean(3); //En SQL Lite el campo es NOT NULL entonces revertimos el valor
                    dataColumn.DefaultValue = dataReader.IsDBNull(4) ? null : dataReader.GetString(4);
                    //dataColumn.Unique = dataReader.GetInt32(5) != 0 ? true : false;
                    dataColumn.ExtendedProperties.Add("SQL_Type", dataReader.GetString(2));
                    dataColumn.ExtendedProperties.Add("Precision", precision);
                    dataColumn.ExtendedProperties.Add("Lenght", lenght);
                    dataColumn.ExtendedProperties.Add("Primary_Key_Order", dataReader.GetInt32(5));

                    dataTable.Columns.Add(dataColumn);
                }

                //Add primary key columns to dataTable
                DataColumn[] dataColumns = dataTable.Columns.Cast<DataColumn>()
                                                            .Where(x => x.ExtendedProperties["Primary_Key_Order"].ToString() != "0") //TODO Find a better way to do this
                                                            .OrderBy(z => z.ExtendedProperties["Primary_Key_Order"]) //Order by pk id
                                                            .ToArray();
                dataTable.PrimaryKey = dataColumns;

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
        /// Adds all rows of the table to the datatable
        /// </summary>
        /// <param name="dataTable"></param>
        private void GetRowsTable(DataTable dataTable)
        {
            string query = $"SELECT * FROM '{dataTable.TableName}'";
            OpenReader(query);
            
            if (dataReader.HasRows)
            {
                while (dataReader.Read())
                {
                    DataRow dataRow = dataTable.NewRow();

                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        DataColumn dataColumn = dataTable.Columns[i]; // I rather have it in a different variable and ref it later

                        if (!(dataReader.GetValue(i) is DBNull))
                            dataRow[dataColumn] = Convert.ChangeType(dataReader.GetValue(i),dataColumn.DataType);
                    }

                    dataTable.Rows.Add(dataRow);
                }
                Messages.WriteSuccess($"Added all the rows of the table {dataTable.TableName} succesfully");
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

        public override void WriteTable(DataTable dataTable)
        {
            throw new NotImplementedException();
        }
    }
}
