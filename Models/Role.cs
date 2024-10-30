namespace ETS_CRUD_DEMO.Models
{
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string Permissions { get; set; } // JSON or comma-separated values
    }

}
