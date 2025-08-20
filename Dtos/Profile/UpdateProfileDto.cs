using System;

namespace BackEnd.Dtos.Profile
{
    public class UpdateProfileDto
    {
        public string Bio { get; set; }
        public string FaceBook { get; set; }
        public string Instgram { get; set; }
        public string Behance { get; set; }
        public string GitHub { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string UserTitle { get; set; }
        public string PhoneNumber { get; set; }
        public string CVUrl { get; set; }
        // Password fields
        public string CurrentPassword { get; set; } // Required for password changes
        public string NewPassword { get; set; }     // New password if changing

    }
}