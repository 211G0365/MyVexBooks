using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MyVexBooks.Helpers
{
    public class JwtHelper
    {
        private readonly string _secretKey;
        private readonly string _issuer; private readonly string _audience;
        public JwtHelper(IConfiguration configuration)
        {
          
            _secretKey = configuration["JwtSettings:SecretKey"] ?? "";
            _issuer = configuration["JwtSettings:Issuer"] ?? "";
            _audience = configuration["JwtSettings:Audience"] ?? "";
        }
        public string GenerateJwtToken(List<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddMinutes(5), 
                NotBefore = DateTime.Now,
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = creds
            }; var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
