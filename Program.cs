using Azure.Identity;
using Azure.Messaging.EventGrid;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Net.Http.Headers;
using System.Net.Mail;
using Weather_API;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();

builder.Configuration.AddAzureAppConfiguration(a =>
    a.ConfigureKeyVault(c => c.Register(new SecretClient(new Uri(builder.Configuration["Endpoints:KeyVault"]!), new DefaultAzureCredential())))
    .Connect(new Uri(builder.Configuration["Endpoints:AppConfiguration"]!), new DefaultAzureCredential()));

WebApplication app = builder.Build();

app.UseHttpsRedirection();

app.UseCors(c =>
    c.SetIsOriginAllowed(a => new Uri(a).IsLoopback)
     .WithHeaders(HeaderNames.ContentType)
);

CosmosClient client = new(
    accountEndpoint: app.Configuration["Weather-Cosmos-URI"],
    tokenCredential: new DefaultAzureCredential()
);

Database database = client.GetDatabase("Weather");
Container container = database.GetContainer("Users");

EventGridPublisherClient eventGridPublisherClient = new(new Uri(app.Configuration["Weather-Grid-Event-URI"]!), new DefaultAzureCredential());

app.MapPost("/subscribe", async (Subscription subscription) =>
{
    if (!IsValidEmail(subscription.Email))
    {
        return Results.BadRequest("Wrong email format");
    }

    FeedIterator<WeatherUser> feed = container
        .GetItemLinqQueryable<WeatherUser>()
        .Where(s => s.Email == subscription.Email)
        .ToFeedIterator();

    if (feed.HasMoreResults)
    {
        FeedResponse<WeatherUser> response = await feed.ReadNextAsync();
        WeatherUser? existingSubscription = response.FirstOrDefault(s => s.Email == subscription.Email);

        if (existingSubscription is not null)
        {
            existingSubscription.NotificationTime = ExtractHours(subscription.NotificationTime);
            existingSubscription.DeltaTemperature = subscription.DeltaTemperature;

            await container.UpsertItemAsync(existingSubscription);
            await eventGridPublisherClient.SendEventAsync(CreateEvent(true, subscription.Email));
            return Results.Ok();
        }
    }

    await container.UpsertItemAsync(new WeatherUser(Guid.NewGuid(), subscription.Email, subscription.DeltaTemperature, ExtractHours(subscription.NotificationTime)));

    await eventGridPublisherClient.SendEventAsync(CreateEvent(false, subscription.Email));

    return Results.Created();
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

static int[] ExtractHours(TimeOnly[]? times)
{
    if (times is null)
        return [];

    return times
        .Select(t => t.Hour)
        .ToArray();
}

static EventGridEvent CreateEvent(bool isUpdate, string email)
{
    return new("WeatherContainerAPI",
        isUpdate ? "UpdateSubscription" : "NewSubscription",
        "1.0",
        email);
}

app.Run();