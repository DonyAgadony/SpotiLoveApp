using System.Web;
using System.Diagnostics;

namespace SpotiLove;

public class SpotifyAuthHandler
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
            httpClient.BaseAddress = new Uri("https://spotilove-2.onrender.com");
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            Debug.WriteLine($"🌐 Fetching user profile for ID: {userId}");
            var response = await httpClient.GetAsync($"/users/{userId}");
            Debug.WriteLine($"📡 API Response: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"📄 Response length: {content.Length} characters");

                var userResponse = System.Text.Json.JsonSerializer.Deserialize<ApiUserResponse>(
                    content,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

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

                    // Check if profile is complete (Age > 0 means they filled it out)
                    if (isNewUser || userResponse.User.Age == 0 || string.IsNullOrEmpty(userResponse.User.Gender))
                    {
                        // Profile incomplete - navigate to profile completion
                        await Shell.Current.DisplayAlert(
                            "Welcome to SpotiLove!",
                            $"Hi {userResponse.User.Name}! Let's set up your profile.",
                            "Let's Go"
                        );

                        Debug.WriteLine("🚀 Navigating to CompleteProfilePage...");
                        await Shell.Current.Navigation.PushAsync(
                            new CompleteProfilePage(userResponse.User.Id, userResponse.User.Name ?? "")
                        );
                    }
                    else
                    {
                        // Profile complete - navigate to main page
                        string message = $"Welcome back, {userResponse.User.Name}! Your music profile has been updated.";
                        await Shell.Current.DisplayAlert("Success", message, "OK");

                        Debug.WriteLine("🚀 Navigating to MainPage...");
                        await Shell.Current.GoToAsync("//MainPage");
                    }
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

    private class ApiUserResponse
    {
        public UserDto? User { get; set; }
    }
}