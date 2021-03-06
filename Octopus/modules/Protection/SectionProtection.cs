﻿using Octopus.modules.messages;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Octopus.modules.Protection
{
    class SectionProtection
    {
        //TODO Allow user to add a connection String easily after its protected
        
        protected void EncryptSection(Configuration config, string section)
        {
            ConfigurationSection configurationSection = config.GetSection(section);
            if (configurationSection != null)
            {
                if (!configurationSection.SectionInformation.IsProtected)
                {
                    configurationSection.SectionInformation.ProtectSection("DataProtectionConfigurationProvider");
                    config.Save();
                    Messages.WriteSuccess($"Configuration section {section} succesfully protected");
                }
                else
                {
                    Messages.WriteQuestion($"Configuration section {section} was already protected");
                }
            }
        }

        protected void DecryptSection(Configuration config, string section, bool save)
        {
            ConfigurationSection configurationSection = config.GetSection(section);
            if (configurationSection != null)
            {
                if (configurationSection.SectionInformation.IsProtected)
                {
                    configurationSection.SectionInformation.UnprotectSection();
                    if (save)
                    { 
                        config.Save();
                        Messages.WriteWarning($"Configuration section {section} succesfully unprotected");
                    }
                }
                else
                {
                    Messages.WriteWarning("You should encrypt your connection strings with the -p parameter");
                }
            }
        }
    }
}
