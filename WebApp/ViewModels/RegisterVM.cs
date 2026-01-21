using System.ComponentModel.DataAnnotations;
using WebApp.Models;

namespace WebApp.ViewModels
{
    public class RegisterVM
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public required string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The passwords do not match.")]
        public required string ConfirmPassword { get; set; }

        [Required]
        public required string FirstName { get; set; }

        [Required]
        public required string LastName { get; set; }

        [Phone]
        public required string PhoneNumber { get; set; }

        [Required]
        public UserRole Role { get; set; }
    }
}
