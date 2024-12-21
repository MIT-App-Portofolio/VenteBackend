using System.ComponentModel.DataAnnotations;

namespace Server.Models;

public class LoginModel
{
    [Required, EmailAddress]
    public string Email { get; set; }
    [Required, DataType(DataType.Password)]
    public string Password { get; set; }
}