using System;
using System.Collections.Generic;

namespace SpotiLove;

// Simple DTOs

public class UserDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public int Age { get; set; }
    public string? Location { get; set; }
    public string? Bio { get; set; }
    public string? Gender { get; set; }
    public string? SexualOrientation { get; set; }
    public MusicProfileDto? MusicProfile { get; set; }
    public List<string>? Images { get; set; }
}

public class MusicProfileDto
{
    public List<string>? FavoriteSongs { get; set; }
    public List<string>? FavoriteArtists { get; set; }
    public List<string>? FavoriteGenres { get; set; }
}

public class TakeExUsersResponse
{
    public bool Success { get; set; }
    public int Count { get; set; }
    public List<UserDto> Users { get; set; } = new();
    public string? Message { get; set; }
}

public class ResponseMessage
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

public class LoginResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Token { get; set; }
    public UserDto? User { get; set; }
}

public class ErrorResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

// Records for API requests
public record LikeDto(Guid FromUserId, Guid ToUserId, bool IsLike);

public record RegisterRequest(
    string Name,
    string Email,
    string Password,
    int Age,
    string Gender,
    string? SexualOrientation = null,
    string? Bio = null,
    string? ProfileImage = null
);

public record LoginRequest(
    string Email,
    string Password,
    bool RememberMe = false
);

public record UpdateMusicProfileRequest(
    string Artists,
    string Songs,
    string Genres
);