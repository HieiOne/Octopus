using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Octopus.modules.messages
{
    class Messages
    {
        public static System.DateTime dateNow = System.DateTime.Now; //Date & Time at the moment

        public static void WriteError(string value, bool log = true)
        {
            //Console.BackgroundColor = ConsoleColor.Red;
            value = "ERROR: " + value; //Add error in front

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(value.PadRight(Console.WindowWidth - 1)); // <-- see note
                                                                        //
                                                                        // Reset the color.
                                                                        //
            Console.ResetColor();

            if (log)
                Logger(value);
        }

        public static void WriteQuestion(string value, bool log = true)
        {
            //Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(value); // <-- see note
                                      //
                                      // Reset the color.
                                      //
            Console.ResetColor();

            if (log)
                Logger(value);
        }

        public static void WriteSuccess(string value, bool log = true)
        {
            //Console.BackgroundColor = ConsoleColor.Green;
            value = "OK: " + value; //Add OK in front

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(value.PadRight(Console.WindowWidth - 1)); // <-- see note
                                                                        //
                                                                        // Reset the color.
                                                                        //
            Console.ResetColor();

            if (log)
                Logger(value);
        }
        public static void WriteExecuteQuery(string value, bool log = true)
        {
            //Console.BackgroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(value.PadRight(Console.WindowWidth - 1)); // <-- see note
                                                                        //
                                                                        // Reset the color.
                                                                        //
            Console.WriteLine();
            Console.ResetColor();

            if (log)
                Logger(value);
        }

        public static void Logger(string logMessage)
        {
            string format = "-yyyyMMdd-hhmmsstt", formatLines = "hh:mm:ss";
            string fileName = ConfigurationManager.AppSettings["LogPath"] + "log" + dateNow.ToString(format) + ".txt";
            
            try
            {
                File.AppendAllText(fileName, "[" + System.DateTime.Now.ToString(formatLines) + "] " + logMessage + Environment.NewLine /*"\n"*/);
            }
            catch (DirectoryNotFoundException)
            {
                Directory.CreateDirectory(ConfigurationManager.AppSettings["LogPath"]);
                Logger(logMessage); // ;D
            }

        }
    }
}
