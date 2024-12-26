using System.ComponentModel.DataAnnotations;

namespace Server.Models;

public class AdminLoginModel
{
    [Required, DataType(DataType.Password)]
    public string Password { get; set; }
}