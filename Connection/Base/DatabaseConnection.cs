using HoneyInPacifier.Connection.Model;
using HoneyInPacifier.Core.Bases;
using System;
using System.Data;
using System.Data.Common;

namespace HoneyInPacifier.Connection.Base
{
    public class DatabaseConnection : DatabaseObject<DatabaseConnection>
    {
        private readonly DatabaseConnectionModel _dbConnectionModel;

        ~DatabaseConnection() => Dispose();

        public DatabaseConnection(string connectionStringName)
        {
            _dbConnectionModel = DatabaseConnectionModel.Instance(connectionStringName);
        }

        public DatabaseConnection(DbConnection db)
        {
            _dbConnectionModel = DatabaseConnectionModel.Instance(db);
        }

        public new static DatabaseConnection Instance(string connectionStringName) => new DatabaseConnection(connectionStringName);

        public new static DatabaseConnection Instance(DbConnection db) => new DatabaseConnection(db);

        public bool Open()
        {
            if (_dbConnectionModel?.DbConnection.State == ConnectionState.Closed)
            {
                _dbConnectionModel.DbConnection.Open();
            }

            return _dbConnectionModel.DbConnection.State == ConnectionState.Open;
        }

        public bool Close()
        {
            if (_dbConnectionModel?.DbConnection.State == ConnectionState.Open)
            {
                _dbConnectionModel.DbConnection.Close();
            }

            return _dbConnectionModel?.DbConnection.State == ConnectionState.Closed;
        }

        public string GetVariable()
        {
            return _dbConnectionModel.Variable;
        }

        public DbDataAdapter CreateDbDataAdapter()
        {
            DbDataAdapter da;
            try
            {
                da = DbProviderFactories.GetFactory(_dbConnectionModel.DbConnection).CreateDataAdapter();
            }
            catch
            {
                da = DbProviderFactories.GetFactory(_dbConnectionModel.ProviderName).CreateDataAdapter();
            }

            return da;
        }

        public DbConnection GetConnection()
        {
            return _dbConnectionModel.DbConnection;
        }

        public DbCommand CreateCommand()
        {
            return _dbConnectionModel.DbConnection.CreateCommand();
        }

        public DbTransaction BeginTransaction()
        {
            return _dbConnectionModel.DbConnection.BeginTransaction(IsolationLevel.ReadCommitted);
        }

        public override void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }
    }
}