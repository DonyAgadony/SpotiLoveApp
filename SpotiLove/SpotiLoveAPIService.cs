using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SpotiLove
{
    class SpotiLoveAPIService
    {
        static public async Task<List<UserDto>?> GetSwipes(UserDto current)
        {
            try
            {
                Debug.WriteLine($"=== GetSwipes called ===");
                Debug.WriteLine($"Current user ID: {current?.Id}");
                Debug.WriteLine($"Current user Name: {current?.Name}");
                Debug.WriteLine($"Current user Email: {current?.Email}");

                // ✅ FIX 1: Better validation
                if (current == null)
                {
                    Debug.WriteLine($"❌ Current user is null!");
                    return null;
                }

                // ✅ FIX 2: Check if user has MusicProfile
                if (current.MusicProfile == null)
                {
                    Debug.WriteLine($"⚠️ Warning: User {current.Id} has no MusicProfile");
                    Debug.WriteLine($"⚠️ This will cause the API to reject the request");
                }

                using var client = new HttpClient();
                client.BaseAddress = new Uri("https://spotilove-2.onrender.com");
                client.Timeout = TimeSpan.FromSeconds(30);

                var url = $"users?userId={current.Id}&count=10";
                Debug.WriteLine($"🌐 Making request to: {client.BaseAddress}{url}");

                var response = await client.GetAsync(url);
                Debug.WriteLine($"📡 Response Status: {response.StatusCode}");

                var responseBody = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"📄 Response Body Length: {responseBody?.Length ?? 0}");

                // ✅ FIX 4: Show full response for debugging
                if (responseBody != null && responseBody.Length < 500)
                {
                    Debug.WriteLine($"📄 Full Response: {responseBody}");
                }
                else
                {
                    Debug.WriteLine($"📄 Response Preview: {responseBody?.Substring(0, Math.Min(500, responseBody?.Length ?? 0))}...");
                }

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"❌ API Error: {response.StatusCode}");
                    Debug.WriteLine($"❌ Full Error Response: {responseBody}");
                    return null;
                }

                var result = System.Text.Json.JsonSerializer.Deserialize<TakeExUsersResponse>(
                    responseBody,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (result == null)
                {
                    Debug.WriteLine("❌ Deserialization returned null");
                    return null;
                }

                Debug.WriteLine($"✅ Success: {result.Success}");
                Debug.WriteLine($"   Count: {result.Count}");
                Debug.WriteLine($"   Message: {result.Message}");

                if (result.Success && result.Users != null && result.Users.Count > 0)
                {
                    Debug.WriteLine($"✅ Retrieved {result.Users.Count} users");
                    foreach (var user in result.Users.Take(3))
                    {
                        Debug.WriteLine($"   - User {user.Id}: {user.Name}, Age {user.Age}");
                    }
                    return result.Users;
                }
                else
                {
                    Debug.WriteLine($"⚠️ {result.Message}");
                    return new List<UserDto>(); // Return empty list instead of null
                }
            }
            catch (TaskCanceledException ex)
            {
                Debug.WriteLine($"❌ Request timeout: {ex.Message}");
                return null;
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"❌ Network error: {ex.Message}");
                return null;
            }
            catch (System.Text.Json.JsonException ex)
            {
                Debug.WriteLine($"❌ JSON parsing error: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error: {ex.Message}");
                Debug.WriteLine($"Stack: {ex.StackTrace}");
                return null;
            }
        }

        static public async Task SendDislike(UserDto current)
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri("https://spotilove-2.onrender.com");

            var response = await client.GetFromJsonAsync<ResponseMessage>($"/dislikeUser?id={current.Id}");

            if (response != null && response.Success)
            {
                Debug.WriteLine("✅ Dislike sent successfully");
            }
            else
            {
                Debug.WriteLine("❌ Failed to send dislike");
            }
        }

        // ✅ NEW: Helper method to verify user exists and has profile
        static public async Task<bool> VerifyUserExists(int userId)
        {
            try
            {
                using var client = new HttpClient();
                client.BaseAddress = new Uri("https://spotilove-2.onrender.com");
                client.Timeout = TimeSpan.FromSeconds(10);

                var response = await client.GetAsync($"/debug/user/{userId}");
                var result = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"🔍 User verification for ID {userId}:");
                Debug.WriteLine($"   Status: {response.StatusCode}");
                Debug.WriteLine($"   Response: {result}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error verifying user: {ex.Message}");
                return false;
            }
        }
    }
}

// Keep all your existing classes below...
public class User
{
    // Primary Key
    [Key]
    public int Id { get; set; }

    // Core Profile Data
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(18, 120)]
    public int Age { get; set; }

    [Required, MaxLength(20)]
    public string Gender { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Bio { get; set; }

    [MaxLength(100)]
    public string? Location { get; set; }

    // Authentication & Auditing
    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [JsonIgnore] // Typically ignore the hash for API responses
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // --- Navigation Properties ---

    // 1-1 relationship with MusicProfile
    public MusicProfile? MusicProfile { get; set; }

    // 1-Many relationship with UserImage
    public List<UserImage> Images { get; set; } = new();

    // Many-to-Many relationship via Like (Outgoing swipes/likes INITIATED by this user)
    // Used by AppDbContext for Likes.FromUser relationship
    public List<Like> Likes { get; set; } = new();

    // Many-to-Many relationship via Like (Incoming swipes/likes RECEIVED by this user)
    // Used by AppDbContext for Likes.ToUser relationship
    public List<Like> LikesReceived { get; set; } = new();

    // 1-Many relationship with UserSuggestionQueue (This user's queue of potential matches)
    // Used by AppDbContext for USQ.User relationship
    public List<UserSuggestionQueue> Suggestions { get; set; } = new();
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
    public Guid Id { get; set; }
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
    public string? Message { get; set; }
}

public class Like
{
    public int Id { get; set; }
    public int FromUserId { get; set; }
    public int ToUserId { get; set; }
    public bool IsLike { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? FromUser { get; set; }
    public User? ToUser { get; set; }
}

public class UserSuggestionQueue
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int SuggestedUserId { get; set; }
    public int QueuePosition { get; set; }
    public DateTime CreatedAt { get; set; }
    public double CompatibilityScore { get; set; }

    public User User { get; set; } = null!;
    public User SuggestedUser { get; set; } = null!;
}

public record CreateUserDto(string Name, int Age, string Gender, string Genres, string Artists, string Songs = "");
public record UpdateProfileDto(string Genres, string Artists, string Songs = "");
public record LikeDto(Guid FromUserId, Guid ToUserId, bool IsLike);