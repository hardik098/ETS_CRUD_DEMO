using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ETS_CRUD_DEMO.Models
{
    public class City
    {
        [Key]
        public Guid CityId { get; set; }

        [Required]
        [MaxLength(100)]
        public string CityName { get; set; }

        [ForeignKey("State")]
        public Guid StateId { get; set; }
        public State State { get; set; }

        public ICollection<Employee> Employees { get; set; }
    }

}
