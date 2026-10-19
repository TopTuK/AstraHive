using AH.AppDomain.Users;
using FluentValidation;

namespace AH.Application.Users.UpdateUserProfile;

// Lengths are checked after trimming, matching what User.Rename stores.
public sealed class UpdateUserProfileValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileValidator()
    {
        RuleFor(c => c.FirstName)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("First name is required.")
            .Must(name => name is null || name.Trim().Length <= User.NameMaxLength)
            .WithMessage($"First name must be at most {User.NameMaxLength} characters.");
        RuleFor(c => c.LastName)
            .Must(name => name is null || name.Trim().Length <= User.NameMaxLength)
            .WithMessage($"Last name must be at most {User.NameMaxLength} characters.");
    }
}
