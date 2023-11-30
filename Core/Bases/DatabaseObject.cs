using System;
using System.Runtime.Serialization;

namespace HoneyInPacifier.Core.Bases
{
    public class DatabaseObject
    {
        ~DatabaseObject() => Dispose();

        public static E Instance<E>() where E : class
        {
            return (E)FormatterServices.GetUninitializedObject(typeof(E));
        }

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    public class DatabaseObject<E>
    {
        ~DatabaseObject() => Dispose();

        public static E Instance
        {
            get
            {
                return (E)FormatterServices.GetUninitializedObject(typeof(E));
            }
        }

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}