using System;

namespace SharedListTest_Client
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: SharedListTest_Client.exe [Server URL]");
                return;
            }

            using (var list = new SharedList.SharedList(new Uri(args[0])))
            {
                char command = ' ';
                while (command != 'q')
                {
                    Console.WriteLine();
                    Console.WriteLine("List created. Please choose a command:");
                    Console.WriteLine("   a: Add item to list");
                    Console.WriteLine("   e: Enumerate list");
                    Console.WriteLine("   l: Long add test");
                    Console.WriteLine("   q: Quit (closes socket)");
                    command = char.ToLowerInvariant(Console.ReadKey().KeyChar);

                    switch (command)
                    {
                        case 'a':
                            Console.WriteLine();
                            Console.Write("Message: ");
                            var msg = Console.ReadLine();
                            list.Add(msg);
                            break;
                        case 'l':
                            Console.WriteLine();
                            list.Add(new string('a', 2000));
                            break;
                        case 'e':
                            Console.WriteLine();
                            Console.WriteLine("List contents:");
                            foreach (string s in list)
                            {
                                Console.WriteLine($"  {s}");
                            }
                            break;
                        default:
                            Console.WriteLine();
                            break;
                    }
                }
            }
        }
    }
}
