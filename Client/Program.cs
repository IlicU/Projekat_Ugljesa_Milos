using System;
using Telemedicina.Client;

namespace Telemedicina.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                           TELEMEDICINA CLIENT                                        ║");
            Console.WriteLine("║                    Sistem za telemedicinske preglede                                 ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            
            TelemedicinaClient client = new TelemedicinaClient();
            
            try
            {
                client.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška: {ex.Message}");
            }
            
            Console.WriteLine("Pritisnite Enter za izlazak...");
            Console.ReadLine();
        }
    }
}
