using System;
using System.Threading.Tasks;

namespace Hummingbird.Idempotency
{
    public class CacheRequestManager : IRequestManager
    {
        Hummingbird.Cache.IHummingbirdCache<object> _cacheManager;
        Hummingbird.Idempotency.IIdempotencyOption _option;

        public CacheRequestManager(
            Hummingbird.Idempotency.IIdempotencyOption option,
            Hummingbird.Cache.IHummingbirdCache<object> cacheManager)
        {
            _option = option ?? throw new ArgumentNullException(nameof(option));
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        }
        

        public ClientRequest Find(string Id)
        {
            var obj= _cacheManager.Get(Id, _option.IdempotencyRegion);
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
                _cacheManager.Add(Id, cached, _option.Druation, _option.IdempotencyRegion);
            }

            return cached;

        }
    }
}
