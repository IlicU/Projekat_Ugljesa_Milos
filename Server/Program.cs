using System;
using Telemedicina.Server;

namespace Telemedicina.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                           TELEMEDICINA SERVER                                        ║");
            Console.WriteLine("║                    Sistem za telemedicinske preglede                                 ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            
            TelemedicinaServer server = new TelemedicinaServer();
            
            Console.WriteLine("Pritisnite Ctrl+C za zaustavljanje servera");
            Console.WriteLine();
            
            try
            {
                server.Start();
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
