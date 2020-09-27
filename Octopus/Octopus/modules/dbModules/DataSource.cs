using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Octopus.modules.dbModules
{
    public abstract class DataSource
    {
        public abstract void Connect();
        public abstract void Disconnect();
        public abstract void OpenReader();
        public abstract void CloseReader();
        public abstract int ExecuteQuery();
        public abstract void BeginTransaction();
        public abstract void CommitTransaction();
    }
}
