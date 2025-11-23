using System.Web;

namespace SpotiLove;

public class SpotifyAuthHandler
{
    public static async Task HandleSpotifyCallback(string uri)
    {
        try
        {
            var parsedUri = new Uri(uri);
            var queryParams = HttpUtility.ParseQueryString(parsedUri.Query);

            var token = queryParams["token"];
            var userIdStr = queryParams["userId"];
            var isNewUserStr = queryParams["isNewUser"];

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userIdStr))
            {
                await Shell.Current.DisplayAlert("Error", "Invalid authentication response", "OK");
                return;
            }

            int userId = int.Parse(userIdStr);
            bool isNewUser = bool.Parse(isNewUserStr ?? "false");

            // Store authentication data
            await SecureStorage.SetAsync("auth_token", token);
            await SecureStorage.SetAsync("user_id", userId.ToString());

            // Fetch full user profile from API
            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://spotilove-2.onrender.com");

            var response = await httpClient.GetAsync($"/users/{userId}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var userResponse = System.Text.Json.JsonSerializer.Deserialize<UserResponse>(
                    content,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userResponse?.User != null)
                {
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

                    // Show success message
                    string message = isNewUser
                        ? $"Welcome to SpotiLove, {userResponse.User.Name}! Your music profile has been imported from Spotify."
                        : $"Welcome back, {userResponse.User.Name}! Your music profile has been updated.";

                    await Shell.Current.DisplayAlert("Success", message, "OK");
                }
            }

            // Navigate to main page
            await Shell.Current.GoToAsync("//MainPage");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to complete Spotify authentication: {ex.Message}", "OK");
        }
    }

    private class UserResponse
    {
        public UserDto? User { get; set; }
    }
}

// Add this to your App.xaml.cs or MauiProgram.cs to handle deep links
public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }

    protected override async void OnAppLinkRequestReceived(Uri uri)
    {
        base.OnAppLinkRequestReceived(uri);

        if (uri.Scheme == "spotilove" && uri.Host == "auth")
        {
            await SpotifyAuthHandler.HandleSpotifyCallback(uri.ToString());
        }
    }
}