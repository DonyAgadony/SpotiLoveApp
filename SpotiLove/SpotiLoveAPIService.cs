using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace SpotiLove
{
    class SpotiLoveAPIService
    {
        static public async Task<List<UserDto>?> GetSwipes(UserDto current)
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri("https://spotilove-2.onrender.com"); // your API base URL

            // Call the endpoint
            var response = await client.GetFromJsonAsync<TakeExUsersResponse>($"/users?userId={current.Id}");
            if (response != null && response.Success)
            {
                Console.WriteLine($"✅ Retrieved {response.Count} users from API:\n");
                foreach (var user in response.Users)
                {
                    Console.WriteLine($"- {user.Name} ({user.Age}, {user.Location})");
                    Console.WriteLine($"  Genres: {user.MusicProfile?.FavoriteGenres}");
                    Console.WriteLine($"  Artists: {user.MusicProfile?.FavoriteArtists}");
                    Console.WriteLine($"  Songs: {user.MusicProfile?.FavoriteSongs}");
                    Console.WriteLine($"  Images: {string.Join(", ", user.Images ?? new())}");
                    Console.WriteLine();
                }
                return response.Users;
            }
            else
            {
                Console.WriteLine("❌ Failed to retrieve users");
            }
            return null;
        }

        static public async Task SendDislike(UserDto current)
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri("https://spotilove-2.onrender.com"); // your API base URL

            // Call the endpoint
            var response = await client.GetFromJsonAsync<ResponseMessage>($"/dislikeUser?id={current.Id}");

            if (response != null && response.Success)
            {
               
            }
            else
            {
                Console.WriteLine("❌ Failed to retrieve users");
            }
        }

    }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public MusicProfile? MusicProfile { get; set; }
    public List<UserImage> Images { get; set; } = new();
    public List<Like> Likes { get; set; } = new();
    public string? Bio { get; set; }
    public string? Location { get; set; }
    public string? Email { get; set; }
}
public class ResponseMessage
{
    public bool Success { get; set; }
}

public class UserImage
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public int UserId { get; set; }
    public User? User { get; set; }
    public string? ImageUrl { get; set; }
}

public class MusicProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FavoriteGenres { get; set; } = "";
    public string FavoriteArtists { get; set; } = "";
    public string FavoriteSongs { get; set; } = "";
    public User? User { get; set; }
}
public class MusicProfileDto
{
    public string? FavoriteSongs { get; set; }
    public string? FavoriteArtists { get; set; }
    public string? FavoriteGenres { get; set; }
}

public class UserImageDto
{
    public string? ImageUrl { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public int Age { get; set; }
    public string? Location { get; set; }
    public string? Bio { get; set; }
    public MusicProfileDto? MusicProfile { get; set; }
    public List<string>? Images { get; set; }
}

public class TakeExUsersResponse
{
    public bool Success { get; set; }
    public int Count { get; set; }
    public List<UserDto> Users { get; set; } = new();
}

public class Like
{
    public int Id { get; set; }
    public int FromUserId { get; set; }
    public int ToUserId { get; set; }
    public bool IsLike { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? FromUser { get; set; }
    public User? ToUser { get; set; }
}

// New Entity for Queue Management
public class UserSuggestionQueue
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int SuggestedUserId { get; set; }
    public int QueuePosition { get; set; }
    public DateTime CreatedAt { get; set; }
    public double CompatibilityScore { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public User SuggestedUser { get; set; } = null!;
}

// DTOs
public record CreateUserDto(string Name, int Age, string Gender, string Genres, string Artists, string Songs = "");
public record UpdateProfileDto(string Genres, string Artists, string Songs = "");
public record LikeDto(int FromUserId, int ToUserId, bool IsLike);
