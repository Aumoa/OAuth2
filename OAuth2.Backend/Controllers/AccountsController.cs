using Microsoft.AspNetCore.Mvc;
using OAuth2.Data;
using OAuth2.DataTransfer;
using OAuth2.Repositories;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/accounts")]
public class AccountsController(IAccounts accounts) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync([FromQuery] string id, CancellationToken cancellationToken = default)
    {
        var account = await accounts.GetAccountAsync(id, cancellationToken);
        if (account == null)
        {
            if (id == "exist")
            {
                return Ok(new Account());
            }
            return NotFound();
        }

        account.Password = null;
        return Ok(account);
    }

    [HttpPost]
    public async Task<IActionResult> PostAsync([FromBody] RegisterForm form, CancellationToken cancellationToken = default)
    {
        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var emailVerifyCode = await accounts.AddAccountAsync(form.Id, form.Password, form.FullName, form.Email, cancellationToken);
        if (string.IsNullOrEmpty(emailVerifyCode))
        {
            return Conflict();
        }


        return Ok();
    }
}
