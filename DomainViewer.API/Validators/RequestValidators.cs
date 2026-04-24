using DomainViewer.API.DTOs;
using FluentValidation;

namespace DomainViewer.API.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(100);
    }
}

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(100);
    }
}

public class CreateDomainRequestValidator : AbstractValidator<CreateDomainRequest>
{
    public CreateDomainRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255).Must(n => n.Contains('.')).WithMessage("ชื่อ Domain ต้องมีจุด (เช่น example.com)");
        RuleFor(x => x.ExpirationDate).NotEmpty().GreaterThan(DateTime.UtcNow.AddDays(-1));
        RuleFor(x => x.Description).MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.Description));
        RuleFor(x => x.Registrant).MaximumLength(255).When(x => !string.IsNullOrEmpty(x.Registrant));
        RuleFor(x => x.Registrar).MaximumLength(255).When(x => !string.IsNullOrEmpty(x.Registrar));
    }
}

public class UpdateDomainRequestValidator : AbstractValidator<UpdateDomainRequest>
{
    public UpdateDomainRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255).Must(n => n.Contains('.')).WithMessage("ชื่อ Domain ต้องมีจุด (เช่น example.com)");
        RuleFor(x => x.ExpirationDate).NotEmpty().GreaterThan(DateTime.UtcNow.AddDays(-1));
        RuleFor(x => x.Description).MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.Description));
        RuleFor(x => x.Registrant).MaximumLength(255).When(x => !string.IsNullOrEmpty(x.Registrant));
        RuleFor(x => x.Registrar).MaximumLength(255).When(x => !string.IsNullOrEmpty(x.Registrar));
    }
}

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AvatarUrl).MaximumLength(100).When(x => !string.IsNullOrEmpty(x.AvatarUrl));
        RuleFor(x => x.AvatarBgColor).MaximumLength(50).When(x => !string.IsNullOrEmpty(x.AvatarBgColor));
    }
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6).MaximumLength(100);
    }
}

public class CreateUserByOwnerRequestValidator : AbstractValidator<CreateUserByOwnerRequest>
{
    public CreateUserByOwnerRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(100);
    }
}

public class UpdateAlertSettingsRequestValidator : AbstractValidator<UpdateAlertSettingsRequest>
{
    public UpdateAlertSettingsRequestValidator()
    {
        RuleFor(x => x.AlertMonths).GreaterThanOrEqualTo(0).When(x => x.AlertMonths.HasValue);
        RuleFor(x => x.AlertWeeks).GreaterThanOrEqualTo(0).When(x => x.AlertWeeks.HasValue);
        RuleFor(x => x.AlertDays).GreaterThanOrEqualTo(0).When(x => x.AlertDays.HasValue);
    }
}

public class CreateAllowedEmailDomainRequestValidator : AbstractValidator<CreateAllowedEmailDomainRequest>
{
    public CreateAllowedEmailDomainRequestValidator()
    {
        RuleFor(x => x.Domain).NotEmpty().MaximumLength(255)
            .Must(d => !d.Contains('@') && !d.Contains(' ') && d.Contains('.'))
            .WithMessage("รูปแบบโดเมนไม่ถูกต้อง (เช่น company.com)");
    }
}

public class UpdateEmailTemplateRequestValidator : AbstractValidator<Controllers.UpdateEmailTemplateRequest>
{
    public UpdateEmailTemplateRequestValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(50000);
    }
}

public class UpdateRecipientsRequestValidator : AbstractValidator<UpdateRecipientsRequest>
{
    public UpdateRecipientsRequestValidator()
    {
        RuleFor(x => x.UserIds).NotNull();
    }
}
