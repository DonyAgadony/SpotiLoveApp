using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SpotiLove.Models
{
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
}
