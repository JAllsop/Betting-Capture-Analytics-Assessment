using System.Text.Json.Serialization;

namespace OT.Assessment.Shared
{
        public class CasinoWager
        {
            [JsonPropertyName("wagerId")]
            public Guid WagerId { get; set; } = default!;
            

            [JsonPropertyName("theme")]
            public string Theme { get; set; } = default!;
            

            [JsonPropertyName("provider")]
            public string Provider { get; set; } = default!;
            

            [JsonPropertyName("gameName")]
            public string GameName { get; set; } = default!;
            

            [JsonPropertyName("transactionId")]
            public Guid TransactionId { get; set; } = default!;
            

            [JsonPropertyName("brandId")]
            public Guid BrandId { get; set; } = default!;
            

            [JsonPropertyName("accountId")]
            public Guid AccountId { get; set; } = default!;
            

            [JsonPropertyName("Username")]
            public string Username { get; set; } = default!;
            

            [JsonPropertyName("externalReferenceId")]
            public Guid ExternalReferenceId { get; set; } = default!;
            

            [JsonPropertyName("transactionTypeId")]
            public Guid TransactionTypeId { get; set; } = default!;
            

            [JsonPropertyName("amount")]
            public decimal Amount { get; set; } = default!;
            

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
