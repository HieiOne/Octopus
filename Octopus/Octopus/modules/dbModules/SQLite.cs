using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Octopus.modules.dbModules
{
    class SQLite : DataSource
    {
        public override void BeginTransaction()
        {
            throw new NotImplementedException();
        }

        public override void CloseReader()
        {
            throw new NotImplementedException();
        }

        public override void CommitTransaction()
        {
            throw new NotImplementedException();
        }

        public override void Connect()
        {
            Console.WriteLine("Success SQLite");
        }

        public override void Disconnect()
        {
            throw new NotImplementedException();
        }

        public override int ExecuteQuery()
        {
            throw new NotImplementedException();
        }

        public override void OpenReader()
        {
            throw new NotImplementedException();
        }

        public override void OpenReader(int limit)
        {
            throw new NotImplementedException();
        }
    }
}
