using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Octopus.modules.dbModules
{
    public abstract class DataSource
    {
        public string dataSourceName { get; set; } //DataSource name
        public bool fromServer { get; set; } //Indicates if this dataSource is ready to be used as origin
        public bool toServer { get; set; } //Indicates if this dataSource is ready to be used as destination

        /// <summary>
        /// Dictionary which converts SQL Type to C# Type
        /// </summary>
        /// 
        /*
        protected abstract Dictionary<string, Type> SQLTypeToCShartpType { get; set; }
        //TODO Forced Dictionaries
        /// <summary>
        /// Dictionary which converts C# Type to SQL Type
        /// </summary>
        protected abstract Dictionary<Type, string> CShartpTypeToSQLType { get; set; }
        */

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
        /// SELECT query with limit of results
        /// </summary>
        /// <param name="limit"></param>
        public abstract void OpenReader(string query, int limit);

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
        /// Reads the table name and adds all columns and registers into the DataTable object
        /// This class must call other methods to create the Schema (Columns + Keys) of the table and add the datarows
        /// </summary>
        /// <param name="dataTable"></param>
        //public abstract void ReadTable(DataTable dataTable);

        /// <summary>
        /// Creates the table (if it doesnt exist) and adds all of the rows in the DataTable set
        /// This class must call other methods to create the table in destiny and then bulk copy the rows
        /// </summary>
        /// <param name="dataTable"></param>
        //public abstract void WriteTable(DataTable dataTable);


        /// <summary>
        /// Adds all rows of the table to the datatable
        /// </summary>
        /// <param name="dataTable"></param>
        public abstract void GetRowsTable(DataTable dataTable);

        /// <summary>
        /// Adds the dataschema to the datatable
        /// </summary>
        /// <param name="dataTable"></param>
        public abstract void GetSchemaTable(DataTable dataTable);

        /// <summary>
        /// Checks if the source is connected or not
        /// </summary>
        /// <returns></returns>
        public abstract bool IsConnected();


        public abstract void DropTable(string tableName);

        public abstract void CreateTable(DataTable dataTable);

        public abstract void InsertRows(DataTable dataTable);

    }
}
