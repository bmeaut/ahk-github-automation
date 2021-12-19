using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Ahk.GradeManagement.ResultProcessing.Dto
{
    public class AhkProcessResult
    {
        [JsonPropertyName("gitHubRepoName")]
        [Required]
        [StringLength(maximumLength: 200, MinimumLength = 1)]
        public string GitHubRepoName { get; set; }

        [JsonPropertyName("gitHubBranch")]
        public string GitHubBranch { get; set; }

        [JsonPropertyName("gitHubCommitHash")]
        public string GitHubCommitHash { get; set; }

        [JsonPropertyName("gitHubPullRequestNum")]
        public int? GitHubPullRequestNum { get; set; }

        [JsonPropertyName("neptunCode")]
        [Required]
        [StringLength(maximumLength: 100, MinimumLength = 1)]
        public string NeptunCode { get; set; }

        [JsonPropertyName("result")]
        public AhkTaskResult[] Result { get; set; }

        [JsonPropertyName("origin")]
        public string Origin { get; set; }
    }
}
