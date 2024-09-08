using System.ComponentModel.DataAnnotations;

namespace Rsp.IrasPortal.Domain.Entities
{
    public class PlaygroundModel : IValidatableObject
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Enter a short project title.")]
        [StringLength(20, ErrorMessage = "Short project title must be minimum 3 and maximum 20 characters.", MinimumLength = 3)]
        public string ShortProjectTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a IRAS project ID.")]
        [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "IRAS project ID must be 6 digits")]
        public string IrasProjectId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a Chief Investigator (CI).")]
        public string ChiefInvestigator { get; set; } = string.Empty;

        [Required(ErrorMessage = "Select an option for whether the study has been reviwed by a REC.")]
        public string ReviewedByRec { get; set; } = string.Empty;

        public string RecStudyLocation { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ReviewedByRec == "yes" && RecStudyLocation == string.Empty)
            {
                yield return new ValidationResult(
                    $"Select a REC study location.",
                    [nameof(RecStudyLocation)]);
            }
        }
    }
}