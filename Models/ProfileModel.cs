using System.ComponentModel.DataAnnotations;

namespace Server.Models;

public class ProfileModel
{
    [MaxLength(30)]
    public string IgHandle { get; set; }
    [MaxLength(35)]
    public string Name { get; set; }
    [MaxLength(200)]
    public string Description { get; set; }
}