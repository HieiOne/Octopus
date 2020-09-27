using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Octopus.modules.dbModules
{
    public abstract class DataSource
    {
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
        public abstract void OpenReader();

        /// <summary>
        /// SELECT query with limit of results
        /// </summary>
        /// <param name="limit"></param>
        public abstract void OpenReader(int limit);

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
    }
}
