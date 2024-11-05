using System.ComponentModel.DataAnnotations;

namespace ETS_CRUD_DEMO.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address")]

        public string Email { get; set; }
    }
}
