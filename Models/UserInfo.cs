using System.Text.Json.Serialization;

namespace WebApplicationCentralino.Models
{
    public class UserInfo
    {
        [JsonPropertyName("id")]
        public int id { get; set; }

        [JsonPropertyName("nome")]
        public string nome { get; set; }

        [JsonPropertyName("email")]
        public string email { get; set; }

        [JsonPropertyName("ruolo")]
        public string ruolo { get; set; }
    }
} 