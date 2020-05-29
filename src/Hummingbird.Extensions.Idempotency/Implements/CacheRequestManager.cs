using Hummingbird.Extensions.Cache;
using System;
using System.Threading.Tasks;

namespace Hummingbird.Extensions.Idempotency
{
    public class CacheRequestManager : IRequestManager
    {
        IHummingbirdCache<object> _cacheManager;
        Hummingbird.Extensions.Idempotency.IIdempotencyOption _option;

        public CacheRequestManager(
            Hummingbird.Extensions.Idempotency.IIdempotencyOption option,
            IHummingbirdCache<object> cacheManager)
        {
            _option = option ?? throw new ArgumentNullException(nameof(option));
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        }
        

        public ClientRequest Find(string Id)
        {
            var obj= _cacheManager.Get(Id, _option.CacheRegion);
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
                _cacheManager.Add(Id, cached, _option.Druation, _option.CacheRegion);
            }

            return cached;

        }
    }
}
