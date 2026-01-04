using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SpotiLove;

public partial class Chats : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl = "https://spotilove-2.onrender.com";
    private ObservableCollection<ChatViewModel> _allChats = new();
    private ObservableCollection<ChatViewModel> _filteredChats = new();

    public Chats()
    {
        InitializeComponent();
        _httpClient = new HttpClient();
        ChatsCollection.ItemsSource = _filteredChats;

        // Add value converters to resources
        Resources.Add("BoolToColorConverter", new BoolToColorConverter());
        Resources.Add("UnreadMessageColorConverter", new UnreadMessageColorConverter());
        Resources.Add("UnreadMessageFontConverter", new UnreadMessageFontConverter());
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadChats();
    }

    private async Task LoadChats()
    {
        try
        {
            if (UserData.Current == null || UserData.Current.Id == Guid.Empty)
            {
                await DisplayAlert("Error", "Please log in first", "OK");
                return;
            }

            Debug.WriteLine($"Loading chats for user: {UserData.Current.Id}");

            // Get user's matches
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/matches/{UserData.Current.Id}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var matchesResponse = System.Text.Json.JsonSerializer.Deserialize<MatchesResponse>(
                    json,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                _allChats.Clear();

                if (matchesResponse?.Matches != null && matchesResponse.Matches.Any())
                {
                    foreach (var match in matchesResponse.Matches)
                    {
                        _allChats.Add(new ChatViewModel
                        {
                            UserId = match.Id,
                            Name = match.Name ?? "Unknown User",
                            ProfileImage = match.Images?.FirstOrDefault() ?? "default_user.png",
                            LastMessage = "You matched! Say hi 👋",
                            TimeStamp = "Now",
                            HasUnread = false,
                            UnreadCount = 0,
                            IsOnline = false
                        });
                    }

                    EmptyStateView.IsVisible = false;
                }
                else
                {
                    EmptyStateView.IsVisible = true;
                }

                UpdateFilteredChats();
            }
            else
            {
                Debug.WriteLine($"Failed to load matches: {response.StatusCode}");
                EmptyStateView.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading chats: {ex.Message}");
            await DisplayAlert("Error", "Failed to load chats. Please try again.", "OK");
        }
    }

    private void UpdateFilteredChats()
    {
        _filteredChats.Clear();
        foreach (var chat in _allChats)
        {
            _filteredChats.Add(chat);
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = e.NewTextValue?.ToLower() ?? "";

        _filteredChats.Clear();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            foreach (var chat in _allChats)
            {
                _filteredChats.Add(chat);
            }
        }
        else
        {
            foreach (var chat in _allChats.Where(c =>
                c.Name.ToLower().Contains(searchText) ||
                c.LastMessage.ToLower().Contains(searchText)))
            {
                _filteredChats.Add(chat);
            }
        }

        EmptyStateView.IsVisible = !_filteredChats.Any();
    }

    private async void OnChatTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is ChatViewModel chat)
        {
            Debug.WriteLine($"Opening chat with: {chat.Name}");

            // Mark as read
            chat.HasUnread = false;
            chat.UnreadCount = 0;

            // Navigate to conversation page
            await Navigation.PushAsync(new Conversation(chat));
        }
    }

    private async void OnNewChatClicked(object sender, EventArgs e)
    {
        // TODO: Implement new chat functionality
        // This could open a page showing all matches
        await DisplayAlert("New Chat", "This feature will let you start a conversation with your matches", "OK");
    }
}

// ===== VIEW MODELS =====
public class ChatViewModel : BindableObject
{
    private Guid _userId;
    private string _name = "";
    private string _profileImage = "";
    private string _lastMessage = "";
    private string _timeStamp = "";
    private bool _hasUnread;
    private int _unreadCount;
    private bool _isOnline;

    public Guid UserId
    {
        get => _userId;
        set { _userId = value; OnPropertyChanged(); }
    }

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string ProfileImage
    {
        get => _profileImage;
        set { _profileImage = value; OnPropertyChanged(); }
    }

    public string LastMessage
    {
        get => _lastMessage;
        set { _lastMessage = value; OnPropertyChanged(); }
    }

    public string TimeStamp
    {
        get => _timeStamp;
        set { _timeStamp = value; OnPropertyChanged(); }
    }

    public bool HasUnread
    {
        get => _hasUnread;
        set { _hasUnread = value; OnPropertyChanged(); }
    }

    public int UnreadCount
    {
        get => _unreadCount;
        set { _unreadCount = value; OnPropertyChanged(); }
    }

    public bool IsOnline
    {
        get => _isOnline;
        set { _isOnline = value; OnPropertyChanged(); }
    }
}

// ===== VALUE CONVERTERS =====
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isOnline && isOnline)
        {
            return Color.FromArgb("#1db954"); // Online = green
        }
        return Colors.Transparent; // Offline = no border
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class UnreadMessageColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool hasUnread && hasUnread)
        {
            return Colors.White; // Unread = white
        }
        return Color.FromArgb("#b3b3b3"); // Read = gray
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class UnreadMessageFontConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool hasUnread && hasUnread)
        {
            return FontAttributes.Bold; // Unread = bold
        }
        return FontAttributes.None; // Read = normal
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

// ===== API RESPONSE MODELS =====
public class MatchesResponse
{
    public List<UserDto>? Matches { get; set; }
    public int Count { get; set; }
    public string? Message { get; set; }
}