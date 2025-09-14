using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Telemedicina.Shared.Models;
using Telemedicina.Shared.Utils;

namespace Telemedicina.Client
{
    public class TelemedicinaClient
    {
        private const string SERVER_IP = "127.0.0.1";
        private const int SERVER_PORT = 8888;
        
        private Socket clientSocket;
        private bool isConnected;
        private string clientType;

        public void Start()
        {
            try
            {
                Console.WriteLine("=== TELEMEDICINA CLIENT ===");
                Console.WriteLine("Povezivanje sa serverom...");
                
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(SERVER_IP), SERVER_PORT);
                clientSocket.Connect(serverEndPoint);
                
                isConnected = true;
                Console.WriteLine("Uspešno povezan sa serverom!");
                Console.WriteLine();
                
                // Pokretanje thread-a za prijem poruka
                Thread receiveThread = new Thread(ReceiveMessages);
                receiveThread.Start();
                
                // Glavni meni
                ShowMainMenu();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri povezivanju: {ex.Message}");
                Console.WriteLine("Proverite da li je server pokrenut!");
            }
        }

        private void ShowMainMenu()
        {
            while (isConnected)
            {
                Console.WriteLine("\n=== GLAVNI MENI - TELEMEDICINA ===");
                Console.WriteLine("1. Pacijent - Registracija i zahtevi za usluge");
                Console.WriteLine("2. Urgentna jedinica");
                Console.WriteLine("3. Dijagnostička jedinica");
                Console.WriteLine("4. Terapeutska jedinica");
                Console.WriteLine("5. Lekar specijalista");
                Console.WriteLine("6. Izlaz");
                Console.WriteLine();
                Console.Write("Vaš izbor: ");
                
                string choice = Console.ReadLine();
                
                switch (choice)
                {
                    case "1":
                        clientType = "Pacijent";
                        PacijentMenu();
                        break;
                    case "2":
                        clientType = "UrgentnaJedinica";
                        JedinicaMenu("Urgentna jedinica");
                        break;
                    case "3":
                        clientType = "DijagnostickaJedinica";
                        JedinicaMenu("Dijagnostička jedinica");
                        break;
                    case "4":
                        clientType = "TerapeutskaJedinica";
                        JedinicaMenu("Terapeutska jedinica");
                        break;
                    case "5":
                        clientType = "LekarSpecijalista";
                        LekarMenu();
                        break;
                    case "6":
                        Disconnect();
                        break;
                    default:
                        Console.WriteLine("Nevažeća opcija!");
                        break;
                }
            }
        }

        private void PacijentMenu()
        {
            while (isConnected)
            {
                Console.WriteLine($"\n=== PACIJENT MENI ===");
                Console.WriteLine("1. Registracija pacijenta");
                Console.WriteLine("2. Zahtev za uslugom");
                Console.WriteLine("3. Povratak na glavni meni");
                Console.Write("Vaš izbor: ");
                
                string choice = Console.ReadLine();
                
                switch (choice)
                {
                    case "1":
                        RegisterPatient();
                        break;
                    case "2":
                        RequestService();
                        break;
                    case "3":
                        return;
                    default:
                        Console.WriteLine("Nevažeća opcija!");
                        break;
                }
            }
        }

        private void RegisterPatient()
        {
            try
            {
                Console.WriteLine("\n===  REGISTRACIJA PACIJENTA ===");
                
                Pacijent pacijent = new Pacijent();
                
                Console.Write("Unesite LBO: ");
                pacijent.LBO = Console.ReadLine();
                
                Console.Write("Unesite ime: ");
                pacijent.Ime = Console.ReadLine();
                
                Console.Write("Unesite prezime: ");
                pacijent.Prezime = Console.ReadLine();
                
                Console.Write("Unesite adresu: ");
                pacijent.Adresa = Console.ReadLine();
                
                NetworkMessage message = new NetworkMessage(MessageType.RegistracijaPacijenta, 
                    "Registracija novog pacijenta", pacijent);
                NetworkHelper.SendMessage(message, clientSocket);
                
                Console.WriteLine(" Registracija poslata serveru...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Greška pri registraciji: {ex.Message}");
            }
        }

