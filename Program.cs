using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Weather_API;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors();

var app = builder.Build();

// Configure the HTTP request pipeline.

//app.UseHttpsRedirection();

app.UseCors(c => c.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

TableServiceClient tableServiceClient = new("***REMOVED***");
Response<TableItem> table = tableServiceClient.CreateTableIfNotExists("Emails");

TableClient tableClient = tableServiceClient.GetTableClient(table.Value.Name);

//smtp username     ***REMOVED***
//value             ***REMOVED***
//secret id         ***REMOVED***

app.MapPost("/subscribe", async (HttpRequest request) =>
{
    using StreamReader stream = new(request.Body);
    string email = await stream.ReadToEndAsync();

    if (!IsValidEmail(email))
    {
        return Results.BadRequest("Wrong email format");
    }

    tableClient.UpsertEntity(new Email(email));

    using (HttpClient client = new())
    {
        await client.GetAsync($"***REMOVED***{email}");
    }

    return Results.Ok();
});

bool IsValidEmail(string email)
{
    var trimmedEmail = email.Trim();

    if (trimmedEmail.EndsWith("."))
    {
        return false; // suggested by @TK-421
    }
    try
    {
        var addr = new System.Net.Mail.MailAddress(email);
        return addr.Address == trimmedEmail;
    }
    catch
    {
        return false;
    }
}

app.Run();