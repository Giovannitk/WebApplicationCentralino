namespace WebApplicationCentralino.Models
{
    public class Chiamata
    {
        public int Id { get; set; }
        public string? NumeroChiamante { get; set; }
        public string? NumeroChiamato { get; set; }
        public string? RagioneSocialeChiamante { get; set; }
        public string? RagioneSocialeChiamato { get; set; }
        public DateTime DataArrivoChiamata { get; set; }
        public DateTime DataFineChiamata { get; set; }
        public string? TipoChiamata { get; set; }
        public string? Locazione { get; set; }
        public string? UniqueID { get; set; }

        // Proprietà calcolata per la durata della chiamata
        public TimeSpan Durata => DataFineChiamata > DataArrivoChiamata
            ? DataFineChiamata - DataArrivoChiamata
            : TimeSpan.Zero;
    }
}
