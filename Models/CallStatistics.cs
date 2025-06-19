using System.Collections.Generic;

namespace WebApplicationCentralino.Models
{
    /// <summary>
    /// Classe che contiene le statistiche dettagliate delle chiamate
    /// </summary>
    public class DetailedCallStatistics
    {
        public int TotaleChiamate { get; set; }
        public int ChiamateInEntrata { get; set; }
        public int ChiamateInUscita { get; set; }
        public int ChiamatePerse { get; set; }
        public int ChiamateNonRisposta { get; set; }
        public int ChiamateManuali { get; set; }
        public int ChiamateAutomatiche { get; set; }
        public int ChiamateInterne { get; set; }
        public double DurataMediaChiamate { get; set; }
        public double DurataTotaleChiamate { get; set; }
        public double DurataMediaInEntrata { get; set; }
        public double DurataMediaInUscita { get; set; }
        public double DurataTotaleInEntrata { get; set; }
        public double DurataTotaleInUscita { get; set; }
        public Dictionary<string, int> ChiamatePerTipo { get; set; } = new();
        public Dictionary<string, int> ChiamatePerLocazione { get; set; } = new();
        public Dictionary<string, int> ChiamatePerGiorno { get; set; } = new();
        public Dictionary<string, int> ChiamatePerOra { get; set; } = new();
        public List<TopChiamante> TopChiamanti { get; set; } = new();
        public List<TopChiamato> TopChiamati { get; set; } = new();
        public List<TopChiamantePerLocazione> TopChiamantiPerLocazione { get; set; } = new();
        public List<TopChiamatoPerLocazione> TopChiamatiPerLocazione { get; set; } = new();
        public Dictionary<string, int> ChiamatePerLocazioneChiamante { get; set; } = new();
        public Dictionary<string, int> ChiamatePerLocazioneChiamato { get; set; } = new();
    }

    public class TopChiamante
    {
        public string Numero { get; set; } = "";
        public string RagioneSociale { get; set; } = "";
        public int NumeroChiamate { get; set; }
        public double DurataTotale { get; set; }
        public double DurataMedia => NumeroChiamate > 0 ? DurataTotale / NumeroChiamate : 0;
    }

    public class TopChiamato
    {
        public string Numero { get; set; } = "";
        public string RagioneSociale { get; set; } = "";
        public int NumeroChiamate { get; set; }
        public double DurataTotale { get; set; }
        public double DurataMedia => NumeroChiamate > 0 ? DurataTotale / NumeroChiamate : 0;
    }

    public class TopChiamantePerLocazione
    {
        public string Numero { get; set; } = "";
        public string RagioneSociale { get; set; } = "";
        public string Locazione { get; set; } = "";
        public int NumeroChiamate { get; set; }
        public double DurataTotale { get; set; }
        public double DurataMedia => NumeroChiamate > 0 ? DurataTotale / NumeroChiamate : 0;
    }

    public class TopChiamatoPerLocazione
    {
        public string Numero { get; set; } = "";
        public string RagioneSociale { get; set; } = "";
        public string Locazione { get; set; } = "";
        public int NumeroChiamate { get; set; }
        public double DurataTotale { get; set; }
        public double DurataMedia => NumeroChiamate > 0 ? DurataTotale / NumeroChiamate : 0;
    }

    /// <summary>
    /// Classe che contiene le statistiche delle chiamate e dei contatti
    /// </summary>
    public class CallStatistics
    {
        public int ChiamateOggi { get; set; }
        public int ChiamateSettimana { get; set; }
        public int ContattiAttivi { get; set; }
        public int ContattiInattivi { get; set; }
        public int ContattiIncompleti { get; set; }
    }

    public class ContactStatistics
    {
        public string Numero { get; set; } = "";
        public string RagioneSociale { get; set; } = "";
        public int ChiamateInEntrata { get; set; }
        public int ChiamateInUscita { get; set; }
        public int ChiamatePerse { get; set; }
        public int ChiamateNonRisposta { get; set; }
        public double DurataTotaleChiamate { get; set; }
        public double DurataMediaChiamate => (ChiamateInEntrata + ChiamateInUscita) > 0 
            ? DurataTotaleChiamate / (ChiamateInEntrata + ChiamateInUscita) 
            : 0;
        public Dictionary<string, int> ChiamatePerGiorno { get; set; } = new();
        public Dictionary<string, int> ChiamatePerOra { get; set; } = new();
        public List<Chiamata> UltimeChiamate { get; set; } = new();
        public Dictionary<string, int> ChiamatePerLocazioneChiamante { get; set; } = new();
        public Dictionary<string, int> ChiamatePerLocazioneChiamato { get; set; } = new();
        public List<TopChiamantePerLocazione> TopChiamantiPerLocazione { get; set; } = new();
        public List<TopChiamatoPerLocazione> TopChiamatiPerLocazione { get; set; } = new();
    }
} 