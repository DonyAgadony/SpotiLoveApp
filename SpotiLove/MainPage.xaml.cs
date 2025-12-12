using System.Net.Http.Json;
namespace SpotiLove;

public partial class MainPage : ContentPage
{
    List<UserDto>? test = null;
    private readonly Dictionary<string, ImageSource> _imageCache = new Dictionary<string, ImageSource>();

    public MainPage()
    {
        InitializeComponent();
        Loaded += MainPage_Loaded;
    }

    private async void MainPage_Loaded(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("=== MainPage_Loaded Started ===");
        System.Diagnostics.Debug.WriteLine($"UserData.Current is null: {UserData.Current == null}");

        if (UserData.Current != null)
        {
            System.Diagnostics.Debug.WriteLine($"UserData.Current.Id: {UserData.Current.Id}");
            System.Diagnostics.Debug.WriteLine($"UserData.Current.Name: {UserData.Current.Name}");

            if (UserData.Current.Id == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ UserData.Current.Id is 0 or negative");
                await DisplayAlert("Error", "Invalid user ID. Please log in again.", "OK");
                if (Shell.Current != null)
                    await Shell.Current.GoToAsync("//Login");
                else
                    await Application.Current.MainPage.Navigation.PushAsync(new Login());
            }
            else
            {
                await Test(UserData.Current.ToDto());
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("❌ UserData.Current is null");
            await DisplayAlert("Error", "User data not found. Please log in again.", "OK");
            if (Shell.Current != null)
                await Shell.Current.GoToAsync("//Login");
            else
                await Application.Current.MainPage.Navigation.PushAsync(new Login());
        }
    }

    async Task Test(UserDto currentDTO)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"=== Test method called with userId: {currentDTO?.Id} ===");

            if (currentDTO == null || currentDTO.Id == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ Invalid currentDTO");
                await DisplayAlert("Error", "Invalid user data", "OK");
                if (Shell.Current != null)
                    await Shell.Current.GoToAsync("//Login");
                else
                    await Application.Current.MainPage.Navigation.PushAsync(new Login());
                return;
            }

            System.Diagnostics.Debug.WriteLine($"🔄 Calling SpotiLoveAPIService.GetSwipes...");
            test = await SpotiLoveAPIService.GetSwipes(currentDTO);

            System.Diagnostics.Debug.WriteLine($"📊 GetSwipes returned: {(test == null ? "null" : $"{test.Count} users")}");

            if (test != null && test.Count > 0){
                System.Diagnostics.Debug.WriteLine($"✅ Found {test.Count} users, displaying first user");
                await UpdateUserDisplay(test[0]);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("⚠️ No users returned from API");

                if (NameLabel != null)
                    NameLabel.Text = "No users available";
                if (UserSuggestionImage != null)
                    UserSuggestionImage.Source = "default_user.png";

                await DisplayAlert("No Users", "No users available at the moment. Please try again later.", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Test error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            await DisplayAlert("Error", $"Failed to load users: {ex.Message}", "OK");
        }
    }

