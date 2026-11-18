using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using tutorial_demo_2.Models;
using tutorial_demo_2.Services;

namespace tutorial_demo_2.ViewModels;

public partial class ForgotPasswordViewModel : ObservableObject
{
    private readonly IForgotPasswordService _forgotPasswordService;

    [ObservableProperty]
    private string workmanId = string.Empty;

    [ObservableProperty]
    private string message = string.Empty;

    [ObservableProperty]
    private string messageColor = "#D32F2F";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMessage))]
    private bool showMessage;

    public bool IsNotBusy => !IsBusy;

    public bool HasMessage => ShowMessage;

    public ForgotPasswordViewModel(
        IForgotPasswordService forgotPasswordService)
    {
        _forgotPasswordService = forgotPasswordService;
    }

    [RelayCommand]
    private async Task ForgotPasswordAsync()
    {
        if (IsBusy)
            return;

        ShowMessage = false;
        Message = string.Empty;

        if (string.IsNullOrWhiteSpace(WorkmanId))
        {
            SetMessage("Please enter your Workman ID.", "#D32F2F");
            return;
        }

        IsBusy = true;

        try
        {
            var request = new ForgotPasswordRequest
            {
                WorkmanId = WorkmanId.Trim().ToUpperInvariant()
            };

            var result = await _forgotPasswordService
                .ForgotPasswordAsync(request);

            if (result.Success)
            {
                SetMessage(
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Your password recovery request was submitted."
                        : result.Message,
                    "#16834A");
            }
            else
            {
                SetMessage(
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Unable to process your request."
                        : result.Message,
                    "#D32F2F");
            }
        }
        catch (HttpRequestException ex)
        {
            SetMessage(ex.Message, "#D32F2F");
        }
        catch (Exception)
        {
            SetMessage(
                "Something went wrong. Please try again.",
                "#D32F2F");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BackToLoginAsync()
    {
        await Shell.Current.GoToAsync("//LoginPage");
    }

    private void SetMessage(string message, string color)
    {
        Message = message;
        MessageColor = color;
        ShowMessage = true;
    }
}