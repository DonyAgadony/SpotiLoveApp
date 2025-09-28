using Microsoft.Maui.Controls;

namespace SpotiLove;

public partial class SignUp : ContentPage
{
    public SignUp()
    {
        InitializeComponent();
    }

    // Navigate to Login page
    private async void OnGoToLogin(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//Login");
    }

    // Handle Create Account button
    private async void OnCreateAccount(object sender, EventArgs e)
    {
        // Add your signup logic here
        // For now, navigate to main page after successful signup
        await Shell.Current.GoToAsync("//MainPage");
    }

    // Handle back button
    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}