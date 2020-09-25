using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Specialized;
using System.Diagnostics;
using Octopus.modules.messages;

namespace Octopus
{
    class Octopus
    {
        static void Main(string[] args)
        {
            List<string> tableList;

            string prefix = ConfigurationManager.AppSettings.Get("prefix") + "_";
            string suffix = "_" + ConfigurationManager.AppSettings.Get("suffix");
            
            if (prefix.Length == 1) // Without value
                prefix = null;

            if (suffix.Length == 1) // Without value
                suffix = null;

            Debug.WriteLine("Reading Config file . . .");
            tableList = ConfigurationTableList();

            if (tableList.Count() != 0)
            {
                Debug.WriteLine("Read Config file succesfully");
            }
            else
            {
                Messages.WriteError("No tables specified in App.Config");
            }


        }

        /// <summary>
        /// Reads from configuration list the section TableList, converts it to a List<string> and returns it
        /// </summary>
        /// <returns>List<string></returns>
        static List<string> ConfigurationTableList()
        {
            var applicationSettings = ConfigurationManager.GetSection("TableList") as NameValueCollection;
            List<string> tableList = new List<string>();

            if (applicationSettings.Count == 0)
            {
                Console.WriteLine("Application Settings are not defined");
            }
            else
            {
                foreach (var key in applicationSettings.AllKeys)
                {
                    tableList.Add(applicationSettings[key]);
                }
            }

            return tableList;
        }
    }
}