    private async void OnGoToSignUp(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("//SignUp");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
        }
    }

    private async void OnDisconnect(object sender, EventArgs e)
    {
        try
        {
            bool confirm = await DisplayAlert(
                "Disconnect",
                "Are you sure you want to log out?",
                "Yes",
                "Cancel"
            );

            if (confirm)
            {
                UserData.Current = null;
                _imageCache.Clear();
                test = null;
                await Navigation.PushAsync(new Login());
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Disconnect error: {ex.Message}");
            await DisplayAlert("Error", "Failed to disconnect. Please try again.", "OK");
        }
    }

    private async void LIKE_Clicked(object sender, EventArgs e)
    {
        if (test != null && test.Count > 0)
        {
            var current = UserData.Current.Id;
            var target = test[0].Id;

            using var client = new HttpClient { BaseAddress = new Uri("https://spotilove-2.onrender.com/") };
            var response = await client.PostAsJsonAsync("/swipe", new LikeDto(current, target, true));
            var result = await response.Content.ReadFromJsonAsync<ResponseMessage>();

            if (result != null && result.Success)
            {
                await LoadNextUser();
            }
            else
            {
                await DisplayAlert("Error", "Failed to like user", "OK");
            }
        }
    }

    private async void Dislike_Clicked(object sender, EventArgs e)
    {
        if (test != null && test.Count > 0)
        {
            var current = UserData.Current.Id;
            var target = test[0].Id;

            using var client = new HttpClient { BaseAddress = new Uri("https://spotilove-2.onrender.com/") };
            var response = await client.PostAsJsonAsync("/swipe", new LikeDto(current, target, false));
            var result = await response.Content.ReadFromJsonAsync<ResponseMessage>();

            if (result != null && result.Success)
            {
                await LoadNextUser();
            }
            else
            {
                await DisplayAlert("Error", "Failed to dislike user", "OK");
            }
        }
    }

    private async Task LoadNextUser()
    {
        try
        {
            if (test != null && test.Count > 0)
            {
                test.RemoveAt(0);
            }

            if (test != null && test.Count > 0)
            {
                await UpdateUserDisplay(test[0]);
            }
            else
            {
                try
                {
                    UserDto currentDTO = new UserDto();
                    currentDTO.Id = UserData.Current.Id;
                    test = await SpotiLoveAPIService.GetSwipes(currentDTO);

                    if (test != null && test.Count > 0)
                    {
                        await UpdateUserDisplay(test[0]);
                    }
                    else
                    {
                        await DisplayAlert("No More Users", "No more users available right now. Check back later!", "OK");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Reload error: {ex.Message}");
                    await DisplayAlert("Error", $"Failed to load more users: {ex.Message}", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadNextUser error: {ex.Message}");
        }
    }

    private async Task UpdateUserDisplay(UserDto user)
    {
        try
        {
            if (user == null)
            {
                System.Diagnostics.Debug.WriteLine("User is null in UpdateUserDisplay");
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                try
                {
                    // Update name and age
                    if (NameLabel != null)
                    {
                        string displayName = !string.IsNullOrEmpty(user.Name) ? user.Name : "Unknown User";
                        if (user.Age > 0)
                        {
                            displayName += $", {user.Age}";
                        }
                        NameLabel.Text = displayName;
                    }

                    // Update location
                    if (LocationLabel != null)
                    {
                        LocationLabel.Text = !string.IsNullOrEmpty(user.Location) ? user.Location : "Location unknown";
                    }

                    // Update bio
                    if (BioLabel != null && BioContainer != null)
                    {
                        if (!string.IsNullOrWhiteSpace(user.Bio))
                        {
                            BioLabel.Text = user.Bio;
                            BioContainer.IsVisible = true;
                        }
                        else
                        {
                            BioLabel.Text = "No bio available";
                            BioContainer.IsVisible = false;
                        }
                    }

                    // Update genre tags
                    if (GenreTagsContainer != null)
                    {
                        GenreTagsContainer.Clear();

                        if (user.MusicProfile != null && user.MusicProfile.FavoriteGenres != null && user.MusicProfile.FavoriteGenres.Count > 0)
                        {
                            var genres = user.MusicProfile.FavoriteGenres
                                .Where(g => !string.IsNullOrWhiteSpace(g))
                                .Select(g => g.Trim())
                                .Take(3)
                                .ToList();

                            foreach (var genre in genres)
                            {
                                var frame = new Frame
                                {
                                    BackgroundColor = genres.IndexOf(genre) == 0
                                        ? Color.FromArgb("#1db954")
                                        : Color.FromArgb("#535353"),
                                    Padding = new Thickness(6, 3),
                                    CornerRadius = 10,
                                    Margin = new Thickness(2),
                                    HasShadow = false
                                };

                                var label = new Label
                                {
                                    Text = genre,
                                    FontSize = 11,
                                    TextColor = Colors.White
                                };

                                frame.Content = label;
                                GenreTagsContainer.Add(frame);
                            }
                        }
                    }

                    // Update profile image
                    if (UserSuggestionImage != null)
                    {
                        if (user.Images != null && user.Images.Count > 0)
                        {
                            var imageUrl = user.Images[0];

                            if (!string.IsNullOrWhiteSpace(imageUrl))
                            {
                                if (_imageCache.ContainsKey(imageUrl))
                                {
                                    UserSuggestionImage.Source = _imageCache[imageUrl];
                                }
                                else
                                {
                                    UserSuggestionImage.Source = "placeholder_loading.png";

                                    try
                                    {
                                        var imageSource = ImageSource.FromUri(new Uri(imageUrl));
                                        _imageCache[imageUrl] = imageSource;
                                        UserSuggestionImage.Source = imageSource;
                                    }
                                    catch (UriFormatException)
                                    {
                                        UserSuggestionImage.Source = imageUrl;
                                    }
                                }
                            }
                            else
                            {
                                UserSuggestionImage.Source = "default_user.png";
                            }
                        }
                        else
                        {
                            UserSuggestionImage.Source = "default_user.png";
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UI Update error: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateUserDisplay error: {ex.Message}");
        }
    }
}