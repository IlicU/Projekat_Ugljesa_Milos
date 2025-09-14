using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Telemedicina.Shared.Models;
using Telemedicina.Shared.Utils;

namespace Telemedicina.Server
{
    public class TelemedicinaServer
    {
        private const int SERVER_PORT = 8888;
        private const int MAX_CLIENTS = 100; // Neograničen broj klijenata
        
        private Socket serverSocket;
        private Socket[] clientSockets;
        private int lastAssignedIndex;
        private bool isRunning;
        private DateTime lastStatusDisplay;
        
        // TelemedHub funkcionalnosti
        private List<Pacijent> pacijenti;
        private List<MedicinskiZahtev> zahtevi;
        private Dictionary<Socket, string> jedinice; // Socket -> JedinicaId mapping

        public TelemedicinaServer()
        {
            clientSockets = new Socket[MAX_CLIENTS];
            lastAssignedIndex = 0;
            pacijenti = new List<Pacijent>();
            zahtevi = new List<MedicinskiZahtev>();
            jedinice = new Dictionary<Socket, string>();
        }

        public void Start()
        {
            try
            {
                // Kreiranje serverske utičnice
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, SERVER_PORT);
                
                // Ne-blokirajući režim
                serverSocket.Blocking = false;
                
                serverSocket.Bind(localEndPoint);
                serverSocket.Listen(MAX_CLIENTS);
                
                // Polling model - ne treba checkRead lista
                
                isRunning = true;
                Console.WriteLine($"=== TELEMEDICINA SERVER ===");
                Console.WriteLine($"Server pokrenut na portu {SERVER_PORT}");
                Console.WriteLine("Čeka konekcije...");
                Console.WriteLine();
                
                // Glavna petlja servera
                MainLoop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri pokretanju servera: {ex.Message}");
            }
        }

