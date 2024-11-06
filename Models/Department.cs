using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ETS_CRUD_DEMO.Models
{
    public class Department
    {
        [Key]
        public Guid DepartmentId { get; set; }

        [Required]
        [MaxLength(100)]
        [DisplayName("Department")]
        public string DepartmentName { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public ICollection<Employee> Employees { get; set; }
    }

}
