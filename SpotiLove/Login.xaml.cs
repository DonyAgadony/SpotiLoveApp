using Microsoft.Maui.Controls;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace SpotiLove;

public partial class Login : ContentPage
{
    private readonly HttpClient _httpClient;
    private const string API_BASE_URL = "https://spotilove-2.onrender.com"; // your API url

    public Login()
    {
        InitializeComponent();
        _httpClient = new HttpClient { BaseAddress = new Uri(API_BASE_URL) };
    }

    private async void OnGoToSignUp(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//SignUp");
    }

    private async void OnSignIn(object sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(email))
        {
            await DisplayAlert("Error", "Please enter your email or username", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Error", "Please enter your password", "OK");
            return;
        }

        ContentPage loadingPage = null!;
        try
        {
            // Show a lightweight modal loader
            loadingPage = new ContentPage
            {
                Content = new VerticalStackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Spacing = 20,
                    Children =
                    {
                        new ActivityIndicator { IsRunning = true, WidthRequest = 50, HeightRequest = 50 },
                        new Label { Text = "Signing in...", FontSize = 16 }
                    }
                },
                BackgroundColor = Color.FromArgb("#121212")
            };

            await Navigation.PushModalAsync(loadingPage, false);

            var loginData = new
            {
                Email = email,
                Password = password,
                RememberMe = RememberMeCheckBox.IsChecked
            };

            var json = JsonSerializer.Serialize(loginData);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/auth/login", content);

            // pop loader
            await Navigation.PopModalAsync(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                try
                {
                    var err = JsonSerializer.Deserialize<ErrorResponse>(errorContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    await DisplayAlert("Login Failed", err?.Message ?? "Unable to login. Please try again.", "OK");
                }
                catch
                {
                    await DisplayAlert("Login Failed", "Unable to login. Please try again.", "OK");
                }
                return;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (loginResponse?.Success == true && loginResponse.User != null)
            {
                // Set global shared UserData
                UserData.Current = new UserData
                {
                    Id = loginResponse.User.Id,
                    Name = loginResponse.User.Name,
                    Email = loginResponse.User.Email,
                    Age = loginResponse.User.Age
                };

                // Persist for next app start
                await SecureStorage.SetAsync("user_id", UserData.Current.Id.ToString());
                if (!string.IsNullOrEmpty(UserData.Current.Name))
                    await SecureStorage.SetAsync("user_name", UserData.Current.Name);
                if (!string.IsNullOrEmpty(UserData.Current.Email))
                    await SecureStorage.SetAsync("user_email", UserData.Current.Email);

                if (!string.IsNullOrEmpty(loginResponse.Token))
                    await SecureStorage.SetAsync("auth_token", loginResponse.Token);

                System.Diagnostics.Debug.WriteLine($"Login success. UserId={UserData.Current.Id}");

                // Navigate once
                await Shell.Current.GoToAsync("//MainPage");
                return;
            }
            else
            {
                await DisplayAlert("Login Failed", loginResponse?.Message ?? "Invalid credentials", "OK");
            }
        }
        catch (HttpRequestException)
        {
            if (loadingPage != null) await Navigation.PopModalAsync(false);
            await DisplayAlert("Connection Error", "Unable to connect to the server. Check your network.", "OK");
        }
        catch (Exception ex)
        {
            if (loadingPage != null) await Navigation.PopModalAsync(false);
            await DisplayAlert("Error", $"Login failed: {ex.Message}", "OK");
        }
    }

    private async void OnForgotPassword(object sender, EventArgs e)
    {
        var email = await DisplayPromptAsync("Forgot Password", "Enter your email address:", "Send Reset Link", "Cancel", keyboard: Keyboard.Email);
        if (string.IsNullOrWhiteSpace(email)) return;

        try
        {
            var resetData = new { Email = email.Trim() };
            var json = JsonSerializer.Serialize(resetData);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/auth/forgot-password", content);
            if (response.IsSuccessStatusCode)
                await DisplayAlert("Success", "Password reset link sent to your email!", "OK");
            else
                await DisplayAlert("Error", "Unable to send reset link. Please try again.", "OK");
        }
        catch
        {
            await DisplayAlert("Error", "Password reset functionality is currently unavailable.", "OK");
        }
    }

    private async void OnBackClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("..");
    private async void OnGoogleLogin(object sender, EventArgs e) => await DisplayAlert("Google Login", "Coming soon", "OK");
    private async void OnAppleLogin(object sender, EventArgs e) => await DisplayAlert("Apple Login", "Coming soon", "OK");
    private async void OnSpotifyLogin(object sender, EventArgs e)
    {
        try
        {
            var spotifyLoginUrl = $"{API_BASE_URL}/login";
            await Browser.OpenAsync(spotifyLoginUrl, BrowserLaunchMode.SystemPreferred);
            await DisplayAlert("Spotify Login", "After authorizing with Spotify, you'll be redirected back to the app.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Unable to open Spotify login: {ex.Message}", "OK");
        }
    }

    // quick demo/guest handlers
    private async void OnDemoUser(object sender, EventArgs e)
    {
        EmailEntry.Text = "demo@spotilove.com";
        PasswordEntry.Text = "demo123";
        await DisplayAlert("Demo User", "Demo credentials loaded. Click Sign In to continue.", "OK");
    }

    private async void OnGuestMode(object sender, EventArgs e)
    {
        var result = await DisplayAlert("Guest Mode", "Continue without an account? You'll have limited features.", "Continue", "Cancel");
        if (result)
        {
            await SecureStorage.SetAsync("is_guest", "true");
            await Shell.Current.GoToAsync("//MainPage");
        }
    }

    // Response models (use shared UserDto if you prefer; using UserDto for deserialization)
    private class LoginResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Token { get; set; }
        public UserDto? User { get; set; }
    }

    private class ErrorResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}
