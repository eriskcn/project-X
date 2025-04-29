using System.Text.Json.Serialization;

namespace ProjectX.DTOs;

public class SePayWebhookRequest
{
    [JsonPropertyName("gateway")] public string Gateway { get; set; } = string.Empty;

    [JsonPropertyName("transactionDate")] public string TransactionDate { get; set; } = string.Empty;

    [JsonPropertyName("accountNumber")] public string AccountNumber { get; set; } = string.Empty;

    [JsonPropertyName("subAccount")] public string SubAccount { get; set; } = string.Empty;

    [JsonPropertyName("code")] public string? Code { get; set; }

    [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;

    [JsonPropertyName("transferType")] public string TransferType { get; set; } = string.Empty;

    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;

    [JsonPropertyName("transferAmount")] public decimal TransferAmount { get; set; }

    [JsonPropertyName("referenceCode")] public string ReferenceCode { get; set; } = string.Empty;

    [JsonPropertyName("accumulated")] public decimal Accumulated { get; set; }

    [JsonPropertyName("id")] public long Id { get; set; }
}