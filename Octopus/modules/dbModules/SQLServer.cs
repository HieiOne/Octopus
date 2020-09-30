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
        private readonly SqlTransaction sqlTransaction;
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
            sqlConnection.BeginTransaction();
        }

        public override void CloseReader()
        {
            throw new NotImplementedException();
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

        public override int ExecuteQuery()
        {
            throw new NotImplementedException();
        }

        public override void OpenReader(string query)
        {
            SqlCommand readSQLServer = new SqlCommand(query, sqlConnection);
            //TODO Doesnt get the transaction
            try
            {
                dataReader = readSQLServer.ExecuteReader();
            }
            catch (Exception e) // when (e.ErrorCode == -2147467259)
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

        public override void GenerateTypeDictionaries()
        {
            AddToDictionaries("INTEGER", typeof(Int32));

            //Local function to add to both dictionaries
            void AddToDictionaries(string sqlType, Type cSharpType)
            {
                SQLTypeToCShartpType.Add(sqlType, cSharpType);
                CShartpTypeToSQLType.Add(cSharpType, sqlType);
            }
        }

        public override void WriteTable(DataTable dataTable)
        {
            Connect(); // Connect to the DB
            //BeginTransaction(); //TTSBegin, we create everything or nothing

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

            if (dataReader.HasRows)
            {
                bool exists = false;
                while (dataReader.Read())
                {
                    exists = dataReader.GetInt32(0) == 0 ? false : true;
                }

                if (exists) // If it doesn't exist we create
                {
                    Messages.WriteQuestion($"Table {dataTable.TableName} already exists");
                }
                else //Create
                {
                    //TODO table new name (prefix and suffix)
                    query = $"CREATE TABLE MAXI.dbo.{dataTable.TableName} ( "; // Inicio de Create

                    List<string> columns = new List<string>();
                    string[] primaryKeysColumns;

                    foreach (DataColumn dataColumn in dataTable.Columns)
                    {
                        string nullOrNot = dataColumn.AllowDBNull ? "NULL" : "NOT NULL";
                        columns.Add($"{dataColumn.ColumnName} VARCHAR(MAX) {nullOrNot}");
                    }

                    query = String.Join(",", columns);
                    Console.Write("");

                }
            }
            

            CloseReader();
        }
    }
}
