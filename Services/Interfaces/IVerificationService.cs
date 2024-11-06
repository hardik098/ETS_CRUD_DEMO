namespace ETS_CRUD_DEMO.Services.Interfaces
{
    public interface IVerificationService
    {
        Task<(bool success, string message)> SendOTPEmail(string email);
        Task<(bool success, string message)> VerifyOTP(string email, string otp);
    }
}
