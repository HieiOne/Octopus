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
        /// <summary>
        /// Hashtable which converts SQL Type to C# Type
        /// </summary>
        /// 
        /*
        protected abstract Dictionary<string, Type> SQLTypeToCShartpType { get; set; }
        //TODO Forced Dictionaries
        /// <summary>
        /// Hashtable which converts C# Type to SQL Type
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
        public abstract int ExecuteQuery();

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
        /// </summary>
        public abstract void ReadTable(DataTable dataTable);
    }
}
