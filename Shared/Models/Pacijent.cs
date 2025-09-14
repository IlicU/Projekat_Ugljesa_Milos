using System;

namespace Telemedicina.Shared.Models
{
    [Serializable]
    public class Pacijent
    {
        public string LBO { get; set; }
        public string Ime { get; set; }
        public string Prezime { get; set; }
        public string Adresa { get; set; }
        public PacijentStatus Status { get; set; }
        public DateTime VremeRegistracije { get; set; }
        public TipUsluge TipUsluge { get; set; }
        public Prioritet Prioritet { get; set; }

        public Pacijent()
        {
            VremeRegistracije = DateTime.Now;
            Status = PacijentStatus.Cekanje;
            Prioritet = Prioritet.Normalan;
        }

        public override string ToString()
        {
            return $"{Ime} {Prezime} (LBO: {LBO}) - {Status} - {TipUsluge}";
        }
    }

    public enum PacijentStatus
    {
        Cekanje,
        UObradi,
        PregledObavljen,
        TerapijaObavljena,
        UrgentnaIntervencija,
        Zavrseno,
        Otkazano
    }

    public enum TipUsluge
    {
        Terapija,
        Pregled,
        UrgentnaPomoc
    }

    public enum Prioritet
    {
        Nizak = 1,
        Normalan = 2,
        Visok = 3,
        Kritican = 4
    }
}
