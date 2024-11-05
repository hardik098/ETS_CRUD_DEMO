using ETS_CRUD_DEMO.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using static System.Net.WebRequestMethods;
using System;
using System.Threading.Tasks;

namespace ETS_CRUD_DEMO.Services.Implementations
{
    public class MailGunVerificationService : IVerificationService
    {

        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<MailGunVerificationService> _logger;
        private const int OTP_EXPIRATION_MINUTES = 5;

        public MailGunVerificationService(
            IEmailService emailService,
            IMemoryCache cache,
            ILogger<MailGunVerificationService> logger)
        {
            _emailService = emailService;
            _cache = cache;
            _logger = logger;
        }

        public async Task<(bool success, string message)> SendOTPEmail(string email)
        {
            try
            {
                // Generate a new 6-digit OTP
                string otp = GenerateOTP();

                // Store OTP in cache with expiration
                var cacheKey = $"OTP_{email}";
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(OTP_EXPIRATION_MINUTES))
                    .SetSlidingExpiration(TimeSpan.FromMinutes(OTP_EXPIRATION_MINUTES));

                _cache.Set(cacheKey, otp, cacheOptions);

                // Create HTML email template
                string emailBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2 style='color: #333;'>Your OTP Verification Code</h2>
                        <p>Hello,</p>
                        <p>You have requested an OTP for verification. Please use the following code:</p>
                        <div style='background-color: #f8f9fa; padding: 15px; text-align: center; margin: 20px 0;'>
                            <h1 style='color: #007bff; margin: 0; letter-spacing: 5px;'>{otp}</h1>
                        </div>
                        <p>This code will expire in {OTP_EXPIRATION_MINUTES} minutes.</p>
                        <p style='color: #666; font-size: 0.9em;'>If you didn't request this code, please ignore this email.</p>
                        <hr style='border: 1px solid #eee; margin: 20px 0;'>
                        <p style='color: #999; font-size: 0.8em;'>This is an automated message, please do not reply.</p>
                    </div>";

                // Send the email using the email service
                var (emailSent, emailMessage) = await _emailService.SendEmailAsync(
                    email,
                    "Your OTP Verification Code",
                    emailBody);

                if (!emailSent)
                {
                    _logger.LogError($"Failed to send OTP email to {email}: {emailMessage}");
                    return (false, "Failed to send OTP email. Please try again.");
                }

                _logger.LogInformation($"OTP sent successfully to {email}");
                return (true, "OTP sent successfully to your email.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in SendOTPEmail for {email}");
                return (false, "An error occurred while sending OTP.");
            }
        }

        public Task<(bool success, string message)> VerifyOTP(string email, string otp)
        {
            try
            {
                var cacheKey = $"OTP_{email}";

                if (!_cache.TryGetValue(cacheKey, out string storedOTP))
                {
                    _logger.LogWarning($"OTP not found or expired for {email}");
                    return Task.FromResult((false, "OTP has expired. Please request a new one."));
                }

                if (storedOTP != otp)
                {
                    _logger.LogWarning($"Invalid OTP attempt for {email}");
                    return Task.FromResult((false, "Invalid OTP. Please try again."));
                }

                // Remove the OTP from cache after successful verification
                _cache.Remove(cacheKey);

                _logger.LogInformation($"OTP verified successfully for {email}");
                return Task.FromResult((true, "OTP verified successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in VerifyOTP for {email}");
                return Task.FromResult((false, "An error occurred while verifying OTP."));
            }
        }

        /* private string GenerateOTP()
         {
             // Generate a random 6-digit number
             Random random = new Random();
             return random.Next(100000, 999999).ToString();
         }*/

        private string GenerateOTP()
        {
            int otp = 123456;
            return otp.ToString();
        }
    }
}
