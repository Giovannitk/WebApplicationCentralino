using System.ComponentModel.DataAnnotations;

namespace WebApplicationCentralino.Models
{
    public class UserEditModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Il nome è obbligatorio")]
        [Display(Name = "Nome")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "L'email è obbligatoria")]
        [EmailAddress(ErrorMessage = "Formato email non valido")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Il ruolo è obbligatorio")]
        [Display(Name = "Ruolo")]
        public string Ruolo { get; set; }
    }
} 