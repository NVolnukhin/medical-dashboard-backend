using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Services.Jwt
{
    public class JwtBuilder : IJwtBuilder
    {
        private readonly JwtConfig _config;

        public JwtBuilder(IOptions<JwtConfig> options)
        {
            _config = options.Value ?? throw new ArgumentNullException(nameof(options));
    
            if (string.IsNullOrEmpty(_config.SecretKey))
                throw new ArgumentException("JWT SecretKey is not configured");
        }

        // Формирование токена
        public async Task<string> GetTokenAsync(Guid userId)
        {

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.SecretKey));  // Ключ для подписи токена на основе секрета
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim("userId", userId.ToString()),
                //new Claim("role", "doctor")                 //TODO: заменить хардкод на сервис ролей 
            };

            var expirationDate = DateTime.UtcNow.AddDays(7);  // Срок действия токена TODO: изменпть на options.ExpiryMinutes
            
            var jwt = new JwtSecurityToken(
                issuer: _config.Issuer,
                audience: _config.Audience,
                claims: claims,
                signingCredentials:
                signingCredentials,
                expires: expirationDate);

            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            return encodedJwt;
        }


        public string ValidateToken(string token)
        {
            var principal = GetPrincipal(token);
            if (principal == null)
            {
                return string.Empty;
            }

            ClaimsIdentity identity;
            try
            {
                identity = (ClaimsIdentity)principal.Identity;
            }
            catch (NullReferenceException)
            {
                return string.Empty;
            }
            var userIdClaim = identity?.FindFirst("userId");
            if (userIdClaim == null)
            {
                return string.Empty;
            }
            var userId = userIdClaim.Value;
            return userId;
        }

        private ClaimsPrincipal GetPrincipal(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = (JwtSecurityToken)tokenHandler.ReadToken(token);
                if (jwtToken == null)
                {
                    return null;
                }
                var key = Encoding.UTF8.GetBytes(_config.SecretKey);
                var parameters = new TokenValidationParameters()
                {
                    RequireExpirationTime = true,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };

                IdentityModelEventSource.ShowPII = true;
                ClaimsPrincipal principal = tokenHandler.ValidateToken(token, parameters, out _);
                return principal;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}


