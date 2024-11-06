using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ETS_CRUD_DEMO.Models
{
    public class State
    {
        [Key]
        public Guid StateId { get; set; }

        [Required]
        [MaxLength(100)]
        [DisplayName("State")]
        public string StateName { get; set; }

        public ICollection<City> Cities { get; set; }
        public ICollection<Employee> Employees { get; set; }
    }

}
