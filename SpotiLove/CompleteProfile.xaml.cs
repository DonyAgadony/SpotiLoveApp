using System.Text;
using System.Text.Json;

namespace SpotiLove;

public partial class CompleteProfilePage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl = "https://spotilove-2.onrender.com";
    private Guid _userId;
    private string _userName;

    public CompleteProfilePage()
    {
        InitializeComponent();
        _httpClient = new HttpClient();
        _userId = Guid.Empty;
        _userName = "User";
    }

    public CompleteProfilePage(Guid userId, string userName) : this()
    {
        _userId = userId;
        _userName = userName;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // If no userId was provided via constructor, try to get from UserData
        if (_userId == Guid.Empty && UserData.Current != null)
        {
            _userId = UserData.Current.Id;
            _userName = UserData.Current.Name ?? "User";
        }
    }

    private void OnBioTextChanged(object sender, TextChangedEventArgs e)
    {
        var length = BioEditor.Text?.Length ?? 0;
        BioCharCountLabel.Text = $"{length}/500 characters";
    }

    private async void OnContinueClicked(object sender, EventArgs e)
    {
        // Validate Age
        if (string.IsNullOrWhiteSpace(AgeEntry.Text))
        {
            await DisplayAlert("Required Field", "Please enter your age", "OK");
            return;
        }

        if (!int.TryParse(AgeEntry.Text, out int age) || age < 18 || age > 120)
        {
            await DisplayAlert("Invalid Age", "Please enter a valid age between 18 and 120", "OK");
            return;
        }

        // Validate Gender
        if (GenderPicker.SelectedIndex == -1)
        {
            await DisplayAlert("Required Field", "Please select your gender", "OK");
            return;
        }

        // Validate Sexual Orientation
        if (SexualOrientationPicker.SelectedIndex == -1)
        {
            await DisplayAlert("Required Field", "Please select who you're interested in", "OK");
            return;
        }

        try
        {
            ContinueButton.IsEnabled = false;
            ContinueButton.Text = "Updating profile...";

            // Map picker selections to API values
            var sexualOrientation = SexualOrientationPicker.SelectedItem.ToString() switch
            {
                "Men" => "Male",
                "Women" => "Female",
                "Everyone" => "Both",
                _ => "Both"
            };

            // Update user profile via API
            var profileUpdate = new
            {
                age = age,
                gender = GenderPicker.SelectedItem.ToString(),
                sexualOrientation = sexualOrientation,
                bio = string.IsNullOrWhiteSpace(BioEditor.Text) ? null : BioEditor.Text.Trim()
            };

            var content = new StringContent(
                JsonSerializer.Serialize(profileUpdate),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PutAsync(
                $"{_apiBaseUrl}/users/{_userId}/basic-profile",
                content
            );

            if (response.IsSuccessStatusCode)
            {
                // Update local UserData
                UserData.Current.Age = age;

                // Navigate to Artist Selection
                await DisplayAlert("Success", "Profile updated! Let's find your music taste.", "Continue");
                await Navigation.PushAsync(new ArtistSelectionPage());
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Error", $"Failed to update profile: {error}", "OK");
                ContinueButton.IsEnabled = true;
                ContinueButton.Text = "Continue to Music Selection";
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            ContinueButton.IsEnabled = true;
            ContinueButton.Text = "Continue to Music Selection";
        }
    }

    private async void OnSkipClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlert(
            "Skip Profile Setup?",
            "You can complete your profile later in settings, but it may affect match quality.",
            "Skip Anyway",
            "Go Back"
        );

        if (confirm)
        {
            await Navigation.PushAsync(new ArtistSelectionPage());
        }
    }
}