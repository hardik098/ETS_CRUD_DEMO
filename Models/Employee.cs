using ETS_CRUD_DEMO.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;
using System.ComponentModel;

namespace ETS_CRUD_DEMO.Models
{
    public class Employee
    {
        [Key]
        public Guid EmployeeId { get; set; }

        [Required]
        [MaxLength(50)]
        [DisplayName("First Name")]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(50)]
        [DisplayName("Last Name")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [DisplayName("Email")]
        public string Email { get; set; }

        [Phone]
        [DisplayName("Phno")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Please enter a valid 10-digit phone number")]
        public string PhoneNumber { get; set; }

        [DisplayName("Gender")]
        public GenderOptions Gender { get; set; }

        [DisplayName("D.O.B")]
        [DataType(DataType.Date)]
        public DateTime DOB { get; set; }

        [Column(TypeName = "jsonb")]
        [DisplayName("Skills")]
        public List<string> Skills { get; set; } = new List<string>();

        [ForeignKey("Department")]
        [DisplayName("Department")]
        public Guid DepartmentId { get; set; }
        public Department Department { get; set; }

        [ForeignKey("Role")]
        [DisplayName("Role")]
        public Guid RoleId { get; set; }
        public Role Role { get; set; }

        [Display(Name = "Active")] 
        public bool IsActive { get; set; }

        [DisplayName("Profile Picture")]
        public string? ProfilePicture { get; set; }

        [ForeignKey("State")]
        [DisplayName("State")]
        public Guid? StateId { get; set; }
        public State? State { get; set; }

        [ForeignKey("City")]
        [DisplayName("City")]
        public Guid? CityId { get; set; }
        public City? City { get; set; }

        [DisplayName("Joining Date")]
        [DataType(DataType.Date)]
        public DateTime JoiningDate { get; set; }
    }


}
