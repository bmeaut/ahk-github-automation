namespace Ahk.GradeManagement.Data.Entities
{
    public class WebhookToken
    {
        public WebhookToken(string id, string secret, string description)
        {
            this.Id = id;
            this.Secret = secret;
            this.Description = description;
        }

        public string Id { get; }
        public string Secret { get; }
        public string Description { get; }
    }
}
