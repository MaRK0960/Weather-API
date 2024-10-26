using System.Net;
using System.Net.Mail;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors();

var app = builder.Build();

// Configure the HTTP request pipeline.

//app.UseHttpsRedirection();

app.UseCors(c => c.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

//smtp username     ***REMOVED***
//value             ***REMOVED***
//secret id         ***REMOVED***

app.MapPost("/subscribe", async (HttpRequest request) =>
{
    using StreamReader stream = new(request.Body);
    string email = await stream.ReadToEndAsync();

    SendEmail("***REMOVED***", "test send mail", "test send mail");
});

static void SendEmail(string toAddress, string subject, string body)
{
    try
    {
        // Create the MailMessage object
        MailMessage mail = new()
        {
            From = new MailAddress("***REMOVED***")
        };

        mail.To.Add(toAddress);
        mail.Subject = subject;
        mail.Body = body;

        // Configure SMTP client
        SmtpClient smtpClient = new("smtp.azurecomm.net", 587)
        {
            Credentials = new NetworkCredential("***REMOVED***", "***REMOVED***"),
            EnableSsl = true, // Use SSL to encrypt the connection
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        // Send the email
        smtpClient.Send(mail);
        Console.WriteLine("Email sent successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error: " + ex.Message);
    }
}

app.Run();