namespace Tarot.Core.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task SendTemplateEmailAsync<T>(string to, string templateSlug, T model);
    Task<string> RenderTemplateAsync<T>(string templateSlug, T model);
}
