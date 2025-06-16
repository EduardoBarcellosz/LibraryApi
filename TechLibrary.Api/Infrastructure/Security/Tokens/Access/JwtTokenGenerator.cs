using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TechLibrary.Api.Domain.Entities;

namespace TechLibrary.Api.Infrastructure.Security.Tokens.Access
{
    public class JwtTokenGenerator
    {
        public string Generate(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Expires = DateTime.UtcNow.AddMinutes(60),
                Subject = new ClaimsIdentity(claims),
                SigningCredentials = new SigningCredentials(SecurityKey(), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var securityToken = tokenHandler.CreateToken(tokenDescriptor); //JWE - JSON Web Encryption

            return tokenHandler.WriteToken(securityToken);
        }

        private static SymmetricSecurityKey SecurityKey()
        {
            var signingKey = "N?wU4bM0rX1K%l<[1?OW{>+dMx{qs7ul";
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        }
    }
}
