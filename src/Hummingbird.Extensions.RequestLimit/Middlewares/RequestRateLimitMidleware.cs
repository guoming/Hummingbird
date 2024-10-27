using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Polly;
using Polly.RateLimit;
using System;
using System.Threading.Tasks;

namespace Hummingbird.Extensions.RequestLimit
{

    public class RequestRateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RequestRateLimitConfiguration _rateLimitConfiguration;
        private readonly ConcurrentDictionary<string, AsyncRateLimitPolicy> _policies;

        public RequestRateLimitMiddleware(RequestDelegate next, RequestRateLimitConfiguration rateLimitConfiguration)
        {
            _next = next;
            _rateLimitConfiguration = rateLimitConfiguration;
            _policies = new ConcurrentDictionary<string, AsyncRateLimitPolicy>();
        }

        private AsyncRateLimitPolicy GetRateLimitPolicy(string route, string method)
        {
            //根据路由获取限流策略
            var rule = _rateLimitConfiguration.Rules.FirstOrDefault(a =>
                a.Method.ToUpper() == method.ToUpper() && Regex.IsMatch(route.ToLower(), a.Route,
                    RegexOptions.IgnoreCase | RegexOptions.Compiled));

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
                    return _policies.GetOrAdd(key,
                        _ => Policy.RateLimitAsync(rule.NumberOfRequests, TimeSpan.FromSeconds(rule.PeriodInSeconds),
                            rule.MaxBurst));
                }
            }

            return null;

        }

        public async Task InvokeAsync(HttpContext context)
        {
            //获取限流策略
            var policy = GetRateLimitPolicy(context.Request.Path.Value, context.Request.Method);

            //不为空则执行限流
            if (policy != null)
            {
                try
                {
                    await policy.ExecuteAsync(() => _next(context));
                }
                catch (RateLimitRejectedException limitRejectedException)
                {
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.Response.Headers.Add("Retry-After", limitRejectedException.RetryAfter.ToString());
                    await context.Response.WriteAsync(
                        $"请求太快, 请{limitRejectedException.RetryAfter.TotalMilliseconds}毫秒后重试");
                }
            }
            else
            {
                await _next(context);
            }
        }
    }
}