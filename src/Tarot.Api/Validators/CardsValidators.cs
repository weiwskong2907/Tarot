using FluentValidation;
using Tarot.Api.Controllers;
using System.Text.RegularExpressions;

namespace Tarot.Api.Validators;

public class CardCreateDtoValidator : AbstractValidator<CardCreateDto>
{
    public CardCreateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.ImageUrl)
            .Must(url => string.IsNullOrWhiteSpace(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("ImageUrl must be a valid absolute URL.");
        RuleFor(x => x.MeaningUpright).MaximumLength(2000).When(x => x.MeaningUpright != null);
        RuleFor(x => x.MeaningReversed).MaximumLength(2000).When(x => x.MeaningReversed != null);
        RuleFor(x => x.Keywords).MaximumLength(500).When(x => x.Keywords != null);
        RuleFor(x => x.AdminNotes).MaximumLength(2000).When(x => x.AdminNotes != null);
    }
}

public class CardUpdateDtoValidator : AbstractValidator<CardUpdateDto>
{
    public CardUpdateDtoValidator()
    {
        Include(new CardCreateDtoValidator());
    }
}
