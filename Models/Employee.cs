using ETS_CRUD_DEMO.Enums;
using System.Data;

namespace ETS_CRUD_DEMO.Models
{
    public class Employee
    {
        public int EmployeeId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public GenderOptions Gender { get; set; }
        public DateTime DOB { get; set; }
        public int DepartmentId { get; set; }
        public Department Department { get; set; }
        public int RoleId { get; set; }
        public Role Role { get; set; }
        public bool IsActive { get; set; }
        public List<string> Skills { get; set; } // JSON serialization
        public string ProfilePicture { get; set; }
        public int StateId { get; set; }
        public State State { get; set; }
        public int CityId { get; set; }
        public City City { get; set; }
        public DateTime JoiningDate { get; set; }
    }

}
