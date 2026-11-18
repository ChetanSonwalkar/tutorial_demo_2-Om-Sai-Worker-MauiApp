using tutorial_demo_2.Models;

namespace tutorial_demo_2.Services;

public interface IForgotPasswordService
{
    Task<ForgotPasswordResponse> ForgotPasswordAsync(
        ForgotPasswordRequest request);
}