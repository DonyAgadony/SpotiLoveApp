using System.Text.Json;

namespace SpotiLove
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new AppShell();
        }

        protected override async void OnStart()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🚀 App OnStart called");

                var userId = await SecureStorage.GetAsync("user_id");
                if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out Guid id))
                {
                    var name = await SecureStorage.GetAsync("user_name");
                    var email = await SecureStorage.GetAsync("user_email");

                    UserData.Current = new UserData
                    {
                        Id = id,
                        Name = name,
                        Email = email
                    };

                    System.Diagnostics.Debug.WriteLine($"✅ Restored user session: Id={id}, Name={name}");

                    // Validate profile with better error handling
                    await ValidateUserProfile(id, name);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ No saved user session found - staying on login page");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ OnStart restore error: {ex.Message}");
            }
        }

        private async Task ValidateUserProfile(Guid userId, string? userName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 Validating profile completeness for user {userId}");

                using var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri("https://spotilove-2.onrender.com");
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var response = await httpClient.GetAsync($"/users/{userId}");

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ User not found in database (Status: {response.StatusCode})");
                    await HandleInvalidUser();
                    return;
                }

                var content = await response.Content.ReadAsStringAsync();
                var userResponse = JsonSerializer.Deserialize<UserProfileResponse>(
                    content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userResponse?.User == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ Failed to deserialize user data");
                    await HandleInvalidUser();
                    return;
                }

                var user = userResponse.User;
                UserData.Current.Age = user.Age;

                bool isProfileIncomplete = user.Age == 0 ||
                                          string.IsNullOrEmpty(user.Gender) ||
                                          string.IsNullOrEmpty(user.SexualOrientation);

                bool isMusicProfileEmpty = user.MusicProfile == null ||
                                           (user.MusicProfile.FavoriteArtists?.Count == 0 &&
                                            user.MusicProfile.FavoriteGenres?.Count == 0 &&
                                            user.MusicProfile.FavoriteSongs?.Count == 0);

                if (isProfileIncomplete)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Basic profile incomplete - navigating to CompleteProfilePage");
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await MainPage.Navigation.PushAsync(
                            new CompleteProfilePage(userId, userName ?? "User")
                        );
                    });
                }
                else if (isMusicProfileEmpty)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Music profile empty - navigating to ArtistSelectionPage");
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await MainPage.DisplayAlert(
                            "Complete Your Profile",
                            "Let's set up your music preferences to find great matches!",
                            "Continue"
                        );
                        await MainPage.Navigation.PushAsync(new ArtistSelectionPage());
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("✅ Profile complete - user can proceed to MainPage");
                }
            }
            catch (TaskCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine($"⏱️ Request timed out: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var retry = await MainPage.DisplayAlert(
                        "Connection Timeout",
                        "The server is taking too long to respond. Retry?",
                        "Retry",
                        "Continue Anyway"
                    );

                    if (retry)
                    {
                        await ValidateUserProfile(userId, userName);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Profile validation error: {ex.Message}");
            }
        }

        private async Task HandleInvalidUser()
        {
            System.Diagnostics.Debug.WriteLine("🔄 Clearing invalid session and returning to login");

            SecureStorage.Remove("user_id");
            SecureStorage.Remove("user_name");
            SecureStorage.Remove("user_email");
            SecureStorage.Remove("auth_token");

            UserData.Current = null;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await MainPage.DisplayAlert(
                    "Session Expired",
                    "Your session has expired. Please log in again.",
                    "OK"
                );
                await Shell.Current.GoToAsync("//Login");
            });
        }

        // ✅ CRITICAL: Handle Deep Links
        protected override async void OnAppLinkRequestReceived(Uri uri)
        {
            base.OnAppLinkRequestReceived(uri);

            System.Diagnostics.Debug.WriteLine($"🔗 Deep link received: {uri}");
            System.Diagnostics.Debug.WriteLine($"   Scheme: {uri.Scheme}");
            System.Diagnostics.Debug.WriteLine($"   Host: {uri.Host}");
            System.Diagnostics.Debug.WriteLine($"   Path: {uri.AbsolutePath}");
            System.Diagnostics.Debug.WriteLine($"   Query: {uri.Query}");

            if (uri.Scheme == "spotilove" && uri.Host == "auth")
            {
                System.Diagnostics.Debug.WriteLine("✅ Valid Spotify callback detected - processing...");

                // Give UI time to be ready
                await Task.Delay(500);

                await SpotifyAuthHandler.HandleSpotifyCallback(uri.ToString());
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Unhandled deep link: {uri.Scheme}://{uri.Host}");
            }
        }

        // DTO for API response
        private class UserProfileResponse
        {
            public UserDto? User { get; set; }
        }
    }
}