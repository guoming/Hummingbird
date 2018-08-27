using System;
using System.Collections.Generic;

namespace Hummingbird.Extersions.Cache
{
    public interface IHummingbirdCache<T>
    {
        void Add(string key, T value, TimeSpan ttl, string region);
        bool Exists(string key, string region);
        T Get(string key, string region);
        void ClearRegion(string region);
        bool Delete(string key, string region);
    }
}
