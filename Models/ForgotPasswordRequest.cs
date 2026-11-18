namespace tutorial_demo_2.Models
{
    public class ForgotPasswordRequest
    {
        public string WorkmanId { get; set; } = string.Empty;
    }

    public class ForgotPasswordResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}