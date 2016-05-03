using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SharedListTest_Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: SharedListTest_Server.exe [Port]");
                return;
            }

            using (var server = new SharedList.SharedListServer(int.Parse(args[0])))
            {
                Console.WriteLine($"Server running, listening on port {args[0]}");
                Console.WriteLine("Press enter to exit");
                Console.ReadLine();
            }
        }
    }
}
