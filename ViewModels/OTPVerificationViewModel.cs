using System.ComponentModel.DataAnnotations;

namespace ETS_CRUD_DEMO.ViewModels
{
    public class OTPVerificationViewModel
    {
        [Required]

        public string Email { get; set; }

        [Required(ErrorMessage = "OTP is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
        [Display(Name = "Enter OTP")]

        public string OTP { get; set; }

        
    }
}
