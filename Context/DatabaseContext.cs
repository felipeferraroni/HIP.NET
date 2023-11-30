using HoneyInPacifier.Core.Bases;
using HoneyInPacifier.Language.Base;
using System;
using System.Data.Common;

namespace HoneyInPacifier.Context
{
    public class DatabaseContext : DatabaseObject<DatabaseContext>
    {
        ~DatabaseContext() => Dispose();

        public override void Dispose()
        {
            _db?.Dispose();
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        private string _connectStringName { get; set; }

        private DbConnection _db { get; set; }

        public DatabaseContext(string connectStringName)
        {
            _connectStringName = connectStringName;
        }

        public DatabaseContext(DbConnection db)
        {
            _db = db;
        }

        public new static DatabaseContext Instance(string connectStringName) => new DatabaseContext(connectStringName);

        public new static DatabaseContext Instance(DbConnection db) => new DatabaseContext(db);

        public DatabaseSet Dbset()
        {
            DatabaseSet dbSet;

            if (!string.IsNullOrWhiteSpace(_connectStringName))
            {
                dbSet = new DatabaseSet(_connectStringName);
            }
            else
            {
                dbSet = new DatabaseSet(_connectStringName);
            }

            return dbSet;
        }

    }
}