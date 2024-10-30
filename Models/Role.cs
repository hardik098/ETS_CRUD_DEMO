using System.ComponentModel.DataAnnotations;

namespace ETS_CRUD_DEMO.Models
{
    public class Role
    {
        [Key]
        public Guid RoleId { get; set; }

        [Required]
        [MaxLength(50)]
        public string RoleName { get; set; }

        [MaxLength(1000)]
        public string Permissions { get; set; }

        public ICollection<Employee> Employees { get; set; }
    }


}
