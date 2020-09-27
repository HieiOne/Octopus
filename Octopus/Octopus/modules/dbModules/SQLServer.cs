using Octopus.modules.messages;
using System;
using System.Collections.Generic;
using System.Configuration;
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

        public SQLServer() //Initial construct of SQL Server
        {
            string connectionString = ConfigurationManager.ConnectionStrings["SQLServerConnectionString"].ConnectionString;

            if (string.IsNullOrEmpty(connectionString)) 
            {
                Messages.WriteError("SQLServerConnectionString not found, please specify it in the App.Config");
                throw new NotImplementedException();
            }
            sqlConnection = new SqlConnection(connectionString);
        }

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
