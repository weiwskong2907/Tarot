using MailKit.Net.Smtp;
using MimeKit;
using RazorLight;
using Tarot.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Tarot.Core.Entities;

namespace Tarot.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly IRazorLightEngine _razorEngine;
    private readonly IRepository<EmailTemplate> _templateRepo;

    public EmailService(IConfiguration config, IRepository<EmailTemplate> templateRepo)
    {
        _config = config;
        _templateRepo = templateRepo;
        
        _razorEngine = new RazorLightEngineBuilder()
            .UseEmbeddedResourcesProject(typeof(EmailService))
            .UseMemoryCachingProvider()
            .Build();
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_config["Email:SenderName"] ?? "Tarot System", _config["Email:SenderEmail"]));
        message.To.Add(new MailboxAddress("", to));
        message.Subject = subject;

        var builder = new BodyBuilder();
        if (isHtml)
        {
            builder.HtmlBody = body;
        }
        else
        {
            builder.TextBody = body;
        }

        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        // For development, we might not have a real SMTP server.
        // We can use configuration to determine if we should actually send or just log.
        var smtpHost = _config["Email:SmtpHost"];
        if (string.IsNullOrEmpty(smtpHost))
        {
            Console.WriteLine($"[Mock Email] To: {to}, Subject: {subject}, Body: {body}");
            return;
        }

        var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
        var smtpUser = _config["Email:SmtpUser"];
        var smtpPass = _config["Email:SmtpPass"];

        await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(smtpUser, smtpPass);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    public async Task SendTemplateEmailAsync<T>(string to, string templateSlug, T model)
    {
        // 1. Get template from DB
        var template = (await _templateRepo.ListAllAsync()).FirstOrDefault(t => t.Slug == templateSlug);
        
        // Fallback for dev/test if DB template missing
        if (template == null)
        {
             Console.WriteLine($"[Warning] Template '{templateSlug}' not found. Using fallback.");
             await SendEmailAsync(to, $"[Template: {templateSlug}]", model?.ToString() ?? "No Model");
             return;
        }

        // 2. Render Body using RazorLight
        // We use the template content as the "key" for caching
        string body = await _razorEngine.CompileRenderStringAsync(template.Slug, template.BodyHtml, model);
        
        // 3. Render Subject (simple replace or also Razor)
        // For simplicity, let's assume subject is simple text or simple replace. 
        // We could also run Razor on subject if needed.
        string subject = template.SubjectTpl; 

        await SendEmailAsync(to, subject, body, true);
    }

    public async Task<string> RenderTemplateAsync<T>(string templateSlug, T model)
    {
        var template = (await _templateRepo.ListAllAsync()).FirstOrDefault(t => t.Slug == templateSlug);
        if (template == null) return $"Template '{templateSlug}' not found.";
        
        return await _razorEngine.CompileRenderStringAsync(template.Slug, template.BodyHtml, model);
    }
}
