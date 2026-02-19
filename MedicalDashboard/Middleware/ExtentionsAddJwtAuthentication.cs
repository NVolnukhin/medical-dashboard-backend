using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Middleware
{
    public static class ExtentionsAddJwtAuthentication
    {
        public static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var section = configuration.GetSection("jwt");
            var optionsJwt = section.GetChildren().ToArray()[0];
            //var options = section.Get<JwtOptions>();
            var key = Encoding.UTF8.GetBytes(optionsJwt.Value);

            section.Bind(optionsJwt);
            services.Configure<JwtConfig>(section);

            const string authProviderKey = "Bearer";

            services.AddAuthentication(authProviderKey)
                .AddJwtBearer(authProviderKey, x =>
                {
                    x.RequireHttpsMetadata = false;
                    x.SaveToken = true;
                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                });

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder(authProviderKey)
                    .RequireAuthenticatedUser()
                    .Build();
            });
        }
    }

}
