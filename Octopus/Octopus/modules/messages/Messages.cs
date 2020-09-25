using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Octopus.modules.messages
{
    class Messages
    {
        public static void WriteError(string value)
        {
            //Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(value.PadRight(Console.WindowWidth - 1)); // <-- see note
                                                                        //
                                                                        // Reset the color.
                                                                        //
            Console.ResetColor();
        }

        public static void WriteQuestion(string value)
        {
            //Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(value); // <-- see note
                                      //
                                      // Reset the color.
                                      //
            Console.ResetColor();
        }

        public static void WriteSuccess(string value)
        {
            //Console.BackgroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(value.PadRight(Console.WindowWidth - 1)); // <-- see note
                                                                        //
                                                                        // Reset the color.
                                                                        //
            Console.ResetColor();
        }
        public static void WriteExecuteQuery(string value)
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
        }
    }
}
