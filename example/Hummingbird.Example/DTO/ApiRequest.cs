using System.Collections.Generic;
using Newtonsoft.Json;

namespace Hummingbird.Example.DTO
{
    public abstract class ApiRequest
    {
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public virtual Dictionary<string, dynamic> GetRequestTags()
        {
            var tags = new Dictionary<string, dynamic>();
            return tags;
        }
    }
}