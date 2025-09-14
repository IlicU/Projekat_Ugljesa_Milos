using System;

namespace Telemedicina.Shared.Models
{
    [Serializable]
    public class MedicinskiZahtev
    {
        public string Id { get; set; }
        public Pacijent Pacijent { get; set; }
        public TipUsluge TipUsluge { get; set; }
        public DateTime VremeZahteva { get; set; }
        public ZahtevStatus Status { get; set; }
        public string Opis { get; set; }
        public string Rezultat { get; set; }
        public string Jedinica { get; set; }

        public MedicinskiZahtev()
        {
            Id = Guid.NewGuid().ToString();
            VremeZahteva = DateTime.Now;
            Status = ZahtevStatus.NaCekanju;
        }

        public override string ToString()
        {
            return $"Zahtev {Id}: {Pacijent.Ime} {Pacijent.Prezime} - {TipUsluge} - {Status}";
        }
    }

    public enum ZahtevStatus
    {
        NaCekanju,
        UObradi,
        Zavrsen,
        Otkazan
    }
}
