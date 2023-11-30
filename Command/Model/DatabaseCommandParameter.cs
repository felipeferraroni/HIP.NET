using HoneyInPacifier.Core.Bases;
using System;
using System.Data;

namespace HoneyInPacifier.Command.Model
{
    public class DatabaseCommandParameter : DatabaseObject<DatabaseCommandParameter>
    {
        ~DatabaseCommandParameter() => Dispose();

        public string Name { get; set; }
        public object Value { get; set; }
        public DbType Type { get; set; }
        public int Size { get; set; }
        public ParameterDirection Direction { get; set; }

        public ParameterDirection SetDirection()
        {
            if (Direction == 0)
            {
                Direction = ParameterDirection.Input;
            }

            return Direction;
        }

        public DbType SetDbType()
        {
            DbType dbType;

            if (Type == 0)
            {
                if (Value is int)
                {
                    dbType = DbType.Int32;
                }
                else if (Value is long)
                {
                    dbType = DbType.Int64;
                }
                else if (Value is double)
                {
                    dbType = DbType.Double;
                }
                else if (Value is decimal)
                {
                    dbType = DbType.Decimal;
                }
                else if (Value is Guid)
                {
                    dbType = DbType.Guid;
                }
                else if (Value is DateTime)
                {
                    dbType = DbType.DateTime;
                }
                else if (Value is TimeSpan)
                {
                    dbType = DbType.DateTime;
                }
                else // if ( Value is String )
                {
                    dbType = DbType.String;
                }
            }
            else
            {
                dbType = Type;
            }

            return dbType;
        }

        public int SetDbSize()
        {
            if (Size == 0)
                return int.MaxValue;

            return Size;
        }

        public object SetDbValue()
        {
            if (Value == null)
                return DBNull.Value;
            return Value;
        }
    }
}