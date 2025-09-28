using System.Threading.Tasks;

namespace SpotiLove;

public partial class MainPage : ContentPage
{
    int count = 0;
    List<UserDto> test = null;
    private readonly Dictionary<string, ImageSource> _imageCache = new Dictionary<string, ImageSource>();

    public MainPage()
    {
        InitializeComponent();
        // Delay initial load to ensure UI is ready
        Loaded += MainPage_Loaded;
    }

    private async void MainPage_Loaded(object sender, EventArgs e)
    {
        await Test();
    }

    // Navigate to SignUp page
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

    // Initialize and load user data from API
    async Task Test()
    {
        try
        {
            test = await SpotiLoveAPIService.GetSwipes();
            if (test != null && test.Count > 0)
            {
                await UpdateUserDisplay(test[0]);

                // Preload next few images in background
                _ = Task.Run(() => PreloadImages());
            }
            else
            {
                // Handle empty user list
                if (NameLabel != null)
                    NameLabel.Text = "No users available";
                if (UserSuggestionImage != null)
                    UserSuggestionImage.Source = "default_user.png";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Test error: {ex.Message}");
            await DisplayAlert("Error", $"Failed to load users: {ex.Message}", "OK");
        }
    }

    // Preload images for better performance
    private async Task PreloadImages()
    {
        try
        {
            if (test == null || test.Count <= 1) return;

            // Preload next 3-5 user images
            var imagesToPreload = test.Skip(1).Take(5);

            var preloadTasks = new List<Task>();

            foreach (var user in imagesToPreload)
            {
                if (user?.Images != null && user.Images.Count > 0)
                {
                    foreach (var imageUrl in user.Images.Take(1)) // Just preload first image per user
                    {
                        if (!string.IsNullOrWhiteSpace(imageUrl))
                        {
                            preloadTasks.Add(PreloadSingleImage(imageUrl));
                        }
                    }
                }
            }

            if (preloadTasks.Count > 0)
            {
                await Task.WhenAll(preloadTasks);
            }
        }
        catch (Exception ex)
        {
            // Silently handle preload errors - don't show to user
            System.Diagnostics.Debug.WriteLine($"Preload error: {ex.Message}");
        }
    }

    // Preload a single image into cache
    private async Task PreloadSingleImage(string imageUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(imageUrl) || _imageCache.ContainsKey(imageUrl))
                return;

            // Create ImageSource and cache it
            var imageSource = ImageSource.FromUri(new Uri(imageUrl));
            _imageCache[imageUrl] = imageSource;

            // Small delay to allow image to start loading
            await Task.Delay(50);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to preload image {imageUrl}: {ex.Message}");
        }
    }

    // Handle Like button click
    private async void LIKE_Clicked(object sender, EventArgs e)
    {
        try
        {
            await LoadNextUser();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Like error: {ex.Message}");
            await DisplayAlert("Error", $"Something went wrong: {ex.Message}", "OK");
        }
    }

    // Handle Dislike button click
    private async void Dislike_Clicked(object sender, EventArgs e)
    {
        try
        {
            await LoadNextUser();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dislike error: {ex.Message}");
            await DisplayAlert("Error", $"Something went wrong: {ex.Message}", "OK");
        }
    }

    // Load next user in the list
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

                // Continue preloading more images in background
                _ = Task.Run(() => PreloadImages());
            }
            else
            {
                // Reload users from API when list is empty
                try
                {
                    test = await SpotiLoveAPIService.GetSwipes();
                    if (test != null && test.Count > 0)
                    {
                        await UpdateUserDisplay(test[0]);

                        // Preload images for new batch
                        _ = Task.Run(() => PreloadImages());
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

    // Update the UI with current user data (optimized)
    private async Task UpdateUserDisplay(UserDto user)
    {
        try
        {
            if (user == null)
            {
                System.Diagnostics.Debug.WriteLine("User is null in UpdateUserDisplay");
                return;
            }

            // Ensure we're on the main thread for UI updates
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                try
                {
                    // Update name label with null check
                    if (NameLabel != null)
                    {
                        NameLabel.Text = !string.IsNullOrEmpty(user.Name) ? user.Name : "Unknown User";
                    }

                    // Update image with caching and null checks
                    if (UserSuggestionImage != null)
                    {
                        if (user.Images != null && user.Images.Count > 0)
                        {
                            var imageUrl = user.Images[0];

                            if (!string.IsNullOrWhiteSpace(imageUrl))
                            {
                                // Check if image is already cached
                                if (_imageCache.ContainsKey(imageUrl))
                                {
                                    UserSuggestionImage.Source = _imageCache[imageUrl];
                                }
                                else
                                {
                                    // Show loading placeholder first
                                    UserSuggestionImage.Source = "placeholder_loading.png";

                                    // Load image and cache it
                                    try
                                    {
                                        var imageSource = ImageSource.FromUri(new Uri(imageUrl));
                                        _imageCache[imageUrl] = imageSource;
                                        UserSuggestionImage.Source = imageSource;
                                    }
                                    catch (UriFormatException)
                                    {
                                        // If URL is invalid, treat as local file
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
                            // Set a default image if no image is available
                            UserSuggestionImage.Source = "default_user.png";
                        }
                    }

                    // You can add more user info updates here if your UserDto has additional properties
                    // For example:
                    // if (!string.IsNullOrEmpty(user.Age)) 
                    //     NameLabel.Text += $", {user.Age}";
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

    // Handle Super Like button (if you want to add this functionality)
    private async void SuperLike_Clicked(object sender, EventArgs e)
    {
        try
        {
            await DisplayAlert("Super Like!", "You super liked this user! ⭐", "Continue");
            await LoadNextUser();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SuperLike error: {ex.Message}");
            await DisplayAlert("Error", $"Something went wrong: {ex.Message}", "OK");
        }
    }
}