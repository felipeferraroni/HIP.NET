using System.Collections.Generic;

namespace HoneyInPacifier.Command.Model
{
    public class DatabaseCommandModel
    {
        public string Query { get; set; }

        public IEnumerable<DatabaseCommandParameter> Parameters { get; set; }

        public bool ReturnId { get; set; }
    }
}