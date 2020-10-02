using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Octopus.modules.ConfigurationSettings
{
    public class TableInstanceCollection : ConfigurationElementCollection
    {
        public TableInstanceElement this[int index]
        {
            get
            {
                // Gets the TableInstanceElement at the specified
                // index in the collection
                return (TableInstanceElement)BaseGet(index);
            }
            set
            {
                // Check if a TableInstanceElement exists at the
                // specified index and delete it if it does
                if (BaseGet(index) != null)
                    BaseRemoveAt(index);

                // Add the new TableInstanceElement at the specified
                // index
                BaseAdd(index, value);
            }
        }

        // Create a property that lets us access an element in the
        // colleciton with the name of the element
        public new TableInstanceElement this[string key]
        {
            get
            {
                // Gets the TableInstanceElement where the name
                // matches the string key specified
                return (TableInstanceElement)BaseGet(key);
            }
            set
            {
                // Checks if a TableInstanceElement exists with
                // the specified name and deletes it if it does
                if (BaseGet(key) != null)
                    BaseRemoveAt(BaseIndexOf(BaseGet(key)));

                // Adds the new SageCRMInstanceElement
                BaseAdd(value);
            }
        }

        // Method that must be overriden to create a new element
        // that can be stored in the collection
        protected override ConfigurationElement CreateNewElement()
        {
            return new TableInstanceElement();
        }

        // Method that must be overriden to get the key of a
        // specified element
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((TableInstanceElement)element).Name;
        }
    }
}
