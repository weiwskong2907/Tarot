using FluentValidation;
using Tarot.Api.Dtos;

namespace Tarot.Api.Validators;

public class CreateAppointmentDtoValidator : AbstractValidator<CreateAppointmentDto>
{
    public CreateAppointmentDtoValidator()
    {
        RuleFor(x => x.ServiceId).NotEmpty();
        RuleFor(x => x.StartTime).NotEmpty().Must(BeInFuture).WithMessage("Appointment time must be in the future.");
    }

    private bool BeInFuture(DateTime date)
    {
        return date > DateTime.UtcNow;
    }
}

public class RescheduleAppointmentDtoValidator : AbstractValidator<RescheduleAppointmentDto>
{
    public RescheduleAppointmentDtoValidator()
    {
        RuleFor(x => x.NewStartTime).NotEmpty().Must(BeInFuture).WithMessage("New appointment time must be in the future.");
    }

    private bool BeInFuture(DateTime date)
    {
        return date > DateTime.UtcNow;
    }
}

public class ConsultationMessageDtoValidator : AbstractValidator<ConsultationMessageDto>
{
    public ConsultationMessageDtoValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
    }
}
