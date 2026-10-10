using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

using SBus.Application.Common.Security;

namespace SBus.Web.Pages.Account;

[Authorize(Policy = AuthPolicies.Company)]
public class ComingSoonModel : PageModel;

