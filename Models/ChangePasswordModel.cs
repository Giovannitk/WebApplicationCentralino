using System.ComponentModel.DataAnnotations;

namespace WebApplicationCentralino.Models
{
    public class ChangePasswordModel
    {
        [Required(ErrorMessage = "La password attuale è obbligatoria")]
        [DataType(DataType.Password)]
        [Display(Name = "Password Attuale")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "La nuova password è obbligatoria")]
        [StringLength(100, ErrorMessage = "La {0} deve essere lunga almeno {2} caratteri.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Nuova Password")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "La conferma della password è obbligatoria")]
        [Compare("NewPassword", ErrorMessage = "La password di conferma non corrisponde alla nuova password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Conferma Nuova Password")]
        public string ConfirmPassword { get; set; }
    }
} 