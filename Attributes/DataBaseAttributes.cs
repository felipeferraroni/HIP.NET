using System;
using System.Data;

namespace HoneyInPacifier.Attributes
{
    public class DataBaseAttributes : Attribute
    {
        public partial class Field : Attribute
        {
            public string Name { get; set; }
            public bool Pk { get; set; }
            public string Fk { get; set; }
            public int Size { get; set; }
            public bool Virtual { get; set; }
            public ParameterDirection Direction { get; set; }
        }

        public partial class Table : Attribute
        {
            public string Name { get; set; }
            public string Schema { get; set; }
        }

        public partial class Sequence : Attribute
        {
            public bool AutoIncrement { get; set; }

            public string Name { get; set; }
        }
    }
}