using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Octopus.modules.ConfigurationSettings
{
    public class TableInstanceElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsKey = true, IsRequired = true)]
        public string Name
        {
            get
            {
                // Return the value of the 'name' attribute as a string
                return (string)base["name"];
            }
            set
            {
                // Set the value of the 'name' attribute
                base["name"] = value;
            }
        }

        [ConfigurationProperty("fromServer")]
        public string FromServer
        {
            get
            {
                // Return the value of the 'server' attribute as a string
                string server = (string)base["fromServer"];

                if (string.IsNullOrEmpty(server)) //In case from server is empty, we sent default value
                    return ConfigurationManager.AppSettings.Get("fromServer");

                return server;
            }
            set
            {
                // Set the value of the 'server' attribute
                base["server"] = value;
            }
        }

        [ConfigurationProperty("toServer")]
        public string ToServer
        {
            get
            {
                // Return the value of the 'server' attribute as a string
                string server = (string)base["toServer"];

                if (string.IsNullOrEmpty(server)) //In case from server is empty, we sent default value
                    return ConfigurationManager.AppSettings.Get("toServer");

                return server;
            }
            set
            {
                // Set the value of the 'server' attribute
                base["toServer"] = value;
            }
        }

        [ConfigurationProperty("fromdatabase")]
        public string Database
        {
            get
            {
                // Return the value of the 'database' attribute as a string
                string fromdatabase = (string)base["fromdatabase"];

                if(string.IsNullOrEmpty(fromdatabase)) //In case from database is empty, we sent default value
                    return ConfigurationManager.AppSettings.Get("fromDB");

                return fromdatabase;
            }
            set
            {
                // Set the value of the 'server' attribute
                base["fromdatabase"] = value;
            }
        }
    }
}
