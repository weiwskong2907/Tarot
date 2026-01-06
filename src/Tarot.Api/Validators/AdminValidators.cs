using FluentValidation;
using Tarot.Api.Dtos;

namespace Tarot.Api.Validators;

public class ReplyRequestValidator : AbstractValidator<ReplyRequest>
{
    public ReplyRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty().WithMessage("Reply message is required.");
    }
}

public class BlockSlotRequestValidator : AbstractValidator<BlockSlotRequest>
{
    public BlockSlotRequestValidator()
    {
        RuleFor(x => x.StartTime).NotEmpty();
        RuleFor(x => x.EndTime).NotEmpty().GreaterThan(x => x.StartTime).WithMessage("End time must be after start time.");
        RuleFor(x => x.Reason).MaximumLength(200);
    }
}

public class CreateStaffRequestValidator : AbstractValidator<CreateStaffRequest>
{
    public CreateStaffRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FullName).NotEmpty();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).WithMessage("Password must be at least 6 characters.");
    }
}

public class RestoreRequestValidator : AbstractValidator<RestoreRequest>
{
    public RestoreRequestValidator()
    {
        RuleFor(x => x.Entity).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
    }
}
