using HoneyInPacifier.Core.Bases;
using HoneyInPacifier.Utils;
using Infra.CrossCutting.Library.ExtensionsMethod;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Configuration;
using System.Data.Common;
using System.Diagnostics;

namespace HoneyInPacifier.Connection.Model
{
    public class DatabaseConnectionModel : DatabaseObject<DatabaseConnectionModel>
    {
        ~DatabaseConnectionModel() => Dispose();

        public DatabaseConnectionModel(string connectionStringName)
        {
            SetDbConnectionString(connectionStringName);
            SetProviderName(connectionStringName);
            SetProviderConnection();
            SetSymbolParameter();
        }

        public DatabaseConnectionModel(DbConnection db)
        {
            SetDbConnection(db);
            SetSymbolParameter();
        }

        public new static DatabaseConnectionModel Instance(string connectionStringName) => new DatabaseConnectionModel(connectionStringName);

        public new static DatabaseConnectionModel Instance(DbConnection db) => new DatabaseConnectionModel(db);

        public DbConnection DbConnection { get; private set; }

        private string ConnectionString { get; set; }

        public string ProviderName { get; private set; }

        public string Variable { get; private set; }

        private void SetProviderConnection()
        {
            if (Debugger.IsAttached)
            {
                if ("Oracle.ManagedDataAccess.Client".Equals(ProviderName))
                {
                    DbConnection = new OracleConnection(ConnectionString);
                }
                else
                {
                    DbConnection = DbProviderFactories.GetFactory(ProviderName).CreateConnection();
                    DbConnection.ConnectionString = ConnectionString;

                }
            }
            else
            {
                DbConnection = DbProviderFactories.GetFactory(ProviderName).CreateConnection();
                DbConnection.ConnectionString = ConnectionString;

            }

        }

        private void SetDbConnection(DbConnection dbConnection)
        {
            if (string.IsNullOrWhiteSpace(dbConnection.ConnectionString))
            {
                dbConnection.ConnectionString = DesConnectString(ConnectionString);
            }

            DbConnection = dbConnection;
        }

        private void SetDbConnectionString(string connectionStringName)
        {
            ConnectionString = DesConnectString(ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString);
        }

        private void SetProviderName(string connectionStringName)
        {
            ProviderName = ConfigurationManager.ConnectionStrings[connectionStringName].ProviderName;
        }

        private string DesConnectString(string connectionString)
        {
            int pwdPosStart = connectionString.IndexOf("{{", StringComparison.Ordinal) + 2;
            int pwdPosEnd = connectionString.IndexOf("}}", StringComparison.Ordinal);

            if (pwdPosStart > 0 && pwdPosStart < pwdPosEnd)
            {
                string pwdEncrypted = connectionString.Substring(pwdPosStart, pwdPosEnd - pwdPosStart);
                string password = DatabaseCrypt.Decrypt(pwdEncrypted);
                connectionString = connectionString.Replace("{{" + pwdEncrypted + "}}", password);
            }

            return connectionString;
        }

        private void SetSymbolParameter()
        {
            if (ProviderName.IsNotEmpty())
            {
                if (ProviderName.ToLower().ToStringOrEmpty().Contains("oracle"))
                {
                    Variable = ":";
                }
                else if (ProviderName.ToLower().ToStringOrEmpty().Contains("sqlclient"))
                {
                    Variable = "@";
                }
                else if (ProviderName.ToLower().ToStringOrEmpty().Contains("mysql"))
                {
                    Variable = ":";
                }
                else if (ProviderName.ToLower().ToStringOrEmpty().Contains("ole"))
                {
                    Variable = "@";
                }
                else if (ProviderName.ToLower().ToStringOrEmpty().Contains("db2"))
                {
                    Variable = "?";
                }
                else
                {
                    Variable = ":";
                }
            }
            else
            {
                Variable = ":";
            }
        }
    }
}