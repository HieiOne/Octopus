using System.Collections.Generic;

namespace Octopus.modules.dbModules
{
    /// <summary>
    /// Class for the DbDefinition JSON
    /// </summary>
    public class DbDefinition
    {
        public string name { get; set; }
        public bool fromServer { get; set; }
        public bool toServer { get; set; }
        public string className { get; set; }
        public string connectionString { get; set; }
    }

    /// <summary>
    /// List composed of DbDefinitions used to run a foreach
    /// </summary>
    public class DbDefinitionList
    {
        public List<DbDefinition> dbDefinitions { get; set; }
        
    }
}
