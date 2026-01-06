using FluentValidation;
using Tarot.Api.Controllers;

namespace Tarot.Api.Validators;

public class ContactMessageCreateDtoValidator : AbstractValidator<ContactMessageCreateDto>
{
    public ContactMessageCreateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(5000);
    }
}

public class ContactMessageReplyDtoValidator : AbstractValidator<ContactMessageReplyDto>
{
    public ContactMessageReplyDtoValidator()
    {
        RuleFor(x => x.Reply).NotEmpty().MaximumLength(5000);
    }
}