        private void RequestService()
        {
            try
            {
                Console.WriteLine("\n===  ZAHTEV ZA USLUGOM ===");
                
                // Unos podataka o pacijentu
                Pacijent pacijent = new Pacijent();
                Console.Write("Unesite LBO pacijenta: ");
                pacijent.LBO = Console.ReadLine();
                Console.Write("Unesite ime: ");
                pacijent.Ime = Console.ReadLine();
                Console.Write("Unesite prezime: ");
                pacijent.Prezime = Console.ReadLine();
                Console.Write("Unesite adresu: ");
                pacijent.Adresa = Console.ReadLine();
                
                // Izbor usluge
                Console.WriteLine("\n Izaberite tip usluge:");
                Console.WriteLine("1.  Terapija (Normalan prioritet)");
                Console.WriteLine("2.  Pregled (Visok prioritet)");
                Console.WriteLine("3.  Urgentna pomoć (Kritičan prioritet)");
                Console.Write("Vaš izbor: ");
                
                string serviceChoice = Console.ReadLine();
                TipUsluge tipUsluge = TipUsluge.Terapija;
                
                switch (serviceChoice)
                {
                    case "1":
                        tipUsluge = TipUsluge.Terapija;
                        pacijent.Prioritet = Prioritet.Normalan;
                        break;
                    case "2":
                        tipUsluge = TipUsluge.Pregled;
                        pacijent.Prioritet = Prioritet.Visok;
                        break;
                    case "3":
                        tipUsluge = TipUsluge.UrgentnaPomoc;
                        pacijent.Prioritet = Prioritet.Kritican;
                        break;
                    default:
                        Console.WriteLine(" Nevažeća opcija, biram terapiju.");
                        break;
                }
                
                pacijent.TipUsluge = tipUsluge;
                
                Console.Write("Unesite opis zahteva: ");
                string opis = Console.ReadLine();
                
                MedicinskiZahtev zahtev = new MedicinskiZahtev
                {
                    Pacijent = pacijent,
                    TipUsluge = tipUsluge,
                    Opis = opis
                };
                
                NetworkMessage message = new NetworkMessage(MessageType.ZahtevZaUslugom, 
                    "Zahtev za medicinskom uslugom", zahtev);
                NetworkHelper.SendMessage(message, clientSocket);
                
                Console.WriteLine($" Zahtev za {tipUsluge} (Prioritet: {pacijent.Prioritet}) poslat serveru...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Greška pri slanju zahteva: {ex.Message}");
            }
        }

        private void JedinicaMenu(string jedinicaNaziv)
        {
            // Registracija jedinice
            string jedinicaId = $"{clientType}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            NetworkMessage registrationMessage = new NetworkMessage(MessageType.RegistracijaJedinice, 
                $"Registracija {jedinicaNaziv}", jedinicaId);
            NetworkHelper.SendMessage(registrationMessage, clientSocket);
            
            Console.WriteLine($"\n=== {jedinicaNaziv.ToUpper()} ===");
            Console.WriteLine($" ID Jedinice: {jedinicaId}");
            Console.WriteLine(" Registracija poslata serveru...");
            
