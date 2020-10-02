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
}
