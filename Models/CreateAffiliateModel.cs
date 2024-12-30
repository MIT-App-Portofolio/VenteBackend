using System.ComponentModel.DataAnnotations;
using Server.Data;

namespace Server.Models
{
    public class CreateAffiliateModel
    {
        [Required]
        public string UserName { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; }
        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public string EventPlaceName { get; set; }
        [Required]
        public Location EventPlaceLocation { get; set; }
        [Required]
        public string EventPlaceDescription { get; set; }
        [Required]
        public int EventPlacePriceRangeBegin { get; set; }
        [Required]
        public int EventPlacePriceRangeEnd { get; set; }
    }
}