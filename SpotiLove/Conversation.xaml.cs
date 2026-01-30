using Microsoft.Maui.Controls.Shapes;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SpotiLove;

public partial class Conversation : ContentPage
{
    private readonly ChatViewModel _chat;
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl = "https://spotilove.danielnaz.com";
    private ObservableCollection<MessageViewModel> _messages = new();

    public Conversation(ChatViewModel chat)
    {
        InitializeComponent();
        _chat = chat;
        _httpClient = new HttpClient();

        // Set header info
        UserNameLabel.Text = chat.Name;
        ProfileImage.Source = chat.ProfileImage;
        StatusLabel.Text = chat.IsOnline ? "Online" : "Last seen recently";
        StatusLabel.TextColor = chat.IsOnline
            ? Color.FromArgb("#1db954")
            : Color.FromArgb("#888888");

        LoadMessages();
    }

    private async void LoadMessages()
    {
        try
        {
            // TODO: Load messages from API
            // For now, we'll clear the example messages and show empty state
            MessagesContainer.Clear();

            // Add date separator
            AddDateSeparator("Today");

            // TODO: Replace with actual API call
            // Example: var response = await _httpClient.GetAsync($"{_apiBaseUrl}/messages/{UserData.Current.Id}/{_chat.UserId}");

            Debug.WriteLine($"Loading messages with: {_chat.Name}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading messages: {ex.Message}");
            await DisplayAlert("Error", "Failed to load messages", "OK");
        }
    }

    private void AddDateSeparator(string dateText)
    {
        var separator = new Label
        {
            Text = dateText,
            FontSize = 12,
            TextColor = Color.FromArgb("#888888"),
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 10)
        };

        MessagesContainer.Add(separator);
    }

    private void AddIncomingMessage(string message, string time, string senderName)
    {
        var messageLayout = new HorizontalStackLayout
        {
            HorizontalOptions = LayoutOptions.Start,
            Spacing = 10,
            Margin = new Thickness(0, 5)
        };

        var messageBorder = new Border
        {
            BackgroundColor = Color.FromArgb("#212121"),
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(15, 15, 15, 0) },
            Padding = new Thickness(12, 8),
            MaximumWidthRequest = 280
        };

        var contentStack = new VerticalStackLayout { Spacing = 4 };

        var messageLabel = new Label
        {
            Text = message,
            FontSize = 14,
            TextColor = Colors.White,
            LineBreakMode = LineBreakMode.WordWrap
        };

        var timeLabel = new Label
        {
            Text = time,
            FontSize = 10,
            TextColor = Color.FromArgb("#888888"),
            HorizontalOptions = LayoutOptions.End
        };

        contentStack.Add(messageLabel);
        contentStack.Add(timeLabel);
        messageBorder.Content = contentStack;
        messageLayout.Add(messageBorder);

        MessagesContainer.Add(messageLayout);

        // Scroll to bottom
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(100);
            await MessagesScrollView.ScrollToAsync(0, MessagesContainer.Height, true);
        });
    }

    private void AddOutgoingMessage(string message, string time, bool isRead = true)
    {
        var messageLayout = new HorizontalStackLayout
        {
            HorizontalOptions = LayoutOptions.End,
            Spacing = 10,
            Margin = new Thickness(0, 5)
        };

        var messageBorder = new Border
        {
            BackgroundColor = Color.FromArgb("#1db954"),
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(15, 15, 0, 15) },
            Padding = new Thickness(12, 8),
            MaximumWidthRequest = 280
        };

        var contentStack = new VerticalStackLayout { Spacing = 4 };

        var messageLabel = new Label
        {
            Text = message,
            FontSize = 14,
            TextColor = Colors.White,
            LineBreakMode = LineBreakMode.WordWrap
        };

        var bottomStack = new HorizontalStackLayout
        {
            HorizontalOptions = LayoutOptions.End,
            Spacing = 5
        };

        var timeLabel = new Label
        {
            Text = time,
            FontSize = 10,
            TextColor = Color.FromArgb("#f0f0f0")
        };

        var readLabel = new Label
        {
            Text = isRead ? "✓✓" : "✓",
            FontSize = 12,
            TextColor = Color.FromArgb("#f0f0f0")
        };

        bottomStack.Add(timeLabel);
        bottomStack.Add(readLabel);

        contentStack.Add(messageLabel);
        contentStack.Add(bottomStack);
        messageBorder.Content = contentStack;
        messageLayout.Add(messageBorder);

        MessagesContainer.Add(messageLayout);

        // Scroll to bottom
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(100);
            await MessagesScrollView.ScrollToAsync(0, MessagesContainer.Height, true);
        });
    }

    private async void OnSendMessageClicked(object sender, EventArgs e)
    {
        var message = MessageEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        try
        {
            var currentTime = DateTime.Now.ToString("h:mm tt");

            // Add message to UI immediately
            AddOutgoingMessage(message, currentTime, false);

            // Clear input
            MessageEntry.Text = string.Empty;

            // TODO: Send message to API
            // var response = await _httpClient.PostAsync($"{_apiBaseUrl}/messages", content);

            Debug.WriteLine($"Sending message: {message} to {_chat.Name}");

            // Simulate message sent
            await Task.Delay(500);

            // Update last message (mark as read)
            // In production, this would be done after receiving server confirmation
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error sending message: {ex.Message}");
            await DisplayAlert("Error", "Failed to send message", "OK");
        }
    }

    private async void OnShareMusicClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Share Music", "This feature will let you search and share songs from Spotify", "OK");
    }

    private async void OnAttachClicked(object sender, EventArgs e)
    {
        try
        {
            var action = await DisplayActionSheet(
                "Share",
                "Cancel",
                null,
                "📷 Photo",
                "🎵 Music",
                "📍 Location"
            );

            Debug.WriteLine($"Selected action: {action}");

            // TODO: Implement attachment functionality based on selection
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error with attachment: {ex.Message}");
        }
    }

    private async void OnMoreOptionsClicked(object sender, EventArgs e)
    {
        try
        {
            var action = await DisplayActionSheet(
                "Options",
                "Cancel",
                null,
                "View Profile",
                "Mute Notifications",
                "Block User"
            );

            Debug.WriteLine($"Selected option: {action}");

            // TODO: Implement options functionality
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error with options: {ex.Message}");
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}