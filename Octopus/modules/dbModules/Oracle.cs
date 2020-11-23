using Octopus.modules.messages;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Octopus.modules.dbModules
{
    public class Oracle : DataSource
    {
        private readonly OracleConnection oracleConnection;
        private OracleTransaction oracleTransaction;
        private OracleDataReader dataReader;

        public Dictionary<string, Type> SQLTypeToCShartpType = new Dictionary<string, Type>();
        public Dictionary<Type, string> CShartpTypeToSQLType = new Dictionary<Type, string>();

        public Oracle(string sqlConnectionString)
        {
            string connectionString = sqlConnectionString;

            if (string.IsNullOrEmpty(connectionString))
            {
                Messages.WriteError("OracleConnectionString not found, please specify it in the App.Config");
                throw new NotImplementedException();
            }

            oracleConnection = new OracleConnection(connectionString);
            GenerateTypeDictionaries();
        }

        public override void BeginTransaction()
        {
            oracleTransaction = oracleConnection.BeginTransaction();
        }

        public override void CloseReader()
        {
            dataReader.Close();
        }

        public override void CommitTransaction()
        {
            oracleTransaction.Commit();
        }

        public override void Connect()
        {
            try
            {
                oracleConnection.Open();           
                Messages.WriteSuccess("Connected to Oracle Server succesfully");
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
                oracleConnection.Close();
                Messages.WriteSuccess("Disconnected from Oracle Server succesfully");
            }
            catch (Exception e)
            {
                Messages.WriteError(e.Message);
                throw;
            }
        }

        public override int ExecuteQuery(string query)
        {
            throw new NotImplementedException();
        }

        public override void OpenReader(string query)
        {
            OracleCommand readOracle = new OracleCommand(query, oracleConnection);

            try
            {
                dataReader = readOracle.ExecuteReader();                
            }
            catch (OracleException e) //when (e.ErrorCode == -2147467259)
            {
                Messages.WriteError(e.Message);
                //return 0; //Error
            }
        }

        public override void GetSchemaTable(DataTable dataTable)
        {
            string query = $"SELECT COLUMN_ID,COLUMN_NAME,DATA_TYPE,NULLABLE,NULL as DEFAULT_VALUE,0 as PK,DATA_LENGTH,NVL(DATA_PRECISION,0) FROM USER_TAB_COLUMNS WHERE TABLE_NAME = '{dataTable.TableName}' ORDER BY 1";
            OpenReader(query);

            if (dataReader.HasRows)
            {
                while (dataReader.Read())
                {
                    string dataType = dataReader.GetString(2);
                    int lenght = dataReader.GetInt32(6) + dataReader.GetInt32(7); //This is done to avoid the precision and number to excess the Lenght of the field
                    if (dataType == "NUMBER")
                    {
                        if (lenght > 38)
                            lenght = 38;
                    }

                    DataColumn dataColumn = new DataColumn();

                    dataColumn.ColumnName = dataReader.GetString(1);
                    dataColumn.DataType = SQLTypeToCShartpType[dataType];
                    dataColumn.AllowDBNull = dataReader.GetString(3) == "Y" ? true : false; //En SQL Lite el campo es NOT NULL entonces revertimos el valor
                    dataColumn.DefaultValue = dataReader.IsDBNull(4) ? null : dataReader.GetString(4);
                    dataColumn.ExtendedProperties.Add("SQL_Type", dataReader.GetString(2));
                    dataColumn.ExtendedProperties.Add("Lenght", lenght );
                    dataColumn.ExtendedProperties.Add("Precision", dataReader.GetInt32(7));
                    dataColumn.ExtendedProperties.Add("Primary_Key_Order", dataReader.GetInt32(5));

                    dataTable.Columns.Add(dataColumn);
                }

                //Add primary key columns to dataTable
                DataColumn[] dataColumns = dataTable.Columns.Cast<DataColumn>()
                                                            .Where(x => x.ExtendedProperties["Primary_Key_Order"].ToString() != "0")
                                                            .OrderBy(z => z.ExtendedProperties["Primary_Key_Order"]) //Order by pk id
                                                            .ToArray();
                dataTable.PrimaryKey = dataColumns;

                Messages.WriteSuccess($"Generated the schema of the table {dataTable.TableName} succesfully");
            }
            else
            {
                Messages.WriteError($"The table {dataTable.TableName} has no columns or wasn't found");
                //throw new NotImplementedException();
            }

            CloseReader();
        }

        public override int GetRowsTable(DataTable dataTable)
        {
            if (!(dataReader.IsClosed) && dataReader.HasRows)
            {
                int r;
                for (r = 0; r < OctopusConfig.batchSize; r++)
                {
                    if (dataReader.Read())
                    {
                        try
                        {
                            DataRow dataRow = dataTable.NewRow();
                            Object[] values = new Object[dataReader.FieldCount];

                            try
                            {
                                dataReader.GetValues(values);
                            }
                            catch (InvalidCastException)
                            {
                                //We continue to load from the first null to avoid re-doing processed fields
                                for (int i = values.ToList().IndexOf(null); i < dataTable.Columns.Count; i++)
                                {
                                    DataColumn dataColumn = dataTable.Columns[i]; // I rather have it in a different variable and ref it later                       
                                    object columnValue;
                                    try
                                    {
                                        columnValue = dataReader.GetValue(i);
                                    }
                                    catch (InvalidCastException) when (dataReader.GetOracleValue(i) is OracleDecimal)
                                    {
                                        columnValue = (decimal)(OracleDecimal.SetPrecision(dataReader.GetOracleDecimal(i), 28));
                                    }
                                    values[i] = columnValue;
                                }
                            }
                            dataTable.LoadDataRow(values, true);
                        }
                        catch (OutOfMemoryException)
                        {
                            Messages.WriteError("Run out of memory for the table");
                            break;
                        }
                    }
                    else
                    {
                        CloseReader();
                        return r;
                    }
                }
                return r;
            }

            Messages.WriteSuccess($"Added all the rows of the table to a dataTable object {dataTable.TableName} succesfully");
            CloseReader();
            return 0;
        }

        public override void GenerateTypeDictionaries()
        {
            //https://docs.microsoft.com/es-es/dotnet/framework/data/adonet/oracle-data-type-mappings
            AddToDictionaries("NUMBER", typeof(decimal),true);
            AddToDictionaries("VARCHAR2", typeof(string),true);
            AddToDictionaries("ROWID", typeof(string));
            AddToDictionaries("CLOB", typeof(string));
            AddToDictionaries("FLOAT", typeof(decimal));
            AddToDictionaries("BLOB", typeof(Byte[]),true);
            AddToDictionaries("RAW", typeof(Byte[]));
            AddToDictionaries("XMLTYPE", typeof(Byte[]));
            AddToDictionaries("DATE", typeof(DateTime),true);
            AddToDictionaries("TIMESTAMP(3)", typeof(DateTime));
            AddToDictionaries("TIMESTAMP(6)", typeof(DateTime));

            //Local function to add to both dictionaries
            //Primary parameter is used to specify which one should be the one used by default
            void AddToDictionaries(string sqlType, Type cSharpType, bool primary=false)
            {
                SQLTypeToCShartpType.Add(sqlType, cSharpType);
                if(primary)
                    CShartpTypeToSQLType.Add(cSharpType, sqlType);
            }
        }

        public override bool IsConnected()
        {
            if (oracleConnection.State == ConnectionState.Open)
                return true;

            return false;
        }

        public override void DropTable(string tableName)
        {
            throw new NotImplementedException();
        }

        public override void CreateTable(DataTable dataTable)
        {
            throw new NotImplementedException();
        }

        public override void InsertRows(DataTable dataTable)
        {
            throw new NotImplementedException();
        }

        public override void SelectAll(string tableName)
        {
            string query = $"SELECT * FROM {tableName}";
            OpenReader(query);

            if(!(dataReader.HasRows))
                Messages.WriteError($"The table {tableName} has no rows or wasn't found");
        }

        public override bool TableExists(string tableName)
        {
            throw new NotImplementedException();
        }
    }
}
