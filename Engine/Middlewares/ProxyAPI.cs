﻿using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Threading;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;
using System.Net;
using System.Linq;
using System.Text;

namespace Lampac.Engine.Middlewares
{
    public class ProxyAPI
    {
        #region ProxyAPI
        private readonly RequestDelegate _next;

        public ProxyAPI(RequestDelegate next)
        {
            _next = next;
        }
        #endregion

        async public Task InvokeAsync(HttpContext httpContext)
        {
            if (httpContext.Request.Path.Value.StartsWith("/proxy/"))
            {
                if (AppInit.conf.disableserverproxy)
                {
                    httpContext.Response.StatusCode = 403;
                    return;
                }

                if (HttpMethods.IsOptions(httpContext.Request.Method))
                {
                    httpContext.Response.StatusCode = 405;
                    return;
                }

                string servUri = httpContext.Request.Path.Value.Replace("/proxy/", "") + httpContext.Request.QueryString.Value;

                HttpClientHandler handler = new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.Brotli | DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    AllowAutoRedirect = false
                };

                handler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

                using (var client = new HttpClient(handler))
                {
                    var request = CreateProxyHttpRequest(httpContext, new Uri(servUri));
                    var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, httpContext.RequestAborted);

                    if ((int)response.StatusCode is 301 or 302 || response.Headers.Location != null)
                    {
                        httpContext.Response.Redirect($"{AppInit.Host(httpContext)}/proxy/{response.Headers.Location.AbsoluteUri}");
                        return;
                    }

                    if (response.Content.Headers.TryGetValues("Content-Type", out var contentType) && contentType.First().ToLower() is "application/x-mpegurl" or "application/vnd.apple.mpegurl" or "text/plain")
                    {
                        using (HttpContent content = response.Content)
                        {
                            if (response.StatusCode == HttpStatusCode.OK)
                            {
                                string proxyhost = $"{AppInit.Host(httpContext)}/proxy";
                                string m3u8 = Regex.Replace(Encoding.UTF8.GetString(await content.ReadAsByteArrayAsync()), "(https?://[^\n\r\"\\# ]+)", m =>
                                {
                                    return $"{proxyhost}/{m.Groups[1].Value}";
                                });

                                string hlshost = Regex.Match(servUri, "(https?://[^/]+)/").Groups[1].Value;
                                string hlspatch = Regex.Match(servUri, "(https?://[^\n\r]+/)([^/]+)$").Groups[1].Value;

                                m3u8 = Regex.Replace(m3u8, "([\n\r])([^\n\r]+)", m =>
                                {
                                    string uri = m.Groups[2].Value;

                                    if (uri.Contains("#") || uri.Contains("\"") || uri.StartsWith("http"))
                                        return m.Groups[0].Value;

                                    if (uri.StartsWith("/"))
                                    {
                                        uri = hlshost + uri;
                                    }
                                    else
                                    {
                                        uri = hlspatch + uri;
                                    }

                                    return m.Groups[1].Value + $"{proxyhost}/{uri}";
                                });

                                m3u8 = Regex.Replace(m3u8, "(URI=\")([^\"]+)", m =>
                                {
                                    string uri = m.Groups[2].Value;

                                    if (uri.Contains("\"") || uri.StartsWith("http"))
                                        return m.Groups[0].Value;

                                    if (uri.StartsWith("/"))
                                    {
                                        uri = hlshost + uri;
                                    }
                                    else
                                    {
                                        uri = hlspatch + uri;
                                    }

                                    return m.Groups[1].Value + $"{proxyhost}/{uri}";
                                });

                                httpContext.Response.ContentType = contentType.First();
                                await httpContext.Response.WriteAsync(m3u8);
                            }
                        }
                    }
                    else
                    {
                        await CopyProxyHttpResponse(httpContext, response);
                    }
                }
            }
            else
            {
                await _next(httpContext);
            }
        }


        #region CreateProxyHttpRequest
        HttpRequestMessage CreateProxyHttpRequest(HttpContext context, Uri uri)
        {
            var request = context.Request;

            var requestMessage = new HttpRequestMessage();
            var requestMethod = request.Method;
            if (HttpMethods.IsPost(requestMethod))
            {
                var streamContent = new StreamContent(request.Body);
                requestMessage.Content = streamContent;
            }

            requestMessage.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/106.0.0.0 Safari/537.36");

            foreach (var header in request.Headers)
            {
                if (header.Key.ToLower() is "origin" or "user-agent" or "referer")
                    continue;

                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && requestMessage.Content != null)
                {
                    //Console.WriteLine(header.Key + ": " + String.Join(" ", header.Value.ToArray()));
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            if (uri.Host.Contains("aniboom."))
            {
                requestMessage.Headers.TryAddWithoutValidation("origin", "https://aniboom.one");
                requestMessage.Headers.TryAddWithoutValidation("referer", "https://aniboom.one/");
            }

            requestMessage.Headers.Host = uri.Authority;
            requestMessage.RequestUri = uri;
            requestMessage.Method = new HttpMethod(request.Method);

            return requestMessage;
        }
        #endregion

        #region CopyProxyHttpResponse
        async Task CopyProxyHttpResponse(HttpContext context, HttpResponseMessage responseMessage)
        {
            var response = context.Response;
            response.StatusCode = (int)responseMessage.StatusCode;

            #region UpdateHeaders
            void UpdateHeaders(HttpHeaders headers)
            {
                foreach (var header in headers)
                {
                    if (header.Key.ToLower() is "transfer-encoding" or "etag" or "connection")
                        continue;

                    if (header.Key.ToLower().Contains("access-control"))
                        continue;

                    string value = string.Empty;
                    foreach (var val in header.Value)
                        value += $"; {val}";

                    response.Headers[header.Key] = Regex.Replace(value, "^; ", "");
                    //response.Headers[header.Key] = header.Value.ToArray();
                }
            }
            #endregion

            UpdateHeaders(responseMessage.Headers);
            UpdateHeaders(responseMessage.Content.Headers);

            using (var responseStream = await responseMessage.Content.ReadAsStreamAsync())
            {
                await CopyToAsyncInternal(response.Body, responseStream, context.RequestAborted);
                //await responseStream.CopyToAsync(response.Body, context.RequestAborted);
            }
        }
        #endregion


        #region CopyToAsyncInternal
        async Task CopyToAsyncInternal(Stream destination, Stream responseStream, CancellationToken cancellationToken)
        {
            if (destination == null)
                throw new ArgumentNullException("destination");

            if (!responseStream.CanRead && !responseStream.CanWrite)
                throw new ObjectDisposedException("ObjectDisposed_StreamClosed");

            if (!destination.CanRead && !destination.CanWrite)
                throw new ObjectDisposedException("ObjectDisposed_StreamClosed");

            if (!responseStream.CanRead)
                throw new NotSupportedException("NotSupported_UnreadableStream");

            if (!destination.CanWrite)
                throw new NotSupportedException("NotSupported_UnwritableStream");

            byte[] buffer = new byte[81920];
            int bytesRead;
            while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
        }
        #endregion
    }
}
