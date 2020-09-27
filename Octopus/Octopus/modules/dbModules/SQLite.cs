using Microsoft.Data.Sqlite;
using Octopus.modules.messages;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Octopus.modules.dbModules
{
    class SQLite : DataSource
    {
        private readonly SqliteConnection sqliteConnection;
        private readonly SqliteDataReader dataReader;

        public SQLite() //Conexión a BD SQLite
        {
            string connectionString = ConfigurationManager.ConnectionStrings["SQLiteConnectionString"].ConnectionString;

            if (string.IsNullOrEmpty(connectionString))
            {
                Messages.WriteError("SQLiteConnectionString not found, please specify it in the App.Config");
                throw new NotImplementedException();
            }
            sqliteConnection = new SqliteConnection(connectionString);
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
                sqliteConnection.Open();
                Messages.WriteSuccess("Connected to SQLite succesfully");
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
                sqliteConnection.Close();
                Messages.WriteSuccess("Disconnected from SQLite succesfully");
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

        public override void ReadTable(DataTable dataTable)
        {
            Connect(); // Connect to the DB

            dataTable = sqliteConnection.GetSchema(dataTable.TableName);

            Disconnect(); // Disconnects from the DB
        }
    }
}
