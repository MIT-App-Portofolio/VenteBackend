using System.ComponentModel.DataAnnotations;
using Server.Data;

namespace Server.Models;

public class RegisterModel
{
    [Required, EmailAddress]
    public string Email { get; set; }
    [Required, MaxLength(25)]
    public string UserName { get; set; }
    [Required]
    public Gender Gender {get; set; }
    
    [Required, DataType(DataType.Password)]
    public string Password { get; set; }

    [Required] 
    public DateTime BirthDate { get; set; }
}