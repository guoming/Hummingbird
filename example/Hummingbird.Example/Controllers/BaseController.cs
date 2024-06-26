using System.Threading;
using System.Threading.Tasks;
using Hummingbird.Example.DTO;
using Microsoft.AspNetCore.Mvc;

namespace Hummingbird.Example.Controllers
{
    /// <summary>
    /// 文件上传处理程序
    /// </summary>
    public abstract class BaseController : Controller
    {
      
        protected ApiResponse OK(string message="")
        {
            return new ApiResponse() { code ="1", message = message };
        }

        protected ApiResponse<T> OK<T>(T data, string message = "")
        {
            return new ApiResponse<T>() { code = "1", message =  message, data = data };
        }

        protected ApiResponse Error(string message = "")
        {
            return new ApiResponse() { code = "0", message = message };
        }

        protected ApiResponse<T> Error<T>(string message = "")
        {
            return new ApiResponse<T>() { code="0", message = message };
        }
    }
}