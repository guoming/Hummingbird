using Newtonsoft.Json;
using OpenTracing;
using OpenTracing.Propagation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Hummingbird.Extensions.Tracing
{
    public class Tracer : IDisposable
    {

        private readonly OpenTracing.IScope Scope;
        private bool status = true;

        public Tracer(string operaName)
        {
            Scope = OpenTracing.Util.GlobalTracer.Instance.BuildSpan(operaName).StartActive();
        }

        public Tracer(string operaName, string spanContextStr)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("uber-trace-id", spanContextStr);
            var callingHeaders = new TextMapExtractAdapter(dic);
            var extractedContext = OpenTracing.Util.GlobalTracer.Instance.Extract(BuiltinFormats.HttpHeaders, callingHeaders);
            Scope = OpenTracing.Util.GlobalTracer.Instance.BuildSpan(operaName).AsChildOf(extractedContext).StartActive();
        }

        public string GetCurrentContext()
        {
            TextMap textMap = new TextMap();
            OpenTracing.Util.GlobalTracer.Instance.Inject(Scope.Span.Context, BuiltinFormats.HttpHeaders, textMap);
            if (textMap.Any())
            {
                return textMap.FirstOrDefault().Value;
            }
            return "";
        }

        public void SetComponent(string name)
        {
            SetTag("component", name);

        }

        public void LogRequest(dynamic value)
        {
            Log("request", value);
        }

        public void LogResponse(dynamic value)
        {
            Log("response", value);
        }

        public void LogException(Exception ex)
        {
            var filed = new Dictionary<string, object>
            {
                ["stack"] = ex.StackTrace,
                ["error.kind"] = ex.Message,
                ["error.object"] = ex
            };
            Scope.Span.Log(filed);
            status = false;
        }

        public void SetTag(string key, dynamic value)
        {
            Scope.Span.SetTag(key, value);
        }

        public void Dispose()
        {
            SetTag("error", !this.status);
            Scope.Span.Finish();
            Scope.Dispose();
        }

        private void Log(string key, dynamic value)
        {
            if (value is string)
            {
                var dic = new Dictionary<string, object>
                {
                    [key] = value
                };
                Scope.Span.Log(dic);
            }
            else
            {
                var dic = new Dictionary<string, object>
                {
                    [key] =  SerializeObject(value) as object
                };
                Scope.Span.Log(dic);
            }
        }

        private string SerializeObject(object value)
        {
            return JsonConvert.SerializeObject(value);
        }

    }

    internal class TextMap : ITextMap
    {
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _values.GetEnumerator();
        }
        public void Set(string key, string value)
        {
            _values[key] = value;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _values.GetEnumerator();
        }
    }
}
