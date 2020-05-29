using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Hummingbird.Extensions.DynamicRoute.Consul
{
    public class ConsulConfig
    {

        public ConsulConfig()
        { }

        /// <summary>
        /// 服务注册中心地址（默认:consul）
        /// </summary>
        public string SERVICE_REGISTRY_ADDRESS { get; set; } = "localhost";
        /// <summary>
        /// 服务注册中心端口(默认：8500)
        /// </summary>
        public string SERVICE_REGISTRY_PORT { get; set; } = "8500";
        /// <summary>
        /// 服务注册中心访问Token
        /// </summary>
        public string SERVICE_REGISTRY_TOKEN { get; set; } = "";
        /// <summary>
        /// 采用服务自注册模式(默认:false)
        /// </summary>
        public string SERVICE_SELF_REGISTER { get; set; } = "false";

        /// <summary>
        /// 服务Id
        /// </summary>
        public string SERVICE_ID { get; set; }
        /// <summary>
        /// 服务名称（服务发现名称）
        /// </summary>
        public string SERVICE_NAME { get; set; }
        /// <summary>
        /// 服务标签
        /// </summary>
        public string SERVICE_TAGS { get; set; } = "";

        /// <summary>
        /// 服务区域（隔离）
        /// </summary>
        public string SERVICE_REGION { get; set; } = "dc1";

        /// <summary>
        /// Http健康检查地址(默认:/healthcheck)
        /// </summary>
        public string SERVICE_80_CHECK_HTTP { get; set; } = "";

        /// <summary>
        /// 服务监控检查周期(默认:15s)
        /// </summary>
        public string SERVICE_80_CHECK_INTERVAL { get; set; } = "15s";
        /// <summary>
        /// 服务监控检查超时时间(默认:5s)
        /// </summary>
        public string SERVICE_80_CHECK_TIMEOUT { get; set; } = "5s";



        /// <summary>
        /// 服务监控检查周期(默认:15s)
        /// </summary>
        public string SERVICE_CHECK_INTERVAL { get; set; } = "15s";
        /// <summary>
        /// 服务监控检查超时时间(默认:5s)
        /// </summary>
        public string SERVICE_CHECK_TIMEOUT { get; set; } = "5s";

        /// <summary>
        /// TCP健康检查
        /// </summary>
        public string SERVICE_CHECK_TCP{ get; set; } = "";

        /// <summary>
        ///脚本健康检查
        /// </summary>
        public string SERVICE_CHECK_SCRIPT { get; set; } = "";

        /// <summary>
        /// TTL健康检查
        /// </summary>
        public int? SERVICE_CHECK_TTL { get; set; }

        public void WithConfig(ConsulConfig config)
        {
            this.SERVICE_REGISTRY_ADDRESS = config.SERVICE_REGISTRY_ADDRESS;
            this.SERVICE_REGISTRY_PORT = config.SERVICE_REGISTRY_PORT;
            this.SERVICE_SELF_REGISTER =config.SERVICE_SELF_REGISTER;
            this.SERVICE_REGION = config.SERVICE_REGION;
            this.SERVICE_NAME = config.SERVICE_NAME;
            this.SERVICE_80_CHECK_HTTP = config.SERVICE_80_CHECK_HTTP;
            this.SERVICE_80_CHECK_INTERVAL = config.SERVICE_80_CHECK_INTERVAL;
            this.SERVICE_80_CHECK_TIMEOUT =config.SERVICE_80_CHECK_TIMEOUT;
            this.SERVICE_TAGS = config.SERVICE_TAGS;
            this.SERVICE_CHECK_SCRIPT = config.SERVICE_CHECK_SCRIPT;
            this.SERVICE_CHECK_TCP = config.SERVICE_CHECK_TCP;
            this.SERVICE_CHECK_TTL = config.SERVICE_CHECK_TTL;
            this.SERVICE_CHECK_TIMEOUT = config.SERVICE_CHECK_TIMEOUT;
            this.SERVICE_CHECK_INTERVAL = config.SERVICE_CHECK_INTERVAL;
        }

    }
}
