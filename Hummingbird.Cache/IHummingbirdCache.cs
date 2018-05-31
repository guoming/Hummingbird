using System;
using System.Collections.Generic;

namespace Hummingbird.Cache
{
    public interface IHummingbirdCache<T>
    {
        void Add(string key, T value, TimeSpan ttl, string region);
        void AddAndDelete(string key, T value, TimeSpan ttl, string region);
        T Get(string key, string region);
        void ClearRegion(string region);
    }
}
