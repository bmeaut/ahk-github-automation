using GradeManagement.Data;
using GradeManagement.Data.Models;
using GradeManagement.Data.Utils;
using GradeManagement.Shared.Dtos.Moodle;
using GradeManagement.Shared.Exceptions;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text;

using JsonSerializer = System.Text.Json.JsonSerializer;

namespace GradeManagement.Bll.Services.Moodle;

public class MoodleIntegrationService(
    TokenGeneratorService tokenGeneratorService,
    StudentService studentService,
    ExerciseService exerciseService,
    GradeManagementDbContext dbContext)
{
    private static Dictionary<string, (string, string)> States { get; } = new();

    public async Task UploadScore(Score score)
    {
        if (score.SyncedToMoodle || !score.IsApproved) return;
        var exercise = score.PullRequest.Assignment.Exercise;
        var student = score.PullRequest.Assignment.Student;
        var scoreType = score.ScoreType;
        if (student.MoodleId == null) throw new MoodleSyncException("No moodle ID for student");

        var token = tokenGeneratorService.GenerateAccessToken();

        if (token == null) throw new MoodleSyncException("Token was null when trying to register score");
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var scoreToUpload = new LtiScore()
        {
            Timestamp = DateTimeOffset.Now.ToString("O"),
            ScoreGiven = Convert.ToInt32(score.Value),
            UserId = student.MoodleId,
            ActivityProgress = "Completed",
            GradingProgress = "FullyGraded"
        };

        var label = exercise.MoodleScoreNamePrefix + scoreType.Type;

        var lineItem = GetOrCreateLineItemByLabel(label, token, exercise);

        var scoreUrl = string.Concat(lineItem.Id.AsSpan(0, lineItem.Id.LastIndexOf("?")), "/scores");

        var scoreJson = JsonSerializer.Serialize(scoreToUpload);
        var response = client.PostAsync(
                scoreUrl,
                new StringContent(scoreJson, Encoding.UTF8,
                    "application/vnd.ims.lis.v1.score+json"))
            .GetAwaiter()
            .GetResult();
        if (!response.IsSuccessStatusCode)
        {
            throw new MoodleSyncException("Score creation failed with status code: " + response.StatusCode +
                                          " Reason phrase: " + response.ReasonPhrase);
        }

        score.SyncedToMoodle = true;
        await dbContext.SaveChangesAsync();
    }

    private static LtiLineItem GetOrCreateLineItemByLabel(string label, string token, Exercise exercise)
    {
        var lineItems = GetAllLineItems(token, exercise);
        var lineItem = lineItems.FirstOrDefault(li => li.Label == label);
        if (lineItem == null)
        {
            lineItem = CreateLineItem(
                new LtiLineItemRequest()
                {
                    StartDateTime = DateTime.Now,
                    EndDateTime = DateTime.Now.AddDays(10),
                    Label = label,
                    ScoreMaximum = 100
                }, token, exercise);
        }

        return lineItem;
    }

    private static List<LtiLineItem> GetAllLineItems(string token, Exercise exercise)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.ims.lis.v2.lineitemcontainer+json"));

        var response = client.GetAsync(exercise.MoodleScoreUrl)
            .GetAwaiter()
            .GetResult();
        if (!response.IsSuccessStatusCode)
            throw new MoodleSyncException("Line item request failed with status code: " + response.StatusCode);
        return JsonSerializer.Deserialize<List<LtiLineItem>>(response.Content.ReadAsStream());
    }

    private static LtiLineItem CreateLineItem(LtiLineItemRequest lineItem, string token, Exercise exercise)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.ims.lis.v2.lineitem+json"));

        var lineItemJSON = JsonSerializer.Serialize(lineItem);
        var response = client.PostAsync(
                exercise.MoodleScoreUrl,
                new StringContent(lineItemJSON, Encoding.UTF8,
                    "application/vnd.ims.lis.v2.lineitem+json"))
            .GetAwaiter()
            .GetResult();
        if (!response.IsSuccessStatusCode)
            throw new MoodleSyncException("Line item creation failed with status code: " + response.StatusCode);
        return JsonSerializer.Deserialize<LtiLineItem>(response.Content.ReadAsStream());
    }

    public string HandleOidc(IFormCollection formData)
    {
        var state = Guid.NewGuid().ToString();
        var nonce = Guid.NewGuid().ToString();
        States.Add(state, (formData["client_id"], nonce));

        return "https://edu.vik.bme.hu/mod/lti/auth.php" + "?" +
               "scope=openid&" +
               "response_type=id_token&" +
               "client_id=" + formData["client_id"] + "&" +
               "redirect_uri=" + formData["target_link_uri"] + "&" +
               "login_hint=" + formData["login_hint"] + "&" +
               "state=" + state + "&" +
               "response_mode=form_post&" +
               "nonce=" + nonce + "&" +
               "prompt=none&" +
               "lti_message_hint=" + formData["lti_message_hint"];
    }

    public async Task<string> HandleJWT(IFormCollection formData)
    {
        var state = States[formData["state"]];
        if (state == (null, null)) throw new MoodleSyncException("No state saved with given value!");
        States.Remove(formData["state"]);
        var id_token = formData["id_token"].FirstOrDefault();
        if (id_token == null) throw new MoodleSyncException("No token available to be read!");
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.ReadToken(id_token) as JwtSecurityToken;
        if (token == null) throw new MoodleSyncException("Error parsing token!");
        if (!token.Payload["aud"].Equals(state.Item1) || !token.Payload["nonce"].Equals(state.Item2))
            throw new MoodleSyncException("Wrong aud or nonce!");
        var lisClaimString = token.Payload[LtiLisClaim.JwtUrl].ToString() ??
                             throw new MoodleSyncException("Lis claim is null");
        var lisClaim = JsonSerializer.Deserialize<LtiLisClaim>(lisClaimString);
        var studentName = token.Payload["name"].ToString() ?? throw new MoodleSyncException("Student name is null");
        var studentMoodleId = token.Payload["sub"].ToString() ?? throw new MoodleSyncException("sub is null");
        await studentService.CreateStudentIfNotAvailableByNeptunAsync(lisClaim.PersonSourcedId, studentName,
            studentMoodleId);
        var ltiResourceLinkString = token.Payload[LtiResourceLink.JwtUrl].ToString() ??
                                    throw new MoodleSyncException("Resource link was null");
        var ltiResourceLink = JsonSerializer.Deserialize<LtiResourceLink>(ltiResourceLinkString);
        var classroomUrl = ltiResourceLink.Description;
        if (string.IsNullOrEmpty(classroomUrl) || !IsValidUrl(classroomUrl))
            throw new MoodleSyncException("Classroom URL was either null, empty or invalid!");
        var lineItemsString = token.Payload[LtiLineItemsWrapper.JwtUrl].ToString() ??
                              throw new MoodleSyncException("Line item was null");
        var lineItems = JsonSerializer.Deserialize<LtiLineItemsWrapper>(lineItemsString);
        await exerciseService.SetMoodleScoreUrlByClassroomUrlWithoutQueryFilterAsync(classroomUrl, lineItems.LineItems);

        return classroomUrl;
    }

    public static bool IsValidUrl(string url)
    {
        return Uri.IsWellFormedUriString(url, UriKind.Absolute) &&
               (url.StartsWith("http://") || url.StartsWith("https://"));
    }
}
