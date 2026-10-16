using System.ComponentModel.DataAnnotations;

namespace SBus.Application.Features.Accounts;

public sealed record ConfirmEmailRequest([Required, StringLength(128)] string UserId, [Required, StringLength(2048)] string Code);