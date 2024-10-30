using ETS_CRUD_DEMO.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;

namespace ETS_CRUD_DEMO.Models
{
    public class Employee
    {
        [Key]
        public Guid EmployeeId { get; set; }

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }

        public GenderOptions Gender { get; set; }

        public DateTime DOB { get; set; }
        [Column(TypeName = "jsonb")]
        public List<string> Skills { get; set; } = new List<string>();

        [ForeignKey("Department")]
        public Guid DepartmentId { get; set; }
        public Department Department { get; set; }

        [ForeignKey("Role")]
        public Guid RoleId { get; set; }
        public Role Role { get; set; }

        public bool IsActive { get; set; }

        public string ProfilePicture { get; set; }

        [ForeignKey("State")]
        public Guid StateId { get; set; }
        public State State { get; set; }

        [ForeignKey("City")]
        public Guid CityId { get; set; }
        public City City { get; set; }

        public DateTime JoiningDate { get; set; }
    }


}
