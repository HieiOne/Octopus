using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace Octopus.modules.dbModules
{
    /// <summary>
    /// List composed of DbDefinitions used to run a foreach
    /// </summary>
    public class DbDefinitionList
    {
        public List<DbDefinition> dbDefinitions { get; set; }
        
        
        /// <summary>
        /// Reads JSON and returns a DbDefinitionList
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static DbDefinitionList JSONDbDefinitions(string file)
        {
            return JsonConvert.DeserializeObject<DbDefinitionList>(File.ReadAllText(file));
        }
    }

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
}
