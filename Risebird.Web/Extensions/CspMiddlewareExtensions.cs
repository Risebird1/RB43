using Microsoft.AspNetCore.Builder;
using Risebird.Web.Hepler;
using Risebird.Web.Security;
using System;

namespace Risebird.Web.Extensions
{
    public static class CspMiddlewareExtensions
    {
        public static IApplicationBuilder UseCsp(
          this IApplicationBuilder app, Action<CspOptionsBuilder> builder)
        {
            var newBuilder = new CspOptionsBuilder();
            builder(newBuilder);

            var options = newBuilder.Build();
            return app.UseMiddleware<CspMiddleware>(options);
        }
    }
}
