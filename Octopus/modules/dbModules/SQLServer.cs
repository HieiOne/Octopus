using Octopus.modules.messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Octopus.modules.dbModules
{
    class SQLServer : DataSource
    {        
        private readonly SqlConnection sqlConnection;
        private SqlTransaction sqlTransaction;
        private SqlDataReader dataReader;

        public Dictionary<string, Type> SQLTypeToCShartpType = new Dictionary<string, Type>();
        public Dictionary<Type, string> CShartpTypeToSQLType = new Dictionary<Type, string>();

        public SQLServer() //Initial construct of SQL Server
        {
            string connectionString = ConfigurationManager.ConnectionStrings["SQLServerConnectionString"].ConnectionString;

            if (string.IsNullOrEmpty(connectionString)) 
            {
                Messages.WriteError("SQLServerConnectionString not found, please specify it in the App.Config");
                throw new NotImplementedException();
            }
            sqlConnection = new SqlConnection(connectionString);
            GenerateTypeDictionaries();
        }

        public override void BeginTransaction()
        {
            sqlTransaction = sqlConnection.BeginTransaction();
        }

        public override void CloseReader()
        {
            dataReader.Close();
        }

        public override void CommitTransaction()
        {
            sqlTransaction.Commit();
        }

        public override void Connect()
        {
            try
            {
                sqlConnection.Open();
                Messages.WriteSuccess("Connected to SQL Server succesfully");
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
                sqlConnection.Close();
                Messages.WriteSuccess("Disconnected from SQL Server succesfully");
            }
            catch (Exception e)
            {
                Messages.WriteError(e.Message);
                throw;
            }
        }

        public override int ExecuteQuery(string query)
        {
            SqlCommand sqlCommand;
            SqlDataAdapter sqlDataAdapter = new SqlDataAdapter();

            sqlCommand = new SqlCommand(query, sqlConnection, sqlTransaction);
            sqlDataAdapter.UpdateCommand = sqlCommand;

            try
            {
                return sqlDataAdapter.UpdateCommand.ExecuteNonQuery();
            }
            catch (SqlException e) when (e.ErrorCode == -2146232060) //No se puede quitar la tabla {table.newName} porque no existe o el usuario no tiene permiso.
            {
                Messages.WriteError(e.Message);
                return 0; //Error
                //throw;
            }
            catch (Exception e)
            {
                Messages.WriteError(e.Message);
                throw;
            }
        }

        public override void OpenReader(string query)
        {
            SqlCommand readSQLServer = new SqlCommand(query, sqlConnection, sqlTransaction);

            try
            {
                dataReader = readSQLServer.ExecuteReader();
            }
            catch (SqlException e) // when (e.ErrorCode == -2147467259)
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
            throw new NotImplementedException();
        }

        public override void WriteTable(DataTable dataTable)
        {
            Connect(); // Connect to the DB
            BeginTransaction(); //TTSBegin, we create everything or nothing

            CreateTable(dataTable);
            //GetRowsTable(dataTable);

            CommitTransaction(); //TTSCommit, we create everything or nothing
            Messages.WriteSuccess("Commited changes");
            Disconnect(); // Disconnects from the DB
        }

        /// <summary>
        /// Creates a table (in case it doesn't already exist) from a dataTable object
        /// </summary>
        /// <param name="dataTable"></param>
        private void CreateTable(DataTable dataTable)
        {
            //Add config for db and schema
            string query = $"SELECT CASE WHEN OBJECT_ID('MAXI.dbo.{dataTable.TableName}', 'U') IS NOT NULL THEN 1 ELSE 0 END";
            OpenReader(query);

            if (!(dataReader.HasRows)) //If it has no rows
            {
                CloseReader(); //Close Reader even if it has no rows
            }
            else
            { 
                bool exists = false;

                if (dataReader.Read())
                {
                    exists = dataReader.GetInt32(0) == 0 ? false : true;
                }

                CloseReader(); //Close Reader after reading it

                if (exists) // If it doesn't exist we create
                {
                    Messages.WriteQuestion($"Table {dataTable.TableName} already exists");
                }
                else //Create
                {
                    //TODO table new name (prefix and suffix)
                    StringBuilder builder = new StringBuilder();

                    query = $"CREATE TABLE MAXI.dbo.{dataTable.TableName} ( "; // Inicio de Create
                    builder.Append(query);

                    int i = 1; //Start at 1 because the property Count doesn't start from 0
                    foreach (DataColumn dataColumn in dataTable.Columns)
                    {
                        string nullOrNot = dataColumn.AllowDBNull ? "NULL" : "NOT NULL";
                        string typeName = CShartpTypeToSQLType[dataColumn.DataType];

                        //TODO precision and lenght
                        if (i == dataTable.Columns.Count) // If its last item
                        {
                            query = $" {dataColumn.ColumnName} {typeName} {nullOrNot}";
                        }
                        else // If its not last item
                        {
                            query = $" {dataColumn.ColumnName} {typeName} {nullOrNot},";
                        }
                        i++;
                        builder.Append(query);
                    }

                    i = 1;
                    foreach (DataColumn dataColumn in dataTable.PrimaryKey)
                    {
                        if (dataTable.PrimaryKey.Length == 1) //We add this check because the first one may be the last one aswel
                        {
                            query = $" PRIMARY KEY ({dataColumn.ColumnName})";
                        }
                        else if (i == 1) //First iteration, we do it here because a table might not have any PK
                        {
                            query = $" PRIMARY KEY ({dataColumn.ColumnName},";
                        }
                        else if (i == dataTable.PrimaryKey.Length) //Last iteration
                        {
                            query = $"{dataColumn.ColumnName})";
                        }
                        else  //Neither first nor last
                        {
                            query = $"{dataColumn.ColumnName},";
                        }

                        i++;
                        builder.Append(query);
                    }

                    //Add last )
                    builder.Append(")");

                    Messages.WriteExecuteQuery("Creating table in destination. . . .");
                    int result = ExecuteQuery(builder.ToString()); //Run CREATE query
                    
                    if (result == -1) //Query success, it returns -1 when is okay
                        Messages.WriteSuccess("Table created!");
                }
            }
        }

        public override void GenerateTypeDictionaries()
        {
            AddToDictionaries("INTEGER", typeof(Int32));
            AddToDictionaries("NVARCHAR", typeof(string)); // We dont use varchar so we can support Unicode strings
            AddToDictionaries("DECIMAL", typeof(decimal));
            AddToDictionaries("VARBINARY(MAX)", typeof(Byte[]));
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
