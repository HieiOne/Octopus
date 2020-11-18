using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Octopus.modules.messages
{
    static class ProgressBar
    {
        const char _block = '■';
        const string _twirl = "-\\|/";
        public static void WriteProgressBar(int percent, int valueCount, int valueMaxCount, long memoryMB, bool update = false, string name = null)
        {
            if (OctopusConfig.console_verbosity == 0) 
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(new string(' ', Console.BufferWidth));
                Console.SetCursorPosition(0, Console.CursorTop-1);
            }

            if (!(string.IsNullOrEmpty(name)))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("Table: " + name + " ");
                Console.ResetColor();
            }

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write("[");
            var p = (int)((percent / 10f) + .5f);
            for (var i = 0; i < 10; ++i)
            {
                if (i >= p)
                    Console.Write(' ');
                else
                    Console.Write(_block);
            }
            Console.Write("]");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("{0,3:##0}% ", percent);
            Console.ResetColor();
            
            Console.Write($"{valueCount}/{valueMaxCount} - {memoryMB} MB Memory Used");

            if (OctopusConfig.console_verbosity > 0)
                Console.WriteLine();
        }

        public static void WriteProgress(int progress, bool update = false)
        {
            if (update)
                Console.Write("\b");
            Console.Write(_twirl[progress % _twirl.Length]);
        }
    }
}
