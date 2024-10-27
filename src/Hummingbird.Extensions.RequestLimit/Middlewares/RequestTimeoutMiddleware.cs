using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Polly;

namespace Hummingbird.Extensions.RequestLimit
{
    public class RequestTimeoutMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RequestTimeoutConfiguration _timeoutConfiguration;
        private readonly ConcurrentDictionary<string, int> _policies;

        public RequestTimeoutMiddleware(RequestDelegate next, RequestTimeoutConfiguration timeoutConfiguration)
        {
            _next = next;
            _timeoutConfiguration = timeoutConfiguration;
            _policies = new ConcurrentDictionary<string, int>();
        }


        private int GetTimeout(string route, string method)
        {
            //根据路由获取限流策略
            var rule = _timeoutConfiguration.Rules.Where(a =>
                a.Method.ToUpper() == method.ToUpper() && Regex.IsMatch(route.ToLower(), a.Route,
                    RegexOptions.IgnoreCase | RegexOptions.Compiled)).FirstOrDefault();
            //策略存在则创建限流策略
            if (rule != null)
            {
                string key = $"{rule.Method}:{rule.Route}";

                if (_policies.ContainsKey(key))
                {
                    return _policies[key];
                }
                else
                {
                    //限流策略缓存起来
                    return _policies.GetOrAdd(key, _ => rule.TimeoutMillseconds);
                }
            }

            return -1;

        }

        public async Task InvokeAsync(HttpContext context)
        {
            var timeout = GetTimeout(context.Request.Path.Value, context.Request.Method);

            if (timeout > 0)
            {
                using (var cts = new CancellationTokenSource(timeout))
                {
                    context.RequestAborted = cts.Token;

                    try
                    {
                        await _next(context);
                    }
                    catch (TaskCanceledException)
                    {
                        context.Response.StatusCode = 408; // 请求超时
                    }
                }
            }
            else
            {
                await _next(context);
            }
        }
    }
}