namespace Hummingbird.Example.DTO
{
    public interface IApiResponse
    {
      
        string code { get;  }

        string message { get; set; }

    }

    /// <summary>
    /// API返回消息基类
    /// </summary>
    public class ApiResponse: IApiResponse
    {
        public string code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string message { get; set; } 
    }

    public class ApiResponse<T> : IApiResponse
    {
        public string code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string message { get; set; }

        public T data { get; set; }
    }
}