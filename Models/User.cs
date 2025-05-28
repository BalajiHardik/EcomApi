namespace EcomApi.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "User";  // "User" or "Admin"
    public bool IsBlocked { get; set; } = false;

    // Optional profile info
    public string? Phone { get; set; }
    public string? Address { get; set; }
}
