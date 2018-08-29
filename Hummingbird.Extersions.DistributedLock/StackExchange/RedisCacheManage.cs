using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace Hummingbird.Extersions.DistributedLock.StackExchangeImplement
{

    class RedisCacheManage : ICacheManager
    {
        #region private

        #region 全局变量
        private static object _syncCreateInstance = new Object();

        private static object _syncCreateClient = new object();

        private static bool _supportSentinal = false;

        private static string _KeyPrefix = "";

        //虚拟节点数量
        private static readonly int _VIRTUAL_NODE_COUNT = 1024;

        //Redis集群分片存储定位器
        private static KetamaHash.KetamaNodeLocator _Locator;

        private static Dictionary<string, StackExchange.Redis.ConfigurationOptions> _clusterConfigOptions = new Dictionary<string, StackExchange.Redis.ConfigurationOptions>();

        private static Dictionary<string, Dictionary<int, RedisClientHelper>> _clients = new Dictionary<string, Dictionary<int, RedisClientHelper>>();
        #endregion

        #region 实例变量
        private int DbNum = 0;
        #endregion

        private RedisCacheManage(int DbNum)
        {
            this.DbNum = DbNum;
        }

        /// <summary>
        /// 创建链接池管理对象
        /// </summary>
        public static RedisCacheManage Create(RedisCacheConfig config)
        {
            _KeyPrefix = config.KeyPrefix + ":";

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
                                    StackExchange.Redis.ConfigurationOptions configOption = new StackExchange.Redis.ConfigurationOptions();
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
                                            StackExchange.Redis.ConfigurationOptions configOption = new StackExchange.Redis.ConfigurationOptions();
                                            configOption.ServiceName = NodeName;
                                            configOption.Password = config.Password;
                                            configOption.AbortOnConnectFail = false;
                                            configOption.DefaultDatabase = config.DBNum;
                                            configOption.Ssl = config.Ssl;
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
                                            
                                            StackExchange.Redis.ConfigurationOptions configOption = new StackExchange.Redis.ConfigurationOptions();
                                            configOption.ServiceName = NodeName;
                                            configOption.Password = config.Password;
                                            configOption.AbortOnConnectFail = false;
                                            configOption.DefaultDatabase = config.DBNum;
                                            configOption.Ssl = config.Ssl;
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
                                    StackExchange.Redis.ConfigurationOptions sentinelConfig = new StackExchange.Redis.ConfigurationOptions();
                                    sentinelConfig.ServiceName = ServiceName;
                                    sentinelConfig.EndPoints.Add(ip, port);
                                    sentinelConfig.AbortOnConnectFail = false;
                                    sentinelConfig.DefaultDatabase = config.DBNum;
                                    sentinelConfig.TieBreaker = ""; //这行在sentinel模式必须加上
                                    sentinelConfig.CommandMap = StackExchange.Redis.CommandMap.Sentinel;
                                    sentinelConfig.DefaultVersion = new Version(3, 0);
                                    _clusterConfigOptions[hostName] = sentinelConfig;
                                }
                                else
                                {
                                    StackExchange.Redis.ConfigurationOptions sentinelConfig = _clusterConfigOptions[hostName] as StackExchange.Redis.ConfigurationOptions;
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

            return new RedisCacheManage(config.DBNum);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 根据缓存名称定位需要访问的缓存服务器
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        RedisClientHelper GetPooledClientManager(string cacheKey)
        {
            var nodeName = _Locator.GetPrimary(_KeyPrefix + cacheKey);

            if (_clients.ContainsKey(nodeName))
            {
                var dbs = _clients[nodeName];

                if (dbs.ContainsKey(DbNum))
                {
                    return dbs[DbNum];
                }
                else
                {
                    return GetClientHelper(nodeName, _KeyPrefix);
                }
            }
            else
            {
                return GetClientHelper(nodeName, _KeyPrefix);
            }
        }

        RedisClientHelper GetClientHelper(string nodeName, string _KeyPrefix)
        {
            lock (_syncCreateClient)
            {
                RedisClientHelper client = null;

                if (_clients.ContainsKey(nodeName))
                {
                    var dbs = _clients[nodeName];

                    if (dbs.ContainsKey(DbNum))
                    {
                        client = dbs[DbNum];
                    }
                    else
                    {
                        client = new RedisClientHelper(DbNum, _clusterConfigOptions[nodeName], _KeyPrefix);
                        dbs[DbNum] = client;
                    }
                }
                else
                {
                    client = new RedisClientHelper(DbNum, _clusterConfigOptions[nodeName], _KeyPrefix);
                    var node = new Dictionary<int, RedisClientHelper>();
                    node[DbNum] = client;
                    _clients[nodeName] = node;
                }

                return client;
            }
        }

        #endregion

        #region 接口实现

        /// <summary>
        /// 缓存是否存在
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public bool KeyExists(string cacheKey)
        {
            if (!string.IsNullOrEmpty(cacheKey))
            {
                var value = GetPooledClientManager(cacheKey).StringGet(cacheKey);

                return value != null ? true : false;
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


        public double HashIncrement(string cacheKey, string dataKey, double value = 1)
        {
            return GetPooledClientManager(cacheKey).HashIncrement(cacheKey, dataKey, value);
        }

        public double HashDecrement(string cacheKey, string dataKey, double value = 1)
        {
            return GetPooledClientManager(cacheKey).HashDecrement(cacheKey, dataKey, value);
        }

        public List<T> HashKeys<T>(string cacheKey)
        {
            return GetPooledClientManager(cacheKey).HashKeys<T>(cacheKey);
        }


        public T HashGet<T>(string cacheKey, string dataKey)
        {
            return GetPooledClientManager(cacheKey).HashGet<T>(cacheKey, dataKey);
        }

        public bool HashKeys<T>(string cacheKey, string dataKey, T value)
        {
            return GetPooledClientManager(cacheKey).HashSet(cacheKey, dataKey, value);
        }


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

        #endregion


        #region Set

        public bool SetAdd<T>(string key, T value)
        {
            return GetPooledClientManager(key).SetAdd(key, value);
        }

        public bool SetContains<T>(string key, T value)
        {
            return GetPooledClientManager(key).SetContains(key, value);
        }

        public long SetLength(string key)
        {
            return GetPooledClientManager(key).SetLength(key);
        }

        public List<T> SetMembers<T>(string key)
        {
            return GetPooledClientManager(key).SetMembers<T>(key);
        }

        public T SetPop<T>(string key)
        {
            return GetPooledClientManager(key).SetPop<T>(key);
        }

        public T SetRandomMember<T>(string key)
        {
            return GetPooledClientManager(key).SetRandomMember<T>(key);
        }

        public List<T> SetRandomMembers<T>(string key, long count)
        {
            return GetPooledClientManager(key).SetRandomMembers<T>(key, count);
        }

        public bool SetRemove<T>(string key, T value)
        {
            return GetPooledClientManager(key).SetRemove(key, value);
        }

        public long SetRemove<T>(string key, T[] values)
        {
            return GetPooledClientManager(key).SetRemove(key, values);
        }

        #endregion

        #endregion
    }
}