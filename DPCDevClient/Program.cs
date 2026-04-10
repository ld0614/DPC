using DPCLibrary.Utils;
using System;
using System.Collections.Generic;

namespace DPCDevClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IList<string> classNames = AccessWMI.GetWMIClassNames();
            foreach (string className in classNames)
            {
                Console.WriteLine(className);
            }

            string value;
            do
            {
                Console.WriteLine("Startup Complete, Type 'quit' to Shutdown...");
                value = Console.ReadLine();
            } while (value.ToUpperInvariant() != "QUIT");
        }
    }
}