        private void MainLoop()
        {
            while (isRunning)
            {
                try
                {
                    // Polling model - proveri serversku utičnicu za nove konekcije
                    if (serverSocket.Poll(1000, SelectMode.SelectRead))
                    {
                        AcceptNewConnection();
                    }
                    
                    // Polling model - proveri sve klijentske utičnice za poruke
                    int eventCount = 0;
                    for (int i = 0; i < MAX_CLIENTS; i++)
                    {
                        if (clientSockets[i] != null && clientSockets[i].Connected)
                        {
                            if (clientSockets[i].Poll(1000, SelectMode.SelectRead))
                            {
                                HandleClientMessage(clientSockets[i]);
                                eventCount++;
                            }
                        }
                    }
                    
                    // Ispis broja događaja
                    if (eventCount > 0)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Broj događaja: {eventCount}");
                    }
                    
                    // Prikaz statistika
                    DisplayStatus();
                }
                catch (Exception ex)
                {
                    // Greška - zaustavi server
                    Console.WriteLine($"Greška u glavnoj petlji: {ex.Message}");
                    Stop();
                    break;
                }
            }
        }

        private void AcceptNewConnection()
        {
            try
            {
                // Prihvati konekciju
                Socket clientSocket = serverSocket.Accept();
                
                // Ispis adrese i porta klijenta
                IPEndPoint clientEndPoint = clientSocket.RemoteEndPoint as IPEndPoint;
                Console.WriteLine($"[NOVA KONEKCIJA] {clientEndPoint.Address}:{clientEndPoint.Port}");
                
                // Ne-blokirajući režim za klijentsku utičnicu
                clientSocket.Blocking = false;
                
                // Smeštanje u niz utičnica
                bool assigned = false;
                for (int i = 0; i < MAX_CLIENTS; i++)
                {
                    if (clientSockets[i] == null)
                    {
                        clientSockets[i] = clientSocket;
                        lastAssignedIndex = i;
                        assigned = true;
                        
                        // Polling model - ne treba checkRead lista
                        
                        Console.WriteLine($"[KLIJENT DODELJEN] Utičnica {i} dodeljena klijentu {clientEndPoint.Address}:{clientEndPoint.Port}");
                        break;
                    }
                }
                
                if (!assigned)
                {
                    Console.WriteLine("[GREŠKA] Nema slobodnih utičnica!");
                    clientSocket.Close();
                }
                
                // Polling model - ograničenje konekcija se postavlja u MainLoop
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri prihvatanju konekcije: {ex.Message}");
            }
        }

        private void HandleClientMessage(Socket clientSocket)
        {
            try
            {
                byte[] buffer = new byte[4096];
                int bytesReceived = clientSocket.Receive(buffer);
                
                if (bytesReceived > 0)
                {
                    // Deserijalizuj poruku
                    NetworkMessage message = NetworkHelper.DeserializeMessage(buffer, bytesReceived);
                    
                    // Prikaz poruke
                    IPEndPoint clientEndPoint = clientSocket.RemoteEndPoint as IPEndPoint;
                    Console.WriteLine($"[PORUKA PRIMLJENA] Od {clientEndPoint.Address}:{clientEndPoint.Port}");
                    Console.WriteLine($"Tip: {message.Tip}");
                    Console.WriteLine($"Sadržaj: {message.Sadrzaj}");
                    
                    // TelemedHub logika obrade poruka
                    ProcessTelemedHubMessage(message, clientSocket);
                }
                else
                {
                    // Klijent se odjavio
                    Console.WriteLine("[KLIJENT SE ODJAVIO]");
                    RemoveClient(clientSocket);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri obradi poruke: {ex.Message}");
                // Greška - ukloni klijenta
                RemoveClient(clientSocket);
            }
        }

        private void ProcessTelemedHubMessage(NetworkMessage message, Socket clientSocket)
        {
            switch (message.Tip)
            {
                case MessageType.RegistracijaPacijenta:
                    HandlePacijentRegistration(message, clientSocket);
                    break;
                case MessageType.ZahtevZaUslugom:
                    HandleServiceRequest(message, clientSocket);
                    break;
                case MessageType.RegistracijaJedinice:
                    HandleUnitRegistration(message, clientSocket);
                    break;
                case MessageType.ZahtevZaListomPacijenata:
                    HandlePatientListRequest(message, clientSocket);
                    break;
                case MessageType.AzuriranjeStatusaPacijenta:
                    HandlePatientStatusUpdate(message, clientSocket);
                    break;
                case MessageType.ZavrsenaObrada:
                    HandleUnitCompletion(message, clientSocket);
                    break;
                case MessageType.KrajKomunikacije:
                    RemoveClient(clientSocket);
                    break;
                default:
                    Console.WriteLine($"Nepoznat tip poruke: {message.Tip}");
                    break;
            }
        }

        private void HandlePacijentRegistration(NetworkMessage message, Socket clientSocket)
        {
            if (message.Podaci is Pacijent pacijent)
            {
                pacijenti.Add(pacijent);
                Console.WriteLine($"[PACIJENT REGISTROVAN] {pacijent.Ime} {pacijent.Prezime} (LBO: {pacijent.LBO})");
                
                NetworkMessage response = new NetworkMessage(MessageType.Potvrda, "Pacijent uspešno registrovan");
                NetworkHelper.SendMessage(response, clientSocket);
            }
        }

        private void HandleServiceRequest(NetworkMessage message, Socket clientSocket)
        {
            if (message.Podaci is MedicinskiZahtev zahtev)
            {
                zahtevi.Add(zahtev);
                
                // Dodaj pacijenta u bazu podataka (ako već nije dodat)
                var existingPatient = pacijenti.FirstOrDefault(p => p.LBO == zahtev.Pacijent.LBO);
                if (existingPatient == null)
                {
                    pacijenti.Add(zahtev.Pacijent);
                    Console.WriteLine($"[PACIJENT DODAT U BAZU] {zahtev.Pacijent.Ime} {zahtev.Pacijent.Prezime} (LBO: {zahtev.Pacijent.LBO})");
                }
                
                // Ažuriraj prioritet pacijenta
                UpdatePatientPriority(zahtev.Pacijent, zahtev.TipUsluge);
                
                Console.WriteLine($"[NOVI ZAHTEV] {zahtev.Pacijent.Ime} {zahtev.Pacijent.Prezime} - {zahtev.TipUsluge} (Prioritet: {zahtev.Pacijent.Prioritet})");
                
                // Automatsko prosleđivanje zahteva odgovarajućoj jedinici
                ForwardRequestToUnit(zahtev);
                
                NetworkMessage response = new NetworkMessage(MessageType.Potvrda, "Zahtev uspešno primljen i prosleđen jedinici");
                NetworkHelper.SendMessage(response, clientSocket);
            }
        }

        private void HandleUnitRegistration(NetworkMessage message, Socket clientSocket)
        {
            if (message.Podaci is string jedinicaId)
            {
                jedinice[clientSocket] = jedinicaId;
                Console.WriteLine($"[JEDINICA REGISTROVANA] {jedinicaId}");
                
                // Automatski prosledi čekajuće zahteve ovoj jedinici
                ProcessPendingRequests(jedinicaId, clientSocket);
                
                NetworkMessage response = new NetworkMessage(MessageType.Potvrda, "Jedinica uspešno registrovana");
                NetworkHelper.SendMessage(response, clientSocket);
            }
        }

        private void HandlePatientListRequest(NetworkMessage message, Socket clientSocket)
        {
            var sortedPatients = pacijenti.OrderByDescending(p => p.Prioritet)
                                         .ThenBy(p => p.VremeRegistracije)
                                         .ToList();
            
            NetworkMessage response = new NetworkMessage(MessageType.ListaPacijenata, "Lista pacijenata", sortedPatients);
            NetworkHelper.SendMessage(response, clientSocket);
            Console.WriteLine("[LISTA PACIJENATA] Poslata lekaru specijalisti");
        }
        
        private void HandlePatientStatusUpdate(NetworkMessage message, Socket clientSocket)
        {
            if (message.Podaci is Pacijent updatedPatient)
            {
                var existingPatient = pacijenti.FirstOrDefault(p => p.LBO == updatedPatient.LBO);
                if (existingPatient != null)
                {
                    // Ažuriraj status pacijenta
                    existingPatient.Status = updatedPatient.Status;
                    existingPatient.Prioritet = updatedPatient.Prioritet;
                    
                    Console.WriteLine($"[STATUS AŽURIRAN] {existingPatient.Ime} {existingPatient.Prezime} - Novi status: {existingPatient.Status}");
                    
                    NetworkMessage response = new NetworkMessage(MessageType.PotvrdaAzuriranja, "Status pacijenta uspešno ažuriran");
                    NetworkHelper.SendMessage(response, clientSocket);
                }
                else
                {
                    Console.WriteLine($"[GREŠKA] Pacijent sa LBO {updatedPatient.LBO} nije pronađen");
                    NetworkMessage response = new NetworkMessage(MessageType.Greska, "Pacijent nije pronađen");
                    NetworkHelper.SendMessage(response, clientSocket);
                }
            }
        }
        
        private void HandleUnitCompletion(NetworkMessage message, Socket clientSocket)
        {
            if (message.Podaci is MedicinskiZahtev zahtev)
            {
                // Ažuriraj status zahteva
                zahtev.Status = ZahtevStatus.Zavrsen;
                
                // Ažuriraj status pacijenta
                var pacijent = pacijenti.FirstOrDefault(p => p.LBO == zahtev.Pacijent.LBO);
                if (pacijent != null)
                {
                    switch (zahtev.TipUsluge)
                    {
                        case TipUsluge.UrgentnaPomoc:
                            pacijent.Status = PacijentStatus.UrgentnaIntervencija;
                            break;
                        case TipUsluge.Pregled:
                            pacijent.Status = PacijentStatus.PregledObavljen;
                            break;
                        case TipUsluge.Terapija:
                            pacijent.Status = PacijentStatus.TerapijaObavljena;
                            break;
                    }
                }
                
                Console.WriteLine($"[OBRADA ZAVRŠENA] {zahtev.Pacijent.Ime} {zahtev.Pacijent.Prezime} - {zahtev.TipUsluge}");
                
                NetworkMessage response = new NetworkMessage(MessageType.Potvrda, "Obrada uspešno završena");
                NetworkHelper.SendMessage(response, clientSocket);
            }
        }

        private void ForwardRequestToUnit(MedicinskiZahtev zahtev)
        {
            Socket targetUnit = null;
            string unitType = "";
            
            // Pronađi odgovarajuću jedinicu na osnovu tipa usluge
            switch (zahtev.TipUsluge)
            {
                case TipUsluge.UrgentnaPomoc:
                    targetUnit = FindAvailableUnit("UrgentnaJedinica");
                    unitType = "Urgentna jedinica";
                    break;
                case TipUsluge.Pregled:
                    targetUnit = FindAvailableUnit("DijagnostickaJedinica");
                    unitType = "Dijagnostička jedinica";
                    break;
                case TipUsluge.Terapija:
                    targetUnit = FindAvailableUnit("TerapeutskaJedinica");
                    unitType = "Terapeutska jedinica";
                    break;
            }
            
            if (targetUnit != null)
            {
                // Prosleđi zahtev jedinici
                NetworkMessage unitMessage = new NetworkMessage(MessageType.ZahtevZaObradu, 
                    $"Novi zahtev za obradu - {zahtev.Pacijent.Ime} {zahtev.Pacijent.Prezime}", zahtev);
                NetworkHelper.SendMessage(unitMessage, targetUnit);
                
                Console.WriteLine($"[ZAHTEV PROSLEĐEN] {zahtev.Pacijent.Ime} {zahtev.Pacijent.Prezime} -> {unitType}");
                
                // Ažuriraj status zahteva
                zahtev.Status = ZahtevStatus.UObradi;
            }
            else
            {
                Console.WriteLine($"[NEMA DOSTUPNIH JEDINICA] Zahtev od {zahtev.Pacijent.Ime} {zahtev.Pacijent.Prezime} čeka dostupnost jedinice");
                zahtev.Status = ZahtevStatus.NaCekanju;
            }
        }
        
        private void ProcessPendingRequests(string jedinicaId, Socket clientSocket)
        {
            // Pronađi čekajuće zahteve koji odgovaraju ovoj jedinici
            var pendingRequests = zahtevi.Where(z => z.Status == ZahtevStatus.NaCekanju).ToList();
            
            foreach (var zahtev in pendingRequests)
            {
                bool shouldProcess = false;
                
                // Proveri da li zahtev odgovara ovoj jedinici
                if (jedinicaId.Contains("UrgentnaJedinica") && zahtev.TipUsluge == TipUsluge.UrgentnaPomoc)
                    shouldProcess = true;
                else if (jedinicaId.Contains("DijagnostickaJedinica") && zahtev.TipUsluge == TipUsluge.Pregled)
                    shouldProcess = true;
                else if (jedinicaId.Contains("TerapeutskaJedinica") && zahtev.TipUsluge == TipUsluge.Terapija)
                    shouldProcess = true;
                
                if (shouldProcess)
                {
                    // Prosledi zahtev jedinici
                    NetworkMessage unitMessage = new NetworkMessage(MessageType.ZahtevZaObradu, 
                        $"Čekajući zahtev za obradu - {zahtev.Pacijent.Ime} {zahtev.Pacijent.Prezime}", zahtev);
                    NetworkHelper.SendMessage(unitMessage, clientSocket);
                    
                    // Ažuriraj status zahteva
                    zahtev.Status = ZahtevStatus.UObradi;
                    
                    Console.WriteLine($"[ČEKAJUĆI ZAHTEV PROSLEĐEN] {zahtev.Pacijent.Ime} {zahtev.Pacijent.Prezime} -> {jedinicaId}");
                    break; // Pošalji samo jedan zahtev po jedinici
                }
            }
        }
        
        private Socket FindAvailableUnit(string unitType)
        {
            foreach (var kvp in jedinice)
            {
                if (kvp.Value.Contains(unitType))
                {
                    return kvp.Key;
                }
            }
            return null;
        }
        
        private void UpdatePatientPriority(Pacijent pacijent, TipUsluge tipUsluge)
        {
            switch (tipUsluge)
            {
                case TipUsluge.UrgentnaPomoc:
                    pacijent.Prioritet = Prioritet.Kritican;
                    break;
                case TipUsluge.Pregled:
                    if (pacijent.Prioritet < Prioritet.Visok)
                        pacijent.Prioritet = Prioritet.Visok;
                    break;
                case TipUsluge.Terapija:
                    if (pacijent.Prioritet < Prioritet.Normalan)
                        pacijent.Prioritet = Prioritet.Normalan;
                    break;
            }
        }

        private void RemoveClient(Socket clientSocket)
        {
            try
            {
                // Poravnanje utičnica - uklanjanje klijenta
                for (int i = 0; i < MAX_CLIENTS; i++)
                {
                    if (clientSockets[i] == clientSocket)
                    {
                        clientSockets[i] = null;
                        Console.WriteLine($"[KLIJENT UKLONJEN] Utičnica {i} oslobođena");
                        break;
                    }
                }
                
                // Polling model - ne treba checkRead lista
                
                // Ukloni iz jedinica ako je bio registrovan
                if (jedinice.ContainsKey(clientSocket))
                {
                    string jedinicaId = jedinice[clientSocket];
                    jedinice.Remove(clientSocket);
                    Console.WriteLine($"[JEDINICA UKLONJENA] {jedinicaId}");
                }
                
                // Zatvorii utičnicu
                clientSocket.Close();
                
                // Polling model - serverska utičnica je uvek dostupna za polling
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri uklanjanju klijenta: {ex.Message}");
            }
        }


        private int GetConnectedClientsCount()
        {
            int count = 0;
            for (int i = 0; i < MAX_CLIENTS; i++)
            {
                if (clientSockets[i] != null)
                    count++;
            }
            return count;
        }

        private void DisplayStatus()
        {
            // Kratki prikaz statusa (svakih 30 sekundi)
            DateTime now = DateTime.Now;
            if ((now - lastStatusDisplay).TotalSeconds >= 30)
            {
                Console.WriteLine($"[STATUS] Klijenti: {GetConnectedClientsCount()}/{MAX_CLIENTS}, " +
                                 $"Pacijenti: {pacijenti.Count}, Zahtevi: {zahtevi.Count}, " +
                                 $"Jedinice: {jedinice.Count}");
                
                // Prikaži tabelu pacijenata
                DisplayPatientsTable();
                lastStatusDisplay = now;
            }
        }
        
        private void DisplayPatientsTable()
        {
            if (pacijenti.Count > 0)
            {
                Console.WriteLine("\n=== TABELA PACIJENATA ===");
                Console.WriteLine($"{"LBO",-8} {"Ime",-12} {"Prezime",-12} {"Status",-20} {"Prioritet",-10} {"Tip Usluge",-15}");
                Console.WriteLine(new string('-', 85));
                
                foreach (var pacijent in pacijenti.OrderByDescending(p => p.Prioritet).ThenBy(p => p.VremeRegistracije))
                {
                    Console.WriteLine($"{pacijent.LBO,-8} {pacijent.Ime,-12} {pacijent.Prezime,-12} {pacijent.Status,-20} {pacijent.Prioritet,-10} {pacijent.TipUsluge,-15}");
                }
                Console.WriteLine(new string('-', 85));
            }
        }

        public void Stop()
        {
            isRunning = false;
            
            // Pošalji poruku o završetku svim klijentima
            NetworkMessage endMessage = new NetworkMessage(MessageType.KrajKomunikacije, "Server se gasi");
            
            for (int i = 0; i < MAX_CLIENTS; i++)
            {
                if (clientSockets[i] != null)
                {
                    try
                    {
                        NetworkHelper.SendMessage(endMessage, clientSockets[i]);
                        clientSockets[i].Close();
                    }
                    catch { }
                    clientSockets[i] = null;
                }
            }
            
            serverSocket?.Close();
            Console.WriteLine("Server je zaustavljen.");
        }
    }
}
