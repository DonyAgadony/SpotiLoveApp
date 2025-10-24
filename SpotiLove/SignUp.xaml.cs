using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SpotiLove;

public partial class SignUp : ContentPage
{
    private readonly HttpClient _httpClient;
    private const string API_BASE_URL = "https://spotilove-2.onrender.com"; // Update with your API URL

    public SignUp()
    {
        InitializeComponent();
        _httpClient = new HttpClient { BaseAddress = new Uri(API_BASE_URL) };
    }

    // Navigate to Login page
    private async void OnGoToLogin(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//Login");
    }

    // Handle Create Account button
    private async void OnCreateAccount(object sender, EventArgs e)
    {
        // Get form values
        var name = NameEntry.Text?.Trim();
        var email = EmailEntry.Text?.Trim();
        var ageText = AgeEntry.Text?.Trim();
        var gender = GenderPicker.SelectedItem?.ToString();
        var password = PasswordEntry.Text;
        var confirmPassword = ConfirmPasswordEntry.Text;
        var termsAccepted = TermsCheckBox.IsChecked;

        // Comprehensive validation
        var validationError = ValidateForm(name, email, ageText, gender, password, confirmPassword, termsAccepted);
        if (validationError != null)
        {
            await DisplayAlert("Validation Error", validationError, "OK");
            return;
        }

        // Parse age (we know it's valid from validation)
        int age = int.Parse(ageText!);

        try
        {
            // Show loading indicator
            var loadingPage = new ContentPage
            {
                Content = new VerticalStackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Spacing = 20,
                    Children =
                    {
                        new ActivityIndicator
                        {
                            IsRunning = true,
                            Color = Color.FromArgb("#1db954"),
                            WidthRequest = 50,
                            HeightRequest = 50
                        },
                        new Label
                        {
                            Text = "Creating your account...",
                            TextColor = Colors.White,
                            FontSize = 16
                        }
                    }
                },
                BackgroundColor = Color.FromArgb("#121212")
            };

            await Navigation.PushModalAsync(loadingPage, false);

            // Call registration API
            var registerData = new
            {
                Name = name,
                Email = email,
                Password = password,
                Age = age,
                Gender = gender
            };

            var json = JsonSerializer.Serialize(registerData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/auth/register", content);

            await Navigation.PopModalAsync(false);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var registerResponse = JsonSerializer.Deserialize<RegisterResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (registerResponse?.Success == true && registerResponse.User != null)
                {
                    // Store user data
                    await SecureStorage.SetAsync("user_id", registerResponse.User.Id.ToString());
                    await SecureStorage.SetAsync("user_email", registerResponse.User.Email ?? "");
                    await SecureStorage.SetAsync("user_name", registerResponse.User.Name ?? "");

                    if (!string.IsNullOrEmpty(registerResponse.Token))
                    {
                        await SecureStorage.SetAsync("auth_token", registerResponse.Token);
                    }

                    // Show success message
                    await DisplayAlert("Success",
                        $"Welcome to SpotiLove, {registerResponse.User.Name}! Your account has been created.",
                        "Get Started");

                    UserData.Current = registerResponse.User;

                    // Navigate to main page
                    await Shell.Current.GoToAsync("//MainPage");
                }
                else
                {
                    await DisplayAlert("Registration Failed",
                        registerResponse?.Message ?? "Unable to create account",
                        "OK");
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                // Try to parse error message
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    await DisplayAlert("Registration Failed",
                        errorResponse?.Message ?? "Unable to create account. Please try again.",
                        "OK");
                }
                catch
                {
                    await DisplayAlert("Registration Failed",
                        "Unable to create account. Please try again.",
                        "OK");
                }
            }
        }
        catch (HttpRequestException)
        {
            await Navigation.PopModalAsync(false);
            await DisplayAlert("Connection Error",
                "Unable to connect to the server. Please check your internet connection and try again.",
                "OK");
        }
        catch (Exception ex)
        {
            await Navigation.PopModalAsync(false);
            await DisplayAlert("Error",
                $"An unexpected error occurred: {ex.Message}",
                "OK");
        }
    }

    // Comprehensive form validation
    private string? ValidateForm(string? name, string? email, string? ageText, string? gender,
                                  string? password, string? confirmPassword, bool termsAccepted)
    {
        // Name validation
        if (string.IsNullOrWhiteSpace(name))
            return "Please enter your full name";

        if (name.Length < 2)
            return "Name must be at least 2 characters long";

        if (name.Length > 50)
            return "Name must be less than 50 characters";

        // Email validation
        if (string.IsNullOrWhiteSpace(email))
            return "Please enter your email address";

        if (!IsValidEmail(email))
            return "Please enter a valid email address";

        // Age validation
        if (string.IsNullOrWhiteSpace(ageText))
            return "Please enter your age";

        if (!int.TryParse(ageText, out int age))
            return "Please enter a valid age";

        if (age < 18)
            return "You must be at least 18 years old to use SpotiLove";

        if (age > 120)
            return "Please enter a valid age";

        // Gender validation
        if (string.IsNullOrWhiteSpace(gender))
            return "Please select your gender";

        // Password validation
        if (string.IsNullOrWhiteSpace(password))
            return "Please enter a password";

        if (password.Length < 6)
            return "Password must be at least 6 characters long";

        if (password.Length > 100)
            return "Password must be less than 100 characters";

        // Check password strength
        if (!HasMinimumPasswordStrength(password))
            return "Password should contain at least one letter and one number";

        // Confirm password validation
        if (string.IsNullOrWhiteSpace(confirmPassword))
            return "Please confirm your password";

        if (password != confirmPassword)
            return "Passwords do not match";

        // Terms acceptance validation
        if (!termsAccepted)
            return "You must agree to the Terms of Service and Privacy Policy to continue";

        return null; // All validation passed
    }

    // Email validation using regex
    private bool IsValidEmail(string email)
    {
        try
        {
            var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, emailPattern, RegexOptions.IgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    // Check minimum password strength
    private bool HasMinimumPasswordStrength(string password)
    {
        // At least one letter and one number
        bool hasLetter = password.Any(char.IsLetter);
        bool hasDigit = password.Any(char.IsDigit);
        return hasLetter && hasDigit;
    }

    // Handle back button
    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//Login");
    }

    // Social signup handlers
    private async void OnGoogleSignUp(object sender, EventArgs e)
    {
        await DisplayAlert("Google Sign Up", "Google authentication coming soon!", "OK");
        // TODO: Implement Google OAuth
    }

    private async void OnAppleSignUp(object sender, EventArgs e)
    {
        await DisplayAlert("Apple Sign Up", "Apple authentication coming soon!", "OK");
        // TODO: Implement Apple Sign In
    }

    private async void OnSpotifySignUp(object sender, EventArgs e)
    {
        try
        {
            var spotifyLoginUrl = $"{API_BASE_URL}/login";
            await Browser.OpenAsync(spotifyLoginUrl, BrowserLaunchMode.SystemPreferred);

            await DisplayAlert("Spotify Sign Up",
                "After authorizing with Spotify, your account will be created automatically.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Unable to open Spotify sign up: {ex.Message}", "OK");
        }
    }

    // Terms and Privacy handlers
    private async void OnTermsClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Terms of Service",
            "By using SpotiLove, you agree to our Terms of Service.\n\n" +
            "• You must be 18 years or older\n" +
            "• You agree to use the service respectfully\n" +
            "• Your account is personal and non-transferable\n" +
            "• We reserve the right to terminate accounts that violate our terms\n\n" +
            "For full terms, visit our website.",
            "OK");
    }

    private async void OnPrivacyClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Privacy Policy",
            "Your privacy is important to us.\n\n" +
            "• We encrypt your personal data\n" +
            "• We never sell your information\n" +
            "• You control your profile visibility\n" +
            "• You can delete your account anytime\n" +
            "• We use cookies to improve your experience\n\n" +
            "For full privacy policy, visit our website.",
            "OK");
    }

    // Response models
    private class RegisterResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Token { get; set; }
        public UserData? User { get; set; }
    }
    private class ErrorResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}