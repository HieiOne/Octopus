using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Octopus.modules.ConfigurationSettings
{
    public class TableConfig : ConfigurationSection
    {
        // Create a property that lets us access the collection
        // of TableInstanceCollection

        // Specify the name of the element used for the property
        [ConfigurationProperty("tables")]
        // Specify the type of elements found in the collection
        [ConfigurationCollection(typeof(TableInstanceCollection))]
        public TableInstanceCollection TableInstances
        {
            get
            {
                // Get the collection and parse it
                return (TableInstanceCollection)this["tables"];
            }
        }

    }

    /// <summary>
    /// Class we use for the configuration table list
    /// </summary>
    class TableElement
    {
        public string Name { get; set; }
        public string FromDatabase { get; set; }
        public string FromServer { get; set; }
        public string ToServer { get; set; }


        /// <summary>
        /// Reads from configuration list the section TableList, converts it to a List<string> and returns it
        /// </summary>
        /// <returns>List<string></returns>
        /// <param name="configuration"></param>
        public static List<TableElement> ConfigurationTableList(Configuration configuration = null)
        {
            dynamic tableConfig;

            if (configuration == null)
            { 
                tableConfig = (TableConfig)ConfigurationManager.GetSection("TableListConfig");
            }
            else
            { 
                tableConfig = (TableConfig)configuration.GetSection("TableListConfig");            
            }

            List<TableElement> tableList = new List<TableElement>();

            // Loop through each instance in the TableInstanceCollection
            foreach (TableInstanceElement instance in tableConfig.TableInstances)
            {
                TableElement tableElement = new TableElement();
                tableElement.Name = instance.Name;
                tableElement.FromDatabase = instance.Database;
                tableElement.FromServer = instance.FromServer;
                tableElement.ToServer = instance.ToServer;
                tableList.Add(tableElement);
            }

            return tableList;
        }
    }
}
