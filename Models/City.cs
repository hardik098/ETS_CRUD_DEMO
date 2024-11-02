using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace ETS_CRUD_DEMO.Models
{
    public class City
    {
        [Key]
        public Guid CityId { get; set; }

        [Required]
        [MaxLength(100)]
        [DisplayName("City")]
        public string CityName { get; set; }

        [ForeignKey("State")]
        [DisplayName("State")]
        public Guid StateId { get; set; }
        public State State { get; set; }

        public ICollection<Employee> Employees { get; set; }
    }

}
