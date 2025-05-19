using System.ComponentModel.DataAnnotations;

namespace WebApplicationCentralino.Models
{
    public class Chiamata
    {
        public int Id { get; set; }
        public string? NumeroChiamante { get; set; }
        public string? NumeroChiamato { get; set; }
        public string? RagioneSocialeChiamante { get; set; }
        public string? RagioneSocialeChiamato { get; set; }


        [DataType(DataType.DateTime)]
        [Display(Name = "Data Arrivo Chiamata")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm:ss}", ApplyFormatInEditMode = true)]
        [Required(ErrorMessage = "La data di arrivo è obbligatoria")]
        [Range(typeof(DateTime), "1/1/2000", "1/1/2100", ErrorMessage = "Data non valida")]
        public DateTime DataArrivoChiamata { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Data Fine Chiamata")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm:ss}", ApplyFormatInEditMode = true)]
        [Required(ErrorMessage = "La data di fine è obbligatoria")]
        [Range(typeof(DateTime), "1/1/2000", "1/1/2100", ErrorMessage = "Data non valida")]
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
