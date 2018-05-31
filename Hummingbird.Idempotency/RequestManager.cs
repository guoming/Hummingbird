using System;
using System.Threading.Tasks;

namespace Hummingbird.Idempotency
{
    public class RequestManager : IRequestManager
    {
        Hummingbird.Cache.IHummingbirdCache<object> _cacheManager;


        public RequestManager(
            Hummingbird.Cache.IHummingbirdCache<object> cacheManager)
        {
            
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        }
        

        public ClientRequest Find(string Id)
        {
            var obj= _cacheManager.Get(Id, "Idempotency");
            return obj as ClientRequest;
        }

        public ClientRequest CreateRequestForCommand<T, R>(
            string Id,
            System.DateTime RequestTime,
            System.DateTime ResponseTime,
            T command,
            R response)
        {
            var cached = Find(Id);

            if (cached == null)
            {
                cached = new ClientRequest()
                {
                    Id = Id,
                    Name = typeof(T).Name,
                    RequestTime = RequestTime,
                    ResponseTime = ResponseTime,
                    Request = Newtonsoft.Json.JsonConvert.SerializeObject(command),
                    Response = Newtonsoft.Json.JsonConvert.SerializeObject(response)
                };
                _cacheManager.Add(Id, cached, TimeSpan.FromMinutes(5), "Idempotency");
            }

            return cached;

        }
    }
}
