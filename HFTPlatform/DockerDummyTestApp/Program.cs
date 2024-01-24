using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DockerDummyTestApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hola Docker!");
            string input = Console.ReadLine();
            Console.WriteLine($"You entered: {input}");
            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
        }
    }
}
