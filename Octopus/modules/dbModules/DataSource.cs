﻿using Octopus.modules.messages;
using System;
using System.Data;
using System.Data.Common;

namespace Octopus.modules.dbModules
{
    public abstract class DataSource
    {
        public string dataSourceName { get; set; } //DataSource name
        public bool fromServer { get; set; } //Indicates if this dataSource is ready to be used as origin
        public bool toServer { get; set; } //Indicates if this dataSource is ready to be used as destination
        public string connectionStringName { get; set; } //Indicates the string id that contains the connection string

        /// <summary>
        /// Forces users to add the method to generate the dictionaries
        /// </summary>
        public abstract void GenerateTypeDictionaries();

        /// <summary>
        /// Opens connection to the datasource
        /// </summary>
        public abstract void Connect();

        /// <summary>
        /// Closes connection to the datasource
        /// </summary>
        public abstract void Disconnect();

        /// <summary>
        /// SELECT query
        /// </summary>
        public abstract void OpenReader(string query);

        /// <summary>
        /// Closes reader used for SELECT query
        /// </summary>
        public abstract void CloseReader();

        /// <summary>
        /// Runs any query and returns an int with the modified registers
        /// </summary>
        /// <returns></returns>
        /// <param name="query"></param>
        public abstract int ExecuteQuery(string query);

        /// <summary>
        /// TTSBegin in SQL
        /// </summary>
        public abstract void BeginTransaction();

        /// <summary>
        /// TTSCommit in SQL
        /// </summary>
        public abstract void CommitTransaction();

        /// <summary>
        /// Adds all rows of the table to the datatable
        /// </summary>
        /// <param name="dataTable"></param>
        /// <returns>Returns the number of lines processed</returns>
        public abstract int AddRows(DataTable dataTable);

        /// <summary>
        /// Adds the dataschema to the datatable
        /// </summary>
        /// <param name="dataTable"></param>
        public abstract void AddSchema(DataTable dataTable);

        /// <summary>
        /// Checks if the source is connected or not
        /// </summary>
        /// <returns></returns>
        public abstract bool IsConnected();

        /// <summary>
        /// Drops table from Db
        /// </summary>
        /// <param name="tableName"></param>
        public abstract void DropTable(string tableName);

        /// <summary>
        /// Creates a table (in case it doesn't already exist) from a dataTable object
        /// </summary>
        /// <param name="dataTable"></param>
        public abstract void CreateTable(DataTable dataTable);

        /// <summary>
        /// Insert rows from a dataTable to db
        /// </summary>
        /// <param name="dataTable"></param>
        public abstract void InsertRows(DataTable dataTable);

        /// <summary>
        /// Opens a reader selecting all from the specified tableName
        /// </summary>
        /// <param name="tableName"></param>
        public abstract void SelectAll(string tableName);

        /// <summary>
        /// Checks if table exists and returns bool
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public abstract bool TableExists(string tableName);

        /// <summary>
        /// Load dataReader rows to a dataTable
        /// </summary>
        /// <param name="dataReader"></param>
        /// <param name="dataTable"></param>
        /// <returns>Count of processed rows</returns>
        public int LoadDataTable(DbDataReader dataReader, DataTable dataTable)
        {
            int count = 0;

            if (!(dataReader.IsClosed) && dataReader.HasRows)
            {
                while (dataReader.Read() && count < OctopusConfig.batchSize)
                {
                    Object[] values = new Object[dataReader.FieldCount];

                    try
                    {
                        dataReader.GetValues(values);
                        dataTable.Rows.Add(values);
                    }
                    catch (OutOfMemoryException)
                    {
                        Messages.WriteError("Run out of memory for the table");
                        return count; //We return it so even if it runs out of memory we can keep running after the rows are cleaned
                    }
                    catch (InvalidCastException exception)
                    {
                        values = LoadDataTableException(values, dataTable, exception);
                        dataTable.Rows.Add(values);
                    }
                    finally
                    {
                        count++;
                    }
                }

                //If the quantity processed is different than the batch size, there's no more rows in the table, in case it ends exactly at that point by sheer coincidence in the next run it will end
                if (count != OctopusConfig.batchSize)
                    CloseReader();

                return count;
            }

            Messages.WriteSuccess($"Added all the rows of the table to a dataTable object {dataTable.TableName} succesfully");
            return count;
        }

        /// <summary>
        /// In case of InvalidCastException in LoadDataTable it will call a personalized way of loading rows in the module
        /// </summary>
        /// <param name="values"></param>
        /// <param name="dataTable"></param>
        /// <param name="exception"></param>
        /// <returns>Returns the same param values but replenished</returns>
        protected abstract Object[] LoadDataTableException(Object[] values, DataTable dataTable, Exception exception = null);
    }
}
