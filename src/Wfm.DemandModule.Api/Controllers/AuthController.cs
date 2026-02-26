using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Wfm.DemandModule.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IConfiguration _cfg;
    public AuthController(IConfiguration cfg) => _cfg = cfg;

    public sealed record TokenRequest(string UserId, string Role);
    public sealed record TokenResponse(string AccessToken, DateTime ExpiresAtUtc);

    [HttpPost("token")]
    public ActionResult<TokenResponse> CreateToken([FromBody] TokenRequest req)
    {
        var issuer = _cfg["Auth:Issuer"]!;
        var audience = _cfg["Auth:Audience"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Auth:SigningKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, req.UserId),
            new Claim(ClaimTypes.Role, req.Role)
        };

        var expires = DateTime.UtcNow.AddHours(8);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: expires, signingCredentials: creds);

        return Ok(new TokenResponse(new JwtSecurityTokenHandler().WriteToken(token), expires));
    }
}
