using System.ComponentModel.DataAnnotations;

namespace Rsp.IrasPortal.Domain.Entities
{
    public class PlaygroundModel
    {
        public int? Id { get; set; }

        [Required]
        [StringLength(20, ErrorMessage = "Username must be at least 3 and at most 20 characters.", MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress(ErrorMessage = "Email must be in a valid format.")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "The Confirm Password is required.")]
        [Compare("Password", ErrorMessage = "Passwords must match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}