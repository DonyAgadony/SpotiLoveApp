using Microsoft.Maui.Controls;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Web;
using System.Diagnostics;

namespace SpotiLove;

public partial class Login : ContentPage
{
    private readonly HttpClient _httpClient;
    public const string API_BASE_URL = "https://spotilove-2.onrender.com";

    public Login()
    {
        InitializeComponent();
        _httpClient = new HttpClient { BaseAddress = new Uri(API_BASE_URL) };
    }

    public async void OnGoToSignUp(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new SignUp());
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
            loadingPage = new ContentPage
            {
                Content = new VerticalStackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Spacing = 20,
                    Children =
                    {
                        new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#1db954"), WidthRequest = 50, HeightRequest = 50 },
                        new Label { Text = "Signing in...", TextColor = Colors.White, FontSize = 16 }
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
                UserData.Current = new UserData
                {
                    Id = loginResponse.User.Id,
                    Name = loginResponse.User.Name,
                    Email = loginResponse.User.Email,
                    Age = loginResponse.User.Age
                };

                await SecureStorage.SetAsync("user_id", UserData.Current.Id.ToString());
                if (!string.IsNullOrEmpty(UserData.Current.Name))
                    await SecureStorage.SetAsync("user_name", UserData.Current.Name);
                if (!string.IsNullOrEmpty(UserData.Current.Email))
                    await SecureStorage.SetAsync("user_email", UserData.Current.Email);

                if (!string.IsNullOrEmpty(loginResponse.Token))
                    await SecureStorage.SetAsync("auth_token", loginResponse.Token);

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

    private async void OnSpotifyLogin(object sender, EventArgs e)
    {
        try
        {
            var spotifyLoginUrl = $"{API_BASE_URL}/login";

            var loadingPage = new ContentPage
            {
                Content = new VerticalStackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Spacing = 20,
                    Children =
                {
                    new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#1db954"), WidthRequest = 50, HeightRequest = 50 },
                    new Label { Text = "Connecting to Spotify...", TextColor = Colors.White, FontSize = 16 }
                }
                },
                BackgroundColor = Color.FromArgb("#121212")
            };

            await Navigation.PushModalAsync(loadingPage, false);

            // Open Spotify login
            var success = await Browser.OpenAsync(spotifyLoginUrl, BrowserLaunchMode.SystemPreferred);

            await Navigation.PopModalAsync(false);

            if (!success)
            {
                await DisplayAlert("Error", "Could not open Spotify login page.", "OK");
                return;
            }

            await DisplayAlert("Spotify Login",
                "After authorizing with Spotify, you'll be automatically signed in.",
                "OK");

        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Unable to connect to Spotify: {ex.Message}", "OK");
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

    // DTOs for standard login
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

// Handles the callback from the Spotify web Auth deep link.
public class SpotifyAuthHandlerLogin
{
    public static async Task HandleSpotifyCallback(string uri)
    {
        try
        {
            Debug.WriteLine($"🔐 Processing Spotify callback: {uri}");

            var parsedUri = new Uri(uri);
            var queryParams = HttpUtility.ParseQueryString(parsedUri.Query);

            var token = queryParams["token"];
            var userIdStr = queryParams["userId"];
            var isNewUserStr = queryParams["isNewUser"];
            var name = queryParams["name"];

            Debug.WriteLine($"📋 Parsed params - Token: {token?.Substring(0, 8)}..., UserId: {userIdStr}, IsNew: {isNewUserStr}, Name: {name}");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userIdStr))
            {
                Debug.WriteLine("❌ Missing token or userId in callback");
                await Shell.Current.DisplayAlert("Error", "Invalid authentication response", "OK");
                return;
            }

            if (!Guid.TryParse(userIdStr, out Guid userId))
            {
                Debug.WriteLine($"❌ Invalid userId format: {userIdStr}");
                await Shell.Current.DisplayAlert("Error", "Invalid user ID in response", "OK");
                return;
            }

            bool isNewUser = bool.Parse(isNewUserStr ?? "false");

            // Store authentication data
            await SecureStorage.SetAsync("auth_token", token);
            await SecureStorage.SetAsync("user_id", userId.ToString());
            Debug.WriteLine("✅ Saved auth token and user ID to secure storage");

            // Fetch full user profile from API
            using var httpClient = new HttpClient();
            // Using the constant from Login class to ensure consistency
            httpClient.BaseAddress = new Uri(Login.API_BASE_URL);
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            Debug.WriteLine($"🌐 Fetching user profile for ID: {userId}");
            var response = await httpClient.GetAsync($"/users/{userId}");
            Debug.WriteLine($"📡 API Response: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"📄 Response length: {content.Length} characters");

                var userResponse = JsonSerializer.Deserialize<UserResponse>(
                    content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userResponse?.User != null)
                {
                    Debug.WriteLine($"✅ User data deserialized: {userResponse.User.Name}");

                    // Set global user data
                    UserData.Current = new UserData
                    {
                        Id = userResponse.User.Id,
                        Name = userResponse.User.Name,
                        Email = userResponse.User.Email,
                        Age = userResponse.User.Age
                    };

                    await SecureStorage.SetAsync("user_name", userResponse.User.Name ?? "");
                    await SecureStorage.SetAsync("user_email", userResponse.User.Email ?? "");

                    Debug.WriteLine($"✅ UserData.Current set: ID={UserData.Current.Id}, Name={UserData.Current.Name}");

                    // Show success message
                    string message = isNewUser
                        ? $"Welcome to SpotiLove, {userResponse.User.Name}! Your music profile has been imported from Spotify."
                        : $"Welcome back, {userResponse.User.Name}! Your music profile has been updated.";

                    await Shell.Current.DisplayAlert("Success", message, "OK");

                    // Navigate to main page
                    Debug.WriteLine("🚀 Navigating to MainPage...");
                    await Shell.Current.GoToAsync("//MainPage");
                }
                else
                {
                    Debug.WriteLine("❌ User data was null after deserialization");
                    await Shell.Current.DisplayAlert("Error", "Failed to load user profile", "OK");
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"❌ API error: {response.StatusCode}");
                Debug.WriteLine($"❌ Error content: {errorContent}");
                await Shell.Current.DisplayAlert("Error", "Failed to fetch user profile from server", "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Exception in HandleSpotifyCallback: {ex.Message}");
            Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            await Shell.Current.DisplayAlert("Error", $"Failed to complete Spotify authentication: {ex.Message}", "OK");
        }
    }

    private class UserResponse
    {
        public UserDto? User { get; set; }
    }
}