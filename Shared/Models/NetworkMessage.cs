using System;

namespace Telemedicina.Shared.Models
{
    [Serializable]
    public class NetworkMessage
    {
        public MessageType Tip { get; set; }
        public string Sadrzaj { get; set; }
        public object Podaci { get; set; }
        public DateTime VremeSlanja { get; set; }
        public string Id { get; set; }

        public NetworkMessage()
        {
            Id = Guid.NewGuid().ToString();
            VremeSlanja = DateTime.Now;
        }

        public NetworkMessage(MessageType tip, string sadrzaj, object podaci = null)
        {
            Id = Guid.NewGuid().ToString();
            VremeSlanja = DateTime.Now;
            Tip = tip;
            Sadrzaj = sadrzaj;
            Podaci = podaci;
        }
    }

    public enum MessageType
    {
        // Pacijent -> Server
        RegistracijaPacijenta,
        ZahtevZaUslugom,
        
        // Server -> Jedinice
        ZahtevZaObradu,
        DodelaUrgentnojJedinici,
        DodelaDijagnostickojJedinici,
        DodelaTerapeutskojJedinici,
        
        // Jedinice -> Server
        RegistracijaJedinice,
        PotvrdaPrimitka,
        RezultatPregleda,
        RezultatTerapije,
        StatusUrgentneIntervencije,
        ZavrsenaObrada,
        
        // Lekar -> Server
        ZahtevZaListomPacijenata,
        AzuriranjeStatusaPacijenta,
        
        // Server -> Lekar
        ListaPacijenata,
        PotvrdaAzuriranja,
        
        // Opšte
        Potvrda,
        Greska,
        KrajKomunikacije
    }
}
