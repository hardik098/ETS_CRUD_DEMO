using ETS_CRUD_DEMO.Models;
using ETS_CRUD_DEMO.Services.Interfaces;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using Microsoft.Extensions.Options;
using RestSharp;
using RestSharp.Authenticators;

namespace ETS_CRUD_DEMO.Services.Implementations
{
    public class MailgunEmailService:IEmailService
    {
        private readonly MailgunSettings _mailgunSettings;
        private readonly ILogger<MailgunEmailService> _logger;


        public MailgunEmailService(
          IOptions<MailgunSettings> mailgunSettings,
          ILogger<MailgunEmailService> logger)
        {
            _mailgunSettings = mailgunSettings.Value;
            _logger = logger;
        }

        public async Task<(bool success, string message)> SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var client = new RestClient(new RestClientOptions
                {
                    BaseUrl = new Uri($"https://api.mailgun.net/v3/{_mailgunSettings.Domain}"),
                    Authenticator = new HttpBasicAuthenticator("api", _mailgunSettings.ApiKey)
                });

                var request = new RestRequest("messages", RestSharp.Method.Post);
                request.AddParameter("from", $"{_mailgunSettings.FromName} <{_mailgunSettings.FromEmail}>");
                request.AddParameter("to", to);
                request.AddParameter("subject", subject);
                request.AddParameter("html", body);

                var response = await client.ExecuteAsync(request);

                if (response.IsSuccessful)
                {
                    _logger.LogInformation($"Email sent successfully to {to}");
                    return (true, "Email sent successfully");
                }

                _logger.LogError($"Failed to send email: {response.ErrorMessage}");
                return (false, "Failed to send email");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email");
                return (false, "An error occurred while sending the email");
            }
        }
    }
}
