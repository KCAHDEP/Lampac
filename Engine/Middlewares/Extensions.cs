﻿using Microsoft.AspNetCore.Builder;

namespace Lampac.Engine.Middlewares
{
    public static class Extensions
    {
        public static IApplicationBuilder UseModHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ModHeaders>();
        }

        public static IApplicationBuilder UseFindKP(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<FindKP>();
        }

        public static IApplicationBuilder UseProxyAPI(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ProxyAPI>();
        }

        public static IApplicationBuilder UseProxyIMG(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ProxyImg>();
        }
    }
}
