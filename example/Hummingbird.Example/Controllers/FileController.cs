using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Hummingbird.Example.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ZT.RFS.Infrastructure.FileProvider;

namespace Hummingbird.Example.Controllers
{
    [ApiController]
    public class FileController: BaseController 
    {
        private readonly IConfiguration _configuration;
        private readonly IFileSystemBucket _fileSystemBucket;

        public FileController(
            IConfiguration configuration,
            IFileSystemBucket fileSystemBucket)
        {
            _configuration = configuration;
            _fileSystemBucket = fileSystemBucket;
        }

        private void EnsureDirExists(string path)
        {
            var directoryName = Path.GetDirectoryName(path);

            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
        }
   

        
        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("api/file/upload")]
        public async Task<IApiResponse> Upload(CancellationToken cancellationToken)
        {
            var rootPath = _configuration.GetSection("FileSystem:Oss:CacheLocalPath").Value;
            var files = HttpContextProvider.Current.Request.Form.Files;
        
            var fileUrlList = new List<string>();

            if (files != null && files.Any())
            {
                foreach (var file in files)
                {
                    var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName);
                    var fileDir = DateTime.UtcNow.ToString("yyyy/MM/dd/HH/");
                    var filePath = Path.Combine(fileDir, fileName);
                    
                    var localFilePath = Path.Combine(rootPath, filePath);
                    
                    EnsureDirExists(localFilePath);
                    
                    using (var fs = new FileStream(localFilePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fs, cancellationToken);
                        await fs.FlushAsync(cancellationToken);


                        await _fileSystemBucket.UploadAsync(filePath, localFilePath,true);
                        
                    }

                    fileUrlList.Add(filePath);
                }

            }
            
            return OK(fileUrlList);   
            
        }
        
    }
}