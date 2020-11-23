using Octopus.modules.messages;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace Octopus.modules.dbModules
{
    public class SQLServer : DataSource
    {        
        private readonly SqlConnection sqlConnection;
        private SqlTransaction sqlTransaction;
        private SqlDataReader dataReader;

        public Dictionary<string, Type> SQLTypeToCShartpType = new Dictionary<string, Type>();
        public Dictionary<Type, string> CShartpTypeToSQLType = new Dictionary<Type, string>();

        public SQLServer(string sqlConnectionString) //Initial construct of SQL Server
        {
            string connectionString = sqlConnectionString;

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


        public override bool TableExists(string tableName)
        {
            //TODO Add config for db and schema
            bool exists = false;
            string query = $"SELECT CASE WHEN OBJECT_ID('{OctopusConfig.toDB}.dbo.{tableName}', 'U') IS NOT NULL THEN 1 ELSE 0 END";
            
            OpenReader(query);

            if (!(dataReader.HasRows)) //If it has no rows
            {
                exists =  false;
            }
            else
            {
                if (dataReader.Read())
                {
                    exists = dataReader.GetInt32(0) == 0 ? false : true;
                }
            }

            CloseReader();
            return exists;
        }

        public override void DropTable(string tableName)
        {
            string query = $"DROP TABLE {OctopusConfig.toDB}.dbo.{tableName}";

            if (TableExists(tableName))
            {
                ExecuteQuery(query);
                Messages.WriteSuccess($"Table {tableName} removed succesfully");
            }
            else
            {
                Messages.WriteWarning($"Table {tableName} couldn't be removed either because of permissions or because the table doesn't exist");
            }
        }

        public override void CreateTable(DataTable dataTable)
        {
            string query;

            if (TableExists($"{dataTable.Prefix}{ dataTable.TableName}")) // If it doesn't exist we create
            {
                Messages.WriteQuestion($"Table {dataTable.Prefix}{dataTable.TableName} already exists");
            }
            else //Create
            {
                StringBuilder builder = new StringBuilder();

                query = $"CREATE TABLE {OctopusConfig.toDB}.dbo.{dataTable.Prefix}{dataTable.TableName} ( "; // Inicio de Create
                builder.Append(query);

                int i = 1; //Start at 1 because the property Count doesn't start from 0
                foreach (DataColumn dataColumn in dataTable.Columns)
                {
                    string nullOrNot = dataColumn.AllowDBNull ? "NULL" : "NOT NULL";
                    string typeName = CShartpTypeToSQLType[dataColumn.DataType];

                    //This string will be used to define lenght and precision e.g (17,2)
                    string lenghtAndPrecision = null; //As starters empty

                    if (dataColumn.ExtendedProperties["Lenght"].ToString() != "-1" && typeName != "INT" && typeName != "DATETIME" && typeName != "VARBINARY(MAX)")
                    {
                        if (dataColumn.ExtendedProperties["Precision"].ToString() != "0")
                        {
                            lenghtAndPrecision = $"({dataColumn.ExtendedProperties["Lenght"].ToString()},{dataColumn.ExtendedProperties["Precision"].ToString()})";
                        }
                        else
                        {
                            lenghtAndPrecision = $"({dataColumn.ExtendedProperties["Lenght"].ToString()})";
                        }
                    }

                    if (i == dataTable.Columns.Count) // If its last item
                    {
                        query = $" {dataColumn.ColumnName} {typeName} {lenghtAndPrecision} {nullOrNot}";
                    }
                    else // If its not last item
                    {
                        query = $" {dataColumn.ColumnName} {typeName} {lenghtAndPrecision} {nullOrNot},";
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

        public override void InsertRows(DataTable dataTable)
        {
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(sqlConnection,SqlBulkCopyOptions.Default,sqlTransaction))
            {
                //TODO add Schema
                bulkCopy.DestinationTableName = $"{OctopusConfig.toDB}.dbo.{dataTable.Prefix}{dataTable.TableName}";
                bulkCopy.BulkCopyTimeout = 90; //90 seconds of timeout

                try
                {
                    // Write from the source to the destination.
                    bulkCopy.WriteToServer(dataTable);
                }
                catch (Exception e)
                {
                    Messages.WriteError(e.Message);
                    //throw;
                }
            }
        }

        public override void GenerateTypeDictionaries()
        {
            AddToDictionaries("INT", typeof(Int32));
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

        public override int AddRows(DataTable dataTable)
        {
            throw new NotImplementedException();
        }

        public override void AddSchema(DataTable dataTable)
        {
            throw new NotImplementedException();
        }

        public override bool IsConnected()
        {
            if (sqlConnection.State == ConnectionState.Open)
                return true;

            return false;
        }

        public override void SelectAll(string tableName)
        {
            throw new NotImplementedException();
        }
    }
}
