using System.ComponentModel.DataAnnotations;

namespace WebApplicationCentralino.Models
{
    public class Contatto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Il numero di contatto è obbligatorio")]
        [RegularExpression(@"^\d{1,15}$", ErrorMessage = "Il numero deve contenere solo cifre e non può superare i 15 caratteri")]
        [Display(Name = "Numero Contatto")]
        public string? NumeroContatto { get; set; }

        [Required(ErrorMessage = "La ragione sociale è obbligatoria")]
        [RegularExpression(@"^.* - Comune di [A-Za-z\s']+$", 
            ErrorMessage = "La ragione sociale deve essere nel formato 'ragioneSociale - Comune di PaeseX'")]
        [Display(Name = "Ragione Sociale")]
        public string? RagioneSociale { get; set; }

        [RegularExpression(@"^[A-Za-z\s']+$", ErrorMessage = "La città non può contenere numeri")]
        [Display(Name = "Città")]
        public string? Citta { get; set; }

        [Required(ErrorMessage = "Specificare se il numero è interno o esterno")]
        [Display(Name = "Interno")]
        public int? Interno { get; set; }
    }
}
