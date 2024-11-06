namespace ETS_CRUD_DEMO.Services.Interfaces
{
    public interface IEmailService
    {
        Task<(bool success, string message)> SendEmailAsync(string to, string subject, string body);

    }
}
