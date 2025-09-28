using Microsoft.Maui.Controls;

namespace SpotiLove;

public partial class Login : ContentPage
{
    public Login()
    {
        InitializeComponent();
    }

    // Navigate to SignUp page
    private async void OnGoToSignUp(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//SignUp");
    }

    // Handle Sign In button
    private async void OnSignIn(object sender, EventArgs e)
    {
        // Add your login logic here
        // For now, navigate to main page after successful login
        await Shell.Current.GoToAsync("//MainPage");
    }

    // Handle Forgot Password
    private async void OnForgotPassword(object sender, EventArgs e)
    {
        await DisplayAlert("Forgot Password", "Password reset functionality coming soon!", "OK");
    }

    // Handle back button
    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}