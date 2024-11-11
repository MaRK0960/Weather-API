using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System.Net.Mail;
using Weather_API;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();

WebApplication app = builder.Build();

app.UseHttpsRedirection();

app.UseCors(c =>
    c.SetIsOriginAllowed(a => new Uri(a).IsLoopback)
);

SecretClient client = new(new("https://weather-vault.vault.azure.net/"), new DefaultAzureCredential());
Response<KeyVaultSecret> connectionStringSecret = await client.GetSecretAsync("Weather-Table-Connection-String");

TableServiceClient tableServiceClient = new(connectionStringSecret.Value.Value);
Response<TableItem> table = tableServiceClient.CreateTableIfNotExists("Emails");

TableClient tableClient = tableServiceClient.GetTableClient(table.Value.Name);

app.MapPost("/subscribe", async (HttpRequest request) =>
{
    using StreamReader stream = new(request.Body);
    string email = await stream.ReadToEndAsync();

    if (!IsValidEmail(email))
    {
        return Results.BadRequest("Wrong email format");
    }

    Email emailEntity = new(email);

    NullableResponse<Email> response = await tableClient.GetEntityIfExistsAsync<Email>(emailEntity.PartitionKey, emailEntity.RowKey);

    if (!response.HasValue)
    {
        await tableClient.UpsertEntityAsync(emailEntity);

        using HttpClient client = new();
        await client.GetAsync($"***REMOVED***{email}");
    }

    return Results.Ok();
});

static bool IsValidEmail(string email)
{
    string trimmedEmail = email.Trim();

    if (trimmedEmail.EndsWith('.'))
        return false;

    try
    {
        MailAddress addr = new(email);
        return addr.Address == trimmedEmail;
    }
    catch
    {
        return false;
    }
}

app.Run();