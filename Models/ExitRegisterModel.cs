using System.ComponentModel.DataAnnotations;

namespace Server.Models;

public class ExitRegisterModel
{
    [MaxLength(20)]
    public string? Name { get; set; }
    [Required]
    public string LocationId { get; set; }
    [Required]
    public List<DateTimeOffset> Dates { get; set; }
    public bool? NoTzTransform { get; set; }
}