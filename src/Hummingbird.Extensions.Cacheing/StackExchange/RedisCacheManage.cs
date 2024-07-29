﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using StackExchange.Redis;
using System.Threading;

namespace Hummingbird.Extensions.Cacheing.StackExchangeImplement
{

    class RedisCacheManage : ICacheManager
    {


        #region 全局变量
        private static object _syncCreateInstance = new Object();

        private static object _syncCreateClient = new object();

        private static bool _supportSentinal = false;

        private static string _KeyPrefix = "";


        //虚拟节点数量
        private static readonly int _VIRTUAL_NODE_COUNT = 1024;

        //Redis集群分片存储定位器
        private static KetamaHash.KetamaNodeLocator _Locator;

        private static Dictionary<string, ConfigurationOptions> _clusterConfigOptions = new Dictionary<string, ConfigurationOptions>();

        private static Dictionary<string, Dictionary<int, LoadBalancers.ILoadBalancer<RedisClientHelper>>> _nodeClients = new Dictionary<string, Dictionary<int, LoadBalancers.ILoadBalancer<RedisClientHelper>>>();
        #endregion
        
        #region 实例变量
        private readonly int _DbNum = 0;
        private readonly int _NumberOfConnections = 10;

        #endregion

        #region 构造函数
        private RedisCacheManage(int DbNum=0, int NumberOfConnections=10)
        {
            this._DbNum = DbNum;
            this._NumberOfConnections = NumberOfConnections;
            
        }

        /// <summary>
        /// 创建链接池管理对象
        /// </summary>
        public static ICacheManager Create(StackExchange.RedisCacheConfig config)
        {
           
            ThreadPool.SetMaxThreads(10000, 10000);
            ThreadPool.SetMinThreads(10000, 10000);
            if (string.IsNullOrEmpty(config.KeyPrefix))
            { 
                _KeyPrefix =string.Empty;
            }
            else
            { 
                _KeyPrefix = config.KeyPrefix + ":";
               
            }
            if (_Locator == null)
            {
                lock (_syncCreateInstance)
                {
                    if (_Locator == null)
                    {
                        if (string.IsNullOrEmpty(config.SentineList) || !_supportSentinal)
                        {
                            //Redis服务器相关配置
                            string writeServerList = config.WriteServerList;
                            string readServerList = config.ReadServerList;
                            var writeServerArray = RedisCacheConfigHelper.SplitString(writeServerList, ",").ToList();
                            var readServerArray = RedisCacheConfigHelper.SplitString(readServerList, ",").ToList();
                            var Nodes = new List<string>();

                            //只有一个写,多个读的情况
                            /*
                             * Redis.ReadServerList	    192.168.100.51:16378,192.168.100.51:26378
                               Redis.WriteServerList	192.168.100.51:6378
                             */
                            if (writeServerArray.Count == 1)
                            {
                                var writeServer = writeServerArray[0];
                                var NodeName = writeServerArray[0];

                                if (!_clusterConfigOptions.ContainsKey(NodeName))
                                {
                                   ConfigurationOptions configOption = new ConfigurationOptions();
                                    configOption.ServiceName = NodeName;
                                    configOption.Password = config.Password;
                                    configOption.AbortOnConnectFail = false;
                                    configOption.DefaultDatabase = config.DBNum;
                                    configOption.Ssl = config.Ssl;

                                    foreach (var ipAndPort in writeServerArray.Union(readServerArray))
                                    {
                                        configOption.EndPoints.Add(ipAndPort);
                                    }

                                    _clusterConfigOptions.Add(writeServer, configOption);
                                }

                                Nodes.Add(NodeName);
                            }
                            /*
                             * 多个写和多个读
                              Redis.ReadServerList	    master-6378@192.168.100.51:16378,master-6379@192.168.100.51:16379,master-6380@192.168.100.51:16380,master-6381@192.168.100.51:16381,master-6382@192.168.100.51:16382,master-6378@192.168.100.51:26378,master-6379@192.168.100.51:26379,master-6380@192.168.100.51:26380,master-6381@192.168.100.51:26381,master-6382@192.168.100.51:26382
                              Redis.WriteServerList	    master-6378@192.168.100.51:6378,master-6379@192.168.100.51:6379,master-6380@192.168.100.51:6380,master-6381@192.168.100.51:6381,master-6382@192.168.100.51:6382         
                            */
                            else
                            {
                                for (int i = 0; i < writeServerArray.Count; i++)
                                {
                                    //存在多个Master服务器的时候
                                    if (writeServerArray[i].IndexOf("@") > 0)
                                    {
                                        //集群名称()
                                        var NodeName = RedisCacheConfigHelper.GetServerClusterName(writeServerArray[i]);
                                        //主服务器名称
                                        var masterServer = RedisCacheConfigHelper.GetServerHost(writeServerArray[i]);

                                        //主服务器列表
                                        var masterServerIPAndPortArray = RedisCacheConfigHelper.GetServerList(config.WriteServerList, NodeName);
                                        //从服务器列表
                                        var slaveServerIPAndPortArray = RedisCacheConfigHelper.GetServerList(config.ReadServerList, NodeName);

                                        //当前集群的配置不存在
                                        if (!_clusterConfigOptions.ContainsKey(NodeName))
                                        {
                                            ConfigurationOptions configOption = new ConfigurationOptions();
                                            configOption.ServiceName = NodeName;
                                            configOption.Password = config.Password;
                                            configOption.AbortOnConnectFail = false;
                                            configOption.DefaultDatabase = config.DBNum;
                                            configOption.Ssl = config.Ssl;
                                            configOption.ConnectTimeout = 15000;
                                            configOption.SyncTimeout = 5000;
                                            configOption.ResponseTimeout = 15000;
                                           
                                            foreach (var ipAndPort in masterServerIPAndPortArray.Union(slaveServerIPAndPortArray).Distinct())
                                            {
                                                configOption.EndPoints.Add(RedisCacheConfigHelper.GetIP(ipAndPort), RedisCacheConfigHelper.GetPort(ipAndPort));
                                            }

                                            _clusterConfigOptions.Add(NodeName, configOption);
                                        }

                                        Nodes.Add(NodeName);
                                    }
                                    else
                                    {
                                        //192.168.10.100:6379
                                        var NodeName = writeServerArray[i];

                                        if (!_clusterConfigOptions.ContainsKey(NodeName))
                                        {
                                            
                                            ConfigurationOptions configOption = new ConfigurationOptions();
                                            configOption.ServiceName = NodeName;
                                            configOption.Password = config.Password;
                                            configOption.AbortOnConnectFail = false;
                                            configOption.DefaultDatabase = config.DBNum;
                                            configOption.Ssl = config.Ssl;
                                            configOption.ConnectTimeout = 15000;
                                            configOption.SyncTimeout = 5000;
                                            configOption.ResponseTimeout = 15000;
                                

                                            configOption.EndPoints.Add(RedisCacheConfigHelper.GetIP(NodeName), RedisCacheConfigHelper.GetPort(NodeName));
                                            _clusterConfigOptions.Add(NodeName, configOption);
                                        }

                                        Nodes.Add(NodeName);
                                    }
                                }
                            }

                            _Locator = new KetamaHash.KetamaNodeLocator(Nodes, _VIRTUAL_NODE_COUNT);
                        }
                        else
                        {
                            List<string> sentinelMasterNameList = new List<string>();
                            List<string> sentinelServerHostList = new List<string>();
                            var SentineList = RedisCacheConfigHelper.SplitString(config.SentineList, ",").ToList();
                            for (int i = 0; i < SentineList.Count; i++)
                            {
                                var args = RedisCacheConfigHelper.SplitString(SentineList[i], "@").ToList();

                                var ServiceName = args[0];
                                var hostName = args[1];
                                var endPoint = RedisCacheConfigHelper.SplitString(hostName, ":").ToList();
                                var ip = endPoint[0]; //IP
                                var port = int.Parse(endPoint[1]); //端口 

                                sentinelMasterNameList.Add(ServiceName);
                                sentinelServerHostList.Add(hostName);
                                if (!_clusterConfigOptions.ContainsKey(hostName))
                                {
                                    //连接sentinel服务器
                                    ConfigurationOptions sentinelConfig = new ConfigurationOptions();
                                    sentinelConfig.ServiceName = ServiceName;
                                    sentinelConfig.EndPoints.Add(ip, port);
                                    sentinelConfig.AbortOnConnectFail = false;
                                    sentinelConfig.DefaultDatabase = config.DBNum;
                                    sentinelConfig.TieBreaker = ""; //这行在sentinel模式必须加上
                                    sentinelConfig.CommandMap = CommandMap.Sentinel;
                                    sentinelConfig.DefaultVersion = new Version(3, 0);
                                    _clusterConfigOptions[hostName] = sentinelConfig;
                                }
                                else
                                {
                                   ConfigurationOptions sentinelConfig = _clusterConfigOptions[hostName] as ConfigurationOptions;
                                    sentinelConfig.EndPoints.Add(ip, port);
                                    _clusterConfigOptions[hostName] = sentinelConfig;
                                }
                            }

                            //初始化Reds分片定位器
                            _Locator = new KetamaHash.KetamaNodeLocator(sentinelServerHostList, _VIRTUAL_NODE_COUNT);
                        }
                    }
                }
            }

            
            return new RedisCacheManage(config.DBNum, config.NumberOfConnections);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 根据缓存名称定位需要访问的缓存服务器
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        private RedisClientHelper GetPooledClientManager(string cacheKey)
        {
          

                var nodeName = _Locator.GetPrimary(_KeyPrefix + cacheKey);

                if (_nodeClients.ContainsKey(nodeName))
                {

                    var dbs = _nodeClients[nodeName];

                    if (dbs.ContainsKey(_DbNum))
                    {
                        return dbs[_DbNum].Lease();
                    }
                    else
                    {
                        return GetClientHelper(nodeName);
                    }
                }
                else
                {

                    return GetClientHelper(nodeName);
                }
         

        }


        private RedisClientHelper GetClientHelper(string nodeName)
        {
           
                lock (_syncCreateClient)
                {
                    if (_nodeClients.ContainsKey(nodeName))
                    {
                        var dbs = _nodeClients[nodeName];

                        if (!dbs.ContainsKey(_DbNum))
                        {
                            dbs[_DbNum] = GetConnectionLoadBalancer(nodeName);
                        }
                    }
                    else
                    {
                        var node = new Dictionary<int, LoadBalancers.ILoadBalancer<RedisClientHelper>>();
                        node[_DbNum] = GetConnectionLoadBalancer(nodeName);
                        _nodeClients[nodeName] = node;
                    }

                    return _nodeClients[nodeName][_DbNum].Lease();
                }
            
        }

        private LoadBalancers.ILoadBalancer<RedisClientHelper> GetConnectionLoadBalancer(string nodeName)
        {
            var factory = new LoadBalancers.DefaultLoadBalancerFactory<RedisClientHelper>();

            return factory.Get(() =>
            {
                var clients = new List<RedisClientHelper>();
                for (int i = 0; i < this._NumberOfConnections; i++)
                {
                    clients.Add(new RedisClientHelper(_DbNum, _clusterConfigOptions[nodeName], _KeyPrefix));
                }
                return clients;
            });
        }

        #endregion

        #region 接口实现

        #region key
        /// <summary>
        /// 缓存是否存在
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public bool KeyExists(string cacheKey)
        {
            if (!string.IsNullOrEmpty(cacheKey))
            {
                return GetPooledClientManager(cacheKey).KeyExists(cacheKey);
            }
            return false;
        }
        
        
        /// <summary>
        /// 移除缓存
        /// </summary>
        /// <param name="cacheKey"></param>
        public bool RemoveCache(string cacheKey)
        {
            return GetPooledClientManager(cacheKey).KeyDelete(cacheKey);
        }

        /// <summary>
        /// 设置缓存的过期时间
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="cacheOutTime"></param>
        public bool ExpireEntryAt(string cacheKey, TimeSpan cacheOutTime)
        {
            return GetPooledClientManager(cacheKey).KeyExpire(cacheKey, cacheOutTime);
        }
        #endregion

        #region String
        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public T StringGet<T>(string cacheKey)
        {
            T cacheData = default(T);
            if (!string.IsNullOrEmpty(cacheKey))
            {
                cacheData = GetPooledClientManager(cacheKey).StringGet<T>(cacheKey);
            }
            return cacheData;
        }

        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public async Task<T> StringGetAsync<T>(string cacheKey)
        {
            T cacheData = default(T);
            if (!string.IsNullOrEmpty(cacheKey))
            {
                return await GetPooledClientManager(cacheKey).StringGetAsync<T>(cacheKey);
            }
            return cacheData;
        }

        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="cacheValue"></param>
        public bool StringSet<T>(string cacheKey, T cacheValue)
        {
            if (!string.IsNullOrEmpty(cacheKey) && cacheValue != null)
            {
                return GetPooledClientManager(cacheKey).StringSet<T>(cacheKey, cacheValue);


            }

            return false;
        }

        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="cacheValue"></param>
        public async Task<bool> StringSetAsync<T>(string cacheKey, T cacheValue)
        {
            if (!string.IsNullOrEmpty(cacheKey) && cacheValue != null)
            {
                return await GetPooledClientManager(cacheKey).StringSetAsync<T>(cacheKey, cacheValue);
            }

            return false;
        }

        /// <summary>
        /// 设置缓存，可以加缓存过期时间
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="cacheValue"></param>
        /// <param name="cacheOutTime"></param>
        public bool StringSet<T>(string cacheKey, T cacheValue, TimeSpan cacheOutTime)
        {
            if (!string.IsNullOrEmpty(cacheKey) && cacheValue != null)
            {
                if (cacheOutTime != null)
                {
                    return GetPooledClientManager(cacheKey).StringSet<T>(cacheKey, cacheValue, cacheOutTime);
                }
                else
                {
                    return StringSet<T>(cacheKey, cacheValue);
                }
            }
            return false;
        }


        /// <summary>
        /// 设置缓存，可以加缓存过期时间
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="cacheValue"></param>
        /// <param name="cacheOutTime"></param>
        public async Task<bool> StringSetAsync<T>(string cacheKey, T cacheValue, TimeSpan cacheOutTime)
        {
            if (!string.IsNullOrEmpty(cacheKey) && cacheValue != null)
            {
                if (cacheOutTime != null)
                {
                    return await GetPooledClientManager(cacheKey).StringSetAsync<T>(cacheKey, cacheValue, cacheOutTime);
                }
                else
                {
                    return await StringSetAsync<T>(cacheKey, cacheValue);
                }
            }
            return false;
        }


        /// <summary>
        /// 数字递减
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public double StringDecrement(string cacheKey, double val = 1)
        {
            return GetPooledClientManager(cacheKey).StringDecrement(cacheKey);
        }


        /// <summary>
        /// 数字递减
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public async Task<double> StringDecrementAsync(string cacheKey, double val = 1)
        {
            return await GetPooledClientManager(cacheKey).StringDecrementAsync(cacheKey);
        }

        /// <summary>
        /// 数字递增
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public double StringIncrement(string cacheKey, double val = 1)
        {
            return GetPooledClientManager(cacheKey).StringIncrement(cacheKey);
        }


        /// <summary>
        /// 数字递增
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public async Task<double> StringIncrementAsync(string cacheKey, double val = 1)
        {
            return await GetPooledClientManager(cacheKey).StringIncrementAsync(cacheKey);
        }
        
        #endregion

        #region 发布订阅

        /// <summary>
        /// 发布一个事件
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public long Publish<T>(string channelId, T msg)
        {
            return GetPooledClientManager(channelId).Publish<T>(channelId, msg);
        }


        /// <summary>
        /// 订阅一个事件
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public void Subscribe<T>(string channelId, Action<T> handler)
        {
            GetPooledClientManager(channelId).Subscribe<T>(channelId, (channel, value) => { handler(value); });
        }

        public void Subscribe(string channelId, Action<object> handler)
        {
            GetPooledClientManager(channelId).Subscribe(channelId, (channel, value) => { handler(value); });
        }

        #endregion
        
        #region Hash
        
        public double HashIncrement(string cacheKey, string dataKey, double value = 1)
        {
            return GetPooledClientManager(cacheKey).HashIncrement(cacheKey, dataKey, value);
        }

        public async Task<double> HashIncrementAsync(string cacheKey, string dataKey, double value = 1)
        {
            return await GetPooledClientManager(cacheKey).HashIncrementAsync(cacheKey, dataKey, value);
        }

        public double HashDecrement(string cacheKey, string dataKey, double value = 1)
        {
            return GetPooledClientManager(cacheKey).HashDecrement(cacheKey, dataKey, value);
        }

        public async Task<double> HashDecrementAsync(string cacheKey, string dataKey, double value = 1)
        {
            return await GetPooledClientManager(cacheKey).HashDecrementAsync(cacheKey, dataKey, value);
        }

        public List<T> HashKeys<T>(string cacheKey)
        {
            return GetPooledClientManager(cacheKey).HashKeys<T>(cacheKey);
        }

        public async Task<List<T>> HashKeysAsync<T>(string cacheKey)
        {
            return await GetPooledClientManager(cacheKey).HashKeysAsync<T>(cacheKey);
        }

        public T HashGet<T>(string cacheKey, string dataKey)
        {
            return GetPooledClientManager(cacheKey).HashGet<T>(cacheKey, dataKey);
        }
        
        public async Task<T> HashGetAsync<T>(string cacheKey, string dataKey)
        {
            return await GetPooledClientManager(cacheKey).HashGetAsync<T>(cacheKey, dataKey);
        }

        public IDictionary<string,T> HashGetAll<T>(string cacheKey)
        {
            return GetPooledClientManager(cacheKey).HashGetAll<T>(cacheKey);
        }

        public async Task<IDictionary<string, T>> HashGetAllAsync<T>(string cacheKey)
        {
            return await GetPooledClientManager(cacheKey).HashGetAllAsync<T>(cacheKey);
        }
        
        public bool HashKeys<T>(string cacheKey, string dataKey, T value)
        {
            return GetPooledClientManager(cacheKey).HashSet(cacheKey, dataKey, value);
        }

        public async Task<bool> HashKeysAsync<T>(string cacheKey, string dataKey, T value)
        {
            return await GetPooledClientManager(cacheKey).HashSetAsync(cacheKey, dataKey, value);
        }
        #endregion
        
        #region Lock

        public bool LockTake(string cacheKey, string value, TimeSpan expire)
        {
            return GetPooledClientManager(cacheKey).LockTake(cacheKey, value, expire);
        }

        public bool LockRelease(string cacheKey, string value)
        {
            return GetPooledClientManager(cacheKey).LockRelease(cacheKey, value);
        }

        public string LockQuery(string cacheKey)
        {
            return GetPooledClientManager(cacheKey).LockQuery(cacheKey);
        }
        #endregion Lock

        #region List

        /// <summary>
        /// 出栈
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="dbNum"></param>
        /// <returns></returns>
        public T ListLeftPop<T>(string cacheKey)
        {
            return GetPooledClientManager(cacheKey).ListLeftPop<T>(cacheKey);
        }

        public async Task<T> ListLeftPopAsync<T>(string cacheKey)
        {
            return await GetPooledClientManager(cacheKey).ListLeftPopAsync<T>(cacheKey);
        }

        /// <summary>
        /// 入栈
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="value"></param>
        /// <param name="dbNum"></param>
        public void ListLeftPush<T>(string cacheKey, T value)
        {
            GetPooledClientManager(cacheKey).ListLeftPush<T>(cacheKey, value);
        }

        public async Task ListLeftPushAsync<T>(string cacheKey, T value)
        {
            await GetPooledClientManager(cacheKey).ListLeftPushAsync<T>(cacheKey, value);

        }

        /// <summary>
        /// 获取列表长度
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="dbNum"></param>
        /// <returns></returns>
        public long ListLength(string cacheKey)
        {
            return GetPooledClientManager(cacheKey).ListLength(cacheKey);
        }

        public async Task<long> ListLengthAsync(string cacheKey)
        {
            return await GetPooledClientManager(cacheKey).ListLengthAsync(cacheKey);

        }

        /// <summary>
        /// 获取列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="dbNum"></param>
        /// <returns></returns>
        public List<T> ListRange<T>(string cacheKey)
        {
            return GetPooledClientManager(cacheKey).ListRange<T>(cacheKey);
        }

        public async Task<List<T>> ListRangeAsync<T>(string cacheKey)
        {
            return await GetPooledClientManager(cacheKey).ListRangeAsync<T>(cacheKey);
        }

        /// <summary>
        /// 移除一个元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="value"></param>
        /// <param name="dbNum"></param>
        public void ListRemove<T>(string cacheKey, T value)
        {
            GetPooledClientManager(cacheKey).ListRemove<T>(cacheKey, value);
        }

        public async Task ListRemoveAsync<T>(string cacheKey, T value)
        {
            await GetPooledClientManager(cacheKey).ListRemoveAsync<T>(cacheKey, value);
        }


        /// <summary>
        /// 入队列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="value"></param>
        /// <param name="dbNum"></param>
        public void ListRightPush<T>(string cacheKey, T value)
        {
            GetPooledClientManager(cacheKey).ListRightPush<T>(cacheKey, value);
        }

        public async Task ListRightPushAsync<T>(string cacheKey, T value)
        {
            await GetPooledClientManager(cacheKey).ListRightPushAsync<T>(cacheKey, value);
        }

        /// <summary>
        /// 出队列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="value"></param>
        /// <param name="dbNum"></param>
        public T ListRightPush<T>(string cacheKey)
        {
            return GetPooledClientManager(cacheKey).ListRightPop<T>(cacheKey);
        }

        public async Task<T> ListRightPushAsync<T>(string cacheKey)
        {
            return await GetPooledClientManager(cacheKey).ListRightPopAsync<T>(cacheKey);

        }

        /// <summary>
        /// 出队列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="value"></param>
        /// <param name="dbNum"></param>
        public T ListRightPopLeftPush<T>(string sourceCacheKey, string destCacheKey)
        {
            return GetPooledClientManager(sourceCacheKey).ListRightPopLeftPush<T>(sourceCacheKey, destCacheKey);
        }

        public async Task<T> ListRightPopLeftPushAsync<T>(string sourceCacheKey, string destCacheKey)
        {
            return await GetPooledClientManager(sourceCacheKey).ListRightPopLeftPushAsync<T>(sourceCacheKey, destCacheKey);

        }

        #endregion

        #region Set

        public bool SetAdd<T>(string key, T value)
        {
            return GetPooledClientManager(key).SetAdd(key, value);
        }

        public Task<bool> SetAddAsync<T>(string key, T value)
        {
            return GetPooledClientManager(key).SetAddAsync(key, value);
        }

        public bool SetContains<T>(string key, T value)
        {
            return GetPooledClientManager(key).SetContains(key, value);
        }

        public async Task<bool> SetContainsAsync<T>(string key, T value)
        {
         
            return await GetPooledClientManager(key).SetContainsAsync(key, value);
        }

        public long SetLength(string key)
        {
            return GetPooledClientManager(key).SetLength(key);
        }

        public async Task<long> SetLengthAsync(string key)
        {
            return await GetPooledClientManager(key).SetLengthAsync(key);
        }

        public List<T> SetMembers<T>(string key)
        {
            return GetPooledClientManager(key).SetMembers<T>(key);
        }

        public async Task<List<T>> SetMembersAsync<T>(string key)
        {
            return await GetPooledClientManager(key).SetMembersAsync<T>(key);

        }

        public T SetPop<T>(string key)
        {
            return GetPooledClientManager(key).SetPop<T>(key);
        }

        public async Task<T> SetPopAsync<T>(string key)
        {
            return await GetPooledClientManager(key).SetPopAsync<T>(key);

        }

        public T SetRandomMember<T>(string key)
        {
            return GetPooledClientManager(key).SetRandomMember<T>(key);
        }

        public async Task<T> SetRandomMemberAsync<T>(string key)
        {
            return await GetPooledClientManager(key).SetRandomMemberAsync<T>(key);

        }

        public List<T> SetRandomMembers<T>(string key, long count)
        {
            return GetPooledClientManager(key).SetRandomMembers<T>(key, count);
        }

        public async Task<List<T>> SetRandomMembersAsync<T>(string key, long count)
        {
            return  await GetPooledClientManager(key).SetRandomMembersAsync<T>(key, count);

        }

        public bool SetRemove<T>(string key, T value)
        {
            return GetPooledClientManager(key).SetRemove(key, value);
        }

        public async Task<bool> SetRemoveAsync<T>(string key, T value)
        {
            return await GetPooledClientManager(key).SetRemoveAsync(key, value);

        }

        public long SetRemove<T>(string key, T[] values)
        {
            return GetPooledClientManager(key).SetRemove(key, values);
        }

        public async Task<long> SetRemoveAsync<T>(string key, T[] values)
        {
            return await GetPooledClientManager(key).SetRemoveAsync(key, values);

        }

        public dynamic Execute(string command, params object[] objs)
        {
            return GetPooledClientManager(command).Execute(command, objs);
        }

        public Task<dynamic> ExecuteAsync(string command, params object[] objs)
        {
            return GetPooledClientManager(command).ExecuteAsync(command, objs);
        }

        #endregion

        #endregion
    }
}