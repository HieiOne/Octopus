using Octopus.modules.Protection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Octopus.modules.ConfigurationSettings
{
    class ConfigurationFile : SectionProtection
    {
        public Configuration configuration { get; set; }

        public ConfigurationFile(string path = null)
        {
            if (path != null) // We load the specified config file
            {
                var map = new ExeConfigurationFileMap
                {
                    ExeConfigFilename = path,
                    LocalUserConfigFilename = path,
                    RoamingUserConfigFilename = path
                };

                configuration = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
            }
            else //We load the default config file
            {
                configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            }
        }

        public void EncryptConnectionString()
        {
            EncryptSection(configuration, "connectionStrings");
        }

        public void DecryptConnectionString(bool save = false)
        {
            DecryptSection(configuration, "connectionStrings", save);
        }
    }
}
