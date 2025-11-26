namespace chat_ya_backend.Models.Dtos.RequestDtos
{
    public class LoginRequestDtos
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? TwoFactorCode { get; set; }
        public string? TwoFactorRecoveryCode { get; set; }
    }
}