            while (isConnected)
            {
                Console.WriteLine($"\n=== {jedinicaNaziv.ToUpper()} MENI ===");
                Console.WriteLine("1.  Prikaži status");
                Console.WriteLine("2.  Povratak na glavni meni");
                Console.Write("Vaš izbor: ");
                
                string choice = Console.ReadLine();
                
                switch (choice)
                {
                    case "1":
                        Console.WriteLine($" {jedinicaNaziv} je aktivna i čeka zahteve od servera...");
                        Console.WriteLine(" Poruke se automatski primaju putem Socket.Select() multipleksiranja");
                        break;
                    case "2":
                        return;
                    default:
                        Console.WriteLine("Nevažeća opcija!");
                        break;
                }
            }
        }

        private void LekarMenu()
        {
            while (isConnected)
            {
                Console.WriteLine($"\n===  LEKAR SPECIJALISTA MENI ===");
                Console.WriteLine("1.  Zatraži listu pacijenata");
                Console.WriteLine("2.  Ažuriraj status pacijenta");
                Console.WriteLine("3.  Povratak na glavni meni");
                Console.Write("Vaš izbor: ");
                
                string choice = Console.ReadLine();
                
                switch (choice)
                {
                    case "1":
                        RequestPatientList();
                        break;
                    case "2":
                        UpdatePatientStatus();
                        break;
                    case "3":
                        return;
                    default:
                        Console.WriteLine("Nevažeća opcija!");
                        break;
                }
            }
        }

        private void RequestPatientList()
        {
            try
            {
                Console.WriteLine("\n===  ZAHTEV ZA LISTOM PACIJENATA ===");
                
                NetworkMessage message = new NetworkMessage(MessageType.ZahtevZaListomPacijenata, 
                    $"Lekar {clientType} traži listu pacijenata");
                NetworkHelper.SendMessage(message, clientSocket);
                
                Console.WriteLine(" Zahtev za listom pacijenata poslat serveru...");
                Console.WriteLine(" Čekam odgovor...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Greška: {ex.Message}");
            }
        }
        
        private void UpdatePatientStatus()
        {
            try
            {
                Console.WriteLine("\n===  AŽURIRANJE STATUSA PACIJENTA ===");
                
                Console.Write("Unesite LBO pacijenta: ");
                string lbo = Console.ReadLine();
                
                Console.WriteLine("\n Izaberite novi status:");
                Console.WriteLine("1. Cekanje");
                Console.WriteLine("2. UObradi");
                Console.WriteLine("3. PregledObavljen");
                Console.WriteLine("4. TerapijaObavljena");
                Console.WriteLine("5. UrgentnaIntervencija");
                Console.WriteLine("6. Zavrseno");
                Console.WriteLine("7. Otkazano");
                Console.Write("Vaš izbor: ");
                
                string statusChoice = Console.ReadLine();
                PacijentStatus newStatus = PacijentStatus.Cekanje;
                
                switch (statusChoice)
                {
                    case "1": newStatus = PacijentStatus.Cekanje; break;
                    case "2": newStatus = PacijentStatus.UObradi; break;
                    case "3": newStatus = PacijentStatus.PregledObavljen; break;
                    case "4": newStatus = PacijentStatus.TerapijaObavljena; break;
                    case "5": newStatus = PacijentStatus.UrgentnaIntervencija; break;
                    case "6": newStatus = PacijentStatus.Zavrseno; break;
                    case "7": newStatus = PacijentStatus.Otkazano; break;
                    default:
                        Console.WriteLine("Nevažeća opcija!");
                        return;
                }
                
                Console.WriteLine("\n Izaberite prioritet:");
                Console.WriteLine("1. Nizak");
                Console.WriteLine("2. Normalan");
                Console.WriteLine("3. Visok");
                Console.WriteLine("4. Kritican");
                Console.Write("Vaš izbor: ");
                
                string priorityChoice = Console.ReadLine();
                Prioritet newPriority = Prioritet.Normalan;
                
                switch (priorityChoice)
                {
                    case "1": newPriority = Prioritet.Nizak; break;
                    case "2": newPriority = Prioritet.Normalan; break;
                    case "3": newPriority = Prioritet.Visok; break;
                    case "4": newPriority = Prioritet.Kritican; break;
                    default:
                        Console.WriteLine("Nevažeća opcija!");
                        return;
                }
                
                // Kreiraj ažuriranog pacijenta
                Pacijent updatedPatient = new Pacijent
                {
                    LBO = lbo,
                    Status = newStatus,
                    Prioritet = newPriority
                };
                
                NetworkMessage message = new NetworkMessage(MessageType.AzuriranjeStatusaPacijenta, 
                    $"Ažuriranje statusa pacijenta {lbo}", updatedPatient);
                NetworkHelper.SendMessage(message, clientSocket);
                
                Console.WriteLine($" Zahtev za ažuriranje statusa pacijenta {lbo} poslat serveru...");
                Console.WriteLine($" Novi status: {newStatus}, Prioritet: {newPriority}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Greška: {ex.Message}");
            }
        }

        private void ReceiveMessages()
        {
            try
            {
                while (isConnected)
                {
                    if (clientSocket.Poll(1000, SelectMode.SelectRead))
                    {
                        byte[] buffer = new byte[4096];
                        int bytesReceived = clientSocket.Receive(buffer);
                        
                        if (bytesReceived > 0)
                        {
                            NetworkMessage message = NetworkHelper.DeserializeMessage(buffer, bytesReceived);
                            HandleReceivedMessage(message);
                        }
                        else
                        {
                            Console.WriteLine(" Server je prekinuo konekciju.");
                            isConnected = false;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (isConnected)
                    Console.WriteLine($" Greška pri primanju poruke: {ex.Message}");
            }
        }

        private void HandleReceivedMessage(NetworkMessage message)
        {
            Console.WriteLine($"\n === PORUKA OD SERVERA ===");
            Console.WriteLine($" Tip: {message.Tip}");
            Console.WriteLine($" Sadržaj: {message.Sadrzaj}");
            
            switch (message.Tip)
            {
                case MessageType.Potvrda:
                    Console.WriteLine(" Potvrda primljena od servera");
                    break;
                case MessageType.PotvrdaAzuriranja:
                    Console.WriteLine(" Status pacijenta uspešno ažuriran!");
                    break;
                case MessageType.ListaPacijenata:
                    HandlePatientList(message);
                    break;
                case MessageType.ZahtevZaObradu:
                    HandleProcessingRequest(message);
                    break;
                case MessageType.Greska:
                    Console.WriteLine($" Greška: {message.Sadrzaj}");
                    break;
                case MessageType.KrajKomunikacije:
                    Console.WriteLine(" Server se gasi...");
                    isConnected = false;
                    break;
                default:
                    Console.WriteLine($" Nepoznat tip poruke: {message.Tip}");
                    break;
            }
            Console.WriteLine("============================");
        }

        private void HandlePatientList(NetworkMessage message)
        {
            if (message.Podaci is System.Collections.Generic.List<Pacijent> pacijenti)
            {
                Console.WriteLine($"\n === LISTA PACIJENATA (SORTIRANA PO PRIORITETU) ===");
                Console.WriteLine($" Ukupno pacijenata: {pacijenti.Count}");
                Console.WriteLine();
                
                if (pacijenti.Count == 0)
                {
                    Console.WriteLine(" Nema registrovanih pacijenata.");
                }
                else
                {
                    foreach (var pacijent in pacijenti)
                    {
                        string priorityIcon = GetPriorityIcon(pacijent.Prioritet);
                        string serviceIcon = GetServiceIcon(pacijent.TipUsluge);
                        
                        Console.WriteLine($"{priorityIcon} {serviceIcon} {pacijent.Ime} {pacijent.Prezime} (LBO: {pacijent.LBO})");
                        Console.WriteLine($"     Adresa: {pacijent.Adresa}");
                        Console.WriteLine($"     Status: {pacijent.Status}");
                        Console.WriteLine($"     Prioritet: {pacijent.Prioritet}");
                        Console.WriteLine($"     Usluga: {pacijent.TipUsluge}");
                        Console.WriteLine($"     Registrovan: {pacijent.VremeRegistracije:dd.MM.yyyy HH:mm}");
                        Console.WriteLine();
                    }
                }
                Console.WriteLine("=================================================");
            }
        }

        private string GetPriorityIcon(Prioritet prioritet)
        {
            switch (prioritet)
            {
                case Prioritet.Kritican:
                    return "[KRITIČAN]";
                case Prioritet.Visok:
                    return "[VISOK]";
                case Prioritet.Normalan:
                    return "[NORMALAN]";
                case Prioritet.Nizak:
                    return "[NIZAK]";
                default:
                    return "[NEPOZNATO]";
            }
        }

        private string GetServiceIcon(TipUsluge tipUsluge)
        {
            switch (tipUsluge)
            {
                case TipUsluge.UrgentnaPomoc:
                    return "[URGENTNO]";
                case TipUsluge.Pregled:
                    return "[PREGLED]";
                case TipUsluge.Terapija:
                    return "[TERAPIJA]";
                default:
                    return "[NEPOZNATO]";
            }
        }

        private void Disconnect()
        {
            try
            {
                NetworkMessage message = new NetworkMessage(MessageType.KrajKomunikacije, 
                    $"Klijent {clientType} se odjavljuje");
                NetworkHelper.SendMessage(message, clientSocket);
                
                isConnected = false;
                clientSocket.Close();
                Console.WriteLine(" Odjavljen sa servera.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Greška pri odjavljivanju: {ex.Message}");
            }
        }
        
        private void HandleProcessingRequest(NetworkMessage message)
        {
            if (message.Podaci is MedicinskiZahtev zahtev)
            {
                Console.WriteLine($"\n === NOVI ZAHTEV ZA OBRADU ===");
                Console.WriteLine($" Pacijent: {zahtev.Pacijent.Ime} {zahtev.Pacijent.Prezime}");
                Console.WriteLine($" LBO: {zahtev.Pacijent.LBO}");
                Console.WriteLine($" Tip usluge: {zahtev.TipUsluge}");
                Console.WriteLine($" Prioritet: {zahtev.Pacijent.Prioritet}");
                Console.WriteLine($" Opis: {zahtev.Opis}");
                
                // Simulacija obrade
                SimulateProcessing(zahtev);
            }
        }
        
        private void SimulateProcessing(MedicinskiZahtev zahtev)
        {
            Console.WriteLine($"\n Simulacija obrade zahteva...");
            
            // Simulacija vremena obrade na osnovu tipa usluge
            int processingTime = 0;
            string processingAction = "";
            
            switch (zahtev.TipUsluge)
            {
                case TipUsluge.UrgentnaPomoc:
                    processingTime = 3;
                    processingAction = "Hitna intervencija";
                    break;
                case TipUsluge.Pregled:
                    processingTime = 5;
                    processingAction = "Dijagnostički pregled";
                    break;
                case TipUsluge.Terapija:
                    processingTime = 4;
                    processingAction = "Terapeutska procedura";
                    break;
            }
            
            Console.WriteLine($" {processingAction} u toku...");
            
            // Simulacija obrade
            for (int i = 0; i < processingTime; i++)
            {
                Thread.Sleep(1000);
                Console.WriteLine($"  Obrada... {i + 1}/{processingTime}");
            }
            
            Console.WriteLine($" Obrada završena! Šaljem rezultat serveru...");
            
            // Pošalji potvrdu serveru
            NetworkMessage completionMessage = new NetworkMessage(MessageType.ZavrsenaObrada, 
                $"Obrada završena za {zahtev.Pacijent.Ime} {zahtev.Pacijent.Prezime}", zahtev);
            NetworkHelper.SendMessage(completionMessage, clientSocket);
        }
    }
}
