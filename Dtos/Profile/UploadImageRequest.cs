using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace BackEnd.Dtos.Profile
{
    public class UploadImageRequest
    {
        [Required]
        public IFormFile Image { get; set; } = default!;

        // Optional fields that might be needed
        public string? Caption { get; set; }
        public string? UserId { get; set; }
    }
}
