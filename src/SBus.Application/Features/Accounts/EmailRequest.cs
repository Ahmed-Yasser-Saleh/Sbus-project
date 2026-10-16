using System.ComponentModel.DataAnnotations;

namespace SBus.Application.Features.Accounts;

public sealed record EmailRequest([Required, EmailAddress, StringLength(256)] string Email);