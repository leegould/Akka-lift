using System;

namespace Akka_lift
{
    public static class ConsoleExtensions
    {
        public static void WriteLineColor(ConsoleColor color, string format)
        {
            var startcolor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(format);
            Console.ForegroundColor = startcolor;
        }
    }
}
