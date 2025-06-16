using System.IdentityModel.Tokens.Jwt;
using TechLibrary.Api.Domain.Entities;
using TechLibrary.Api.Infrastructure.DataAccess;

namespace TechLibrary.Api.Services.LoggedUser;

public class LoggedUserService
{
    private readonly HttpContext _httpContext;

    public LoggedUserService(HttpContext httpContext)
    {
        _httpContext = httpContext;
    }

    public User User(TechLibraryDbContext dbContext) 
    {
        var authentication = _httpContext.Request.Headers.Authorization.ToString();

        var token = authentication["Bearer ".Length..].Trim();

        var tokenHandler = new JwtSecurityTokenHandler();

        var jwtSecurityToken = tokenHandler.ReadJwtToken(token);

        var subClaim = jwtSecurityToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        if (subClaim == null)
        {
            throw new System.Exception("Claim 'sub' não encontrada no token.");
        }
        var identifier = subClaim.Value;


        var userId = Guid.Parse(identifier);

        return dbContext.Users.First(user => user.Id == userId);
    }
}