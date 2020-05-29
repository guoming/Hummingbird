using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Hummingbird.Extensions.Configuration.Json
{
    public class EnvironmentHelper
    {
        public static string GetEnvironmentVariable(string value)
        {
            var result = value;
            var paramList= GetParameters(result);
            foreach (var param in paramList)
            {
                if (!string.IsNullOrEmpty(param))
                {
                    var env = Environment.GetEnvironmentVariable(param);
                    result = result.Replace("${" + param + "|L}", env.ToLower());
                    result = result.Replace("${" + param + "|U}", env.ToUpper());
                    result = result.Replace("${" + param + "}", env);
                }
            }
            return result;
        }

        public static bool GetEnvironmentVariableAsBool(string name, bool defaultValue = false)
        {
            var str = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(str))
            {
                return defaultValue;
            }

            switch (str.ToLowerInvariant())
            {
                case "true":
                case "1":
                case "yes":
                    return true;
                case "false":
                case "0":
                case "no":
                    return false;
                default:
                    return defaultValue;
            }
        }

        private static List<string> GetParameters(string text)
        {
            var matchVale = new List<string>();
            string Reg = @"(?<=\${)[^\${}]*[UL]?(?=})";
            string key = string.Empty;
            foreach (Match m in Regex.Matches(text, Reg))
            {
                matchVale.Add(m.Value.TrimEnd('|','U', 'L'));
            }
            return matchVale;
        }
    }
}
