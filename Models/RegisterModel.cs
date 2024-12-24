using System.ComponentModel.DataAnnotations;

namespace Server.Models;

public class RegisterModel
{
    [Required, EmailAddress]
    public string Email { get; set; }
    [Required, MaxLength(25)]
    public string UserName { get; set; }
    
    [Required, DataType(DataType.Password)]
    public string Password { get; set; }
}