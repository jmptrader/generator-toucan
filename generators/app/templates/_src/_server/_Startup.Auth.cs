﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using <%=assemblyName%>.Service;

namespace <%=assemblyName%>.Server
{
    public static partial class Extensions
    {
        public static void ConfigureAuthentication(this IServiceCollection services, TokenProviderConfig cfg, string[] areas)
        {
            SymmetricSecurityKey signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(cfg.TokenSecurityKey));

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true, // The signing key must match!
                IssuerSigningKey = signingKey,
                ValidateIssuer = true, // Validate the JWT Issuer (iss) claim
                ValidIssuer = cfg.TokenIssuer,
                ValidateAudience = true, // Validate the JWT Audience (aud) claim
                ValidAudience = cfg.TokenAudience,
                ValidateLifetime = true, // Validate the token expiry
                ClockSkew = TimeSpan.Zero // If you want to allow a certain amount of clock drift, set that here:
            };

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = (context) =>
                    {
                        return OnChallenge(context, areas);
                    }
                };
                options.SaveToken = true;
                options.TokenValidationParameters = tokenValidationParameters;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = "access_token";
                options.TicketDataFormat = new Model.TokenDataFormat(cfg.TokenSecurityAlgorithm, CookieAuthenticationDefaults.AuthenticationScheme, tokenValidationParameters);
            });
        }

        private static Task OnChallenge(JwtBearerChallengeContext context, string[] areas)
        {
            if (context.AuthenticateFailure != null)
            {
                string location = CreateReturnLocation(context, areas);

                context.Response.Headers.Append(HeaderNames.Location, location);
                context.Response.Headers.Append(HeaderNames.WWWAuthenticate, context.Options.Challenge);

                if (context.Request.AcceptsJsonResponse())
                {
                    return Task.Factory.StartNew(() =>
                    {
                        context.Response.StatusCode = 401;
                        context.HandleResponse();
                    });
                }
            }

            return Task.Factory.StartNew(() => context.HandleResponse());
        }

        private static string CreateReturnLocation(JwtBearerChallengeContext context, string[] areas)
        {
            string locationHeader = context.Request.Headers[HeaderNames.Location];

            Uri referrer = new Uri(context.Request.Headers[HeaderNames.Referer]);
            Uri location = new Uri(locationHeader ?? referrer.ToString());

            string returnUrl = CreateReturnUrl(referrer, areas);

            string locationUri = QueryHelpers.AddQueryString("Login", "returnUrl", returnUrl);

            if (!string.IsNullOrEmpty(context.Error))
                locationUri = QueryHelpers.AddQueryString(locationUri, "errorCode", context.Error);

            if (!string.IsNullOrEmpty(context.AuthenticateFailure.Message))
                locationUri = QueryHelpers.AddQueryString(locationUri, "errorDesc", context.ErrorDescription);

            return locationUri;
        }

        private static string CreateReturnUrl(Uri referrer, string[] areas)
        {
            string areaPattern = string.Join(string.Empty, areas.Select(o => "/" + o));

            Regex regex = new Regex($"({areaPattern})");

            return regex.Replace(referrer.ToString(), string.Empty, 1);
        }
    }
}
