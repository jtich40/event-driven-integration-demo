using System.ComponentModel.DataAnnotations;

namespace IntegrationApi.Models;

public class CreateUserDto
{
    // this can be optional because dynamoDB will auto-generate IDs
    public string? Id { get; set; }
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = null!;
    [Required(ErrorMessage = "Email is required")]
    public string Email { get; set; } = null!;
}

public class User
{
    // non-nullable for DB
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
}