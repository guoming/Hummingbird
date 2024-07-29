﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Extensions.Cacheing
{
    public interface ICacheManager
    {

        /// <summary>
        /// 缓存是否存在
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        bool KeyExists(string cacheKey);



        /// <summary>
        /// 移除缓存
        /// </summary>
        /// <param name="cacheKey"></param>
        bool RemoveCache(string cacheKey);

        /// <summary>
        /// 设置缓存的过期时间
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="cacheOutTime"></param>
        bool ExpireEntryAt(string cacheKey, TimeSpan cacheOutTime);

        #region String
        
        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        T StringGet<T>(string cacheKey);

        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        Task<T> StringGetAsync<T>(string cacheKey);

        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="cacheValue"></param>
        bool StringSet<T>(string cacheKey, T cacheValue);


        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="cacheValue"></param>
        Task<bool> StringSetAsync<T>(string cacheKey, T cacheValue);

        /// <summary>
        /// 设置相对过期缓存，可以加缓存过期时间
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="cacheValue"></param>
        /// <param name="expiresMinute"></param>
        bool StringSet<T>(string cacheKey, T cacheValue, TimeSpan cacheOutTime);


        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="cacheValue"></param>
        Task<bool> StringSetAsync<T>(string cacheKey, T cacheValue, TimeSpan cacheOutTime);

        /// <summary>
        /// 数字递减
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        double StringDecrement(string cacheKey, double val = 1);

        /// <summary>
        /// 数字递增
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        double StringIncrement(string cacheKey, double val = 1);

        /// <summary>
        /// 数字递减
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        Task<double> StringDecrementAsync(string cacheKey, double val = 1);

        /// <summary>
        /// 数字递增
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        Task<double> StringIncrementAsync(string cacheKey, double val = 1);
    
        #endregion

        #region LOCK

        /// <summary>
        /// 设置Key的时间
        /// </summary>
        /// <param name="key">redis key</param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        bool LockTake(string key, string lockValue, TimeSpan expiry);

        /// <summary>
        /// 设置Key的时间
        /// </summary>
        /// <param name="key">redis key</param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        string LockQuery(string key);

        /// <summary>
        /// 设置Key的时间
        /// </summary>
        /// <param name="key">redis key</param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        bool LockRelease(string key, string lockValue);
        #endregion

        #region Publish&Subscrbe

        /// <summary>
        /// 发布一个事件
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        long Publish<T>(string channelId, T msg);

        /// <summary>
        /// 订阅一个事件
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        void Subscribe<T>(string channelId, Action<T> handler);

        /// <summary>
        /// 订阅一个事件
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        void Subscribe(string channelId, Action<object> handler);
        #endregion

        #region Hash

        double HashIncrement(string cacheKey, string dataKey, double value = 1);
        Task<double> HashIncrementAsync(string cacheKey, string dataKey, double value = 1);
        double HashDecrement(string cacheKey, string dataKey, double value = 1);
        Task<double> HashDecrementAsync(string cacheKey, string dataKey, double value = 1);

        List<T> HashKeys<T>(string cacheKey);
        Task<List<T>> HashKeysAsync<T>(string cacheKey);

        T HashGet<T>(string cacheKey, string dataKey);
        Task<T> HashGetAsync<T>(string cacheKey, string dataKey);

        IDictionary<string, T> HashGetAll<T>(string cacheKey);
        Task<IDictionary<string, T>> HashGetAllAsync<T>(string cacheKey);

        bool HashKeys<T>(string cacheKey, string dataKey, T value);
        Task<bool> HashKeysAsync<T>(string cacheKey, string dataKey, T value);
        #endregion

        #region List
        
        /// <summary>
        /// 出栈
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="dbNum"></param>
        /// <returns></returns>
        T ListLeftPop<T>(string cacheKey);

        /// <summary>
        /// 出栈
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="dbNum"></param>
        /// <returns></returns>
        Task<T> ListLeftPopAsync<T>(string cacheKey);

        
        /// <summary>
        /// 入栈
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="value"></param>
        /// <param name="dbNum"></param>
        void ListLeftPush<T>(string cacheKey, T value);

        /// <summary>
        /// 入栈
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="value"></param>
        /// <param name="dbNum"></param>
        Task ListLeftPushAsync<T>(string cacheKey, T value);

        
        /// <summary>
        /// 获取列表长度
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="dbNum"></param>
        /// <returns></returns>
        long ListLength(string cacheKey);
        
              
        /// <summary>
        /// 获取列表长度
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="dbNum"></param>
        /// <returns></returns>
        Task<long> ListLengthAsync(string cacheKey);

        

        /// <summary>
        /// 获取列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="dbNum"></param>
        /// <returns></returns>
        List<T> ListRange<T>(string cacheKey);
        
        
        /// <summary>
        /// 获取列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="dbNum"></param>
        /// <returns></returns>
        Task<List<T>> ListRangeAsync<T>(string cacheKey);

        /// <summary>
        /// 移除一个元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="value"></param>
        /// <param name="dbNum"></param>
        void ListRemove<T>(string cacheKey, T value);
        
        /// <summary>
        /// 移除一个元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="value"></param>
        /// <param name="dbNum"></param>
        Task ListRemoveAsync<T>(string cacheKey, T value);

        /// <summary>
        /// 入队列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="value"></param>
        /// <param name="dbNum"></param>
        void ListRightPush<T>(string cacheKey, T value);
        
        /// <summary>
        /// 入队列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="value"></param>
        /// <param name="dbNum"></param>
        Task ListRightPushAsync<T>(string cacheKey, T value);

        /// <summary>
        /// 出队列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="value"></param>
        /// <param name="dbNum"></param>
        T ListRightPush<T>(string cacheKey);
        
        /// <summary>
        /// 出队列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="value"></param>
        /// <param name="dbNum"></param>
        Task<T> ListRightPushAsync<T>(string cacheKey);

        /// <summary>
        /// 出队列后写入另外一个队列（原子操作）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="dbNum"></param>
        /// <returns></returns>
        T ListRightPopLeftPush<T>(string source, string destination);
        
        
        
        /// <summary>
        /// 出队列后写入另外一个队列（原子操作）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="dbNum"></param>
        /// <returns></returns>
        Task<T> ListRightPopLeftPushAsync<T>(string source, string destination);
        
        #endregion

        #region Set

        bool SetAdd<T>(string key, T value);
        Task<bool> SetAddAsync<T>(string key, T value);

        bool SetContains<T>(string key, T value);
        Task<bool> SetContainsAsync<T>(string key, T value);

        long SetLength(string key);
        Task<long> SetLengthAsync(string key);

        List<T> SetMembers<T>(string key);
        Task<List<T>> SetMembersAsync<T>(string key);

        T SetPop<T>(string key);
        Task<T> SetPopAsync<T>(string key);

        T SetRandomMember<T>(string key);
        Task<T> SetRandomMemberAsync<T>(string key);

        List<T> SetRandomMembers<T>(string key, long count);
        Task<List<T>> SetRandomMembersAsync<T>(string key, long count);

        bool SetRemove<T>(string key, T value);
        Task<bool> SetRemoveAsync<T>(string key, T value);

        long SetRemove<T>(string key, T[] values);
        Task<long> SetRemoveAsync<T>(string key, T[] values);
        #endregion

        #region Execute
        dynamic Execute(string script, params object[] objs);

        Task<dynamic> ExecuteAsync(string script, params object[] objs);
        #endregion
    }
}
