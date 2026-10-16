using System.ComponentModel.DataAnnotations;

using SBus.Application.Common.Validation;

namespace SBus.Application.Features.Accounts;

public sealed record ResetPasswordRequest(
    [Required, StringLength(128)] string UserId,
    [Required, StringLength(2048)] string Code,
    [Required, StringLength(AccountPasswordPolicy.MaxLength, MinimumLength = AccountPasswordPolicy.MinLength)] string Password,
    [Required] string ConfirmPassword);