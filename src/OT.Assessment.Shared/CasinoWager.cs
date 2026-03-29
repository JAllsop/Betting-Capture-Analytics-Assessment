using System.Text.Json.Serialization;

namespace OT.Asessment.Shared
{
        public class CasinoWager
        {
            [JsonPropertyName("wagerId")]
            public string WagerId { get; set; } = default!;
            

            [JsonPropertyName("theme")]
            public string Theme { get; set; } = default!;
            

            [JsonPropertyName("provider")]
            public string Provider { get; set; } = default!;
            

            [JsonPropertyName("gameName")]
            public string GameName { get; set; } = default!;
            

            [JsonPropertyName("transactionId")]
            public string TransactionId { get; set; } = default!;
            

            [JsonPropertyName("brandId")]
            public string BrandId { get; set; } = default!;
            

            [JsonPropertyName("accountId")]
            public string AccountId { get; set; } = default!;
            

            [JsonPropertyName("Username")]
            public string Username { get; set; } = default!;
            

            [JsonPropertyName("externalReferenceId")]
            public string ExternalReferenceId { get; set; } = default!;
            

            [JsonPropertyName("transactionTypeId")]
            public string TransactionTypeId { get; set; } = default!;
            

            [JsonPropertyName("amount")]
            public double Amount { get; set; } = default!;
            

            [JsonPropertyName("createdDateTime")]
            public DateTime CreatedDateTime { get; set; } = default!;
            

            [JsonPropertyName("numberOfBets")]
            public int NumberOfBets { get; set; } = default!;
            

            [JsonPropertyName("countryCode")]
            public string CountryCode { get; set; } = default!;


            [JsonPropertyName("sessionData")]
            public string SessionData { get; set; } = default!;


            [JsonPropertyName("duration")]
            public long Duration { get; set; }
    }
}
