
using ContactPro.Models;
using ContactPro.Services.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using static MailKit.Security.SecureSocketOptions;

namespace ContactPro.Services;

public class EmailService : IEmailService
{
    private readonly MailSettings _mailSettings;

    // IOptions helps us get values from user secrets. This corresponds with a builder statement in Program.cs builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
    public EmailService(IOptions<MailSettings> mailSettings)
    {
        _mailSettings = mailSettings.Value;
    }
    
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // if _mailSettings.Email returns null then initialise emailSender with Environment.GetEnvironmentVariable()
        var emailSender = _mailSettings.Email ?? Environment.GetEnvironmentVariable("Email");

        MimeMessage newEmail = new();

        newEmail.Sender = MailboxAddress.Parse(emailSender);

        foreach (var emailAddress in email.Split(";"))
        {
            newEmail.To.Add(MailboxAddress.Parse(emailAddress));
        }

        newEmail.Subject = subject;

        // Formats a message
        BodyBuilder emailBody = new();
        emailBody.HtmlBody = htmlMessage;

        newEmail.Body = emailBody.ToMessageBody();
        
        // Login to smtp client
        using SmtpClient smtpClient = new();

        try
        {
            var host = _mailSettings.Host ?? Environment.GetEnvironmentVariable("Host");
            var port = _mailSettings.Port != 0 ? _mailSettings.Port  : int.Parse(Environment.GetEnvironmentVariable("Port"));
            var password = _mailSettings.Password ?? Environment.GetEnvironmentVariable("Password");

            await smtpClient.ConnectAsync(host, port, StartTls);
            await smtpClient.AuthenticateAsync(emailSender, password);

            await smtpClient.SendAsync(newEmail);
            await smtpClient.DisconnectAsync(true);
        }
        catch(Exception ex)
        {
            var error = ex.Message;
            throw;
        }

    }

    // public Task AddContactToCategoryAsync(string email, string subject, string htmlMessage)
    // {
    //     throw new NotImplementedException();
    // }
}