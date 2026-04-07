using System.ComponentModel.DataAnnotations;

namespace Wander.Api.Models.Auth;

public record RegisterRequest(
    [Required][MinLength(3)][MaxLength(50)] string Username,
    [Required][EmailAddress][MaxLength(256)] string Email,
    [Required][MinLength(8)][MaxLength(100)] string Password);

public record LoginRequest(
    [Required][EmailAddress][MaxLength(256)] string Email,
    [Required][MaxLength(100)] string Password);

public record RefreshTokenRequest([Required][MaxLength(500)] string RefreshToken);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt);
