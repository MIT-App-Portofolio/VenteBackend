using System.ComponentModel.DataAnnotations;
using Server.Data;

namespace Server.Models;

public class GoogleRegister
{
    [Required]
    public string Id { get; set; }
    [MaxLength(20), Required]
    public string UserName { get; set; }
    [Required]
    public Gender Gender { get; set; }
    public DateTime? BirthDate { get; set; }
}