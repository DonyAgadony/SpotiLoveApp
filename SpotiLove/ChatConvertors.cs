using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace SpotiLove;

// ===== VALUE CONVERTERS =====
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isOnline && isOnline)
        {
            return Color.FromArgb("#1db954"); // Online = green
        }
        return Colors.Transparent; // Offline = no border
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class UnreadMessageColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool hasUnread && hasUnread)
        {
            return Colors.White; // Unread = white
        }
        return Color.FromArgb("#b3b3b3"); // Read = gray
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class UnreadMessageFontConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool hasUnread && hasUnread)
        {
            return FontAttributes.Bold; // Unread = bold
        }
        return FontAttributes.None; // Read = normal
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}