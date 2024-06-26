using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Hummingbird.Extensions.FileSystem;

namespace ZT.RFS.Infrastructure.FileProvider
{
    public class OssStaticFileInfo: IFileInfo,IDisposable
    {
        private readonly IFileInfo fileInfo;

        public OssStaticFileInfo(IFileInfo fileInfo)
        {
            this.fileInfo = fileInfo;
        }
        
        public Stream CreateReadStream()
        {
            return fileInfo.CreateReadStream();
        }
        
        
        /// <summary>
        /// 创建读取流(异步)
        /// </summary>
        /// <returns></returns>
        public async Task<Stream> CreateReadStreamAsync()
        {
            if (fileInfo.GetType().Name == nameof(OssFileInfo))
            {
                await ((OssFileInfo)fileInfo).CreateReadStreamAsync();
            }
            
            return fileInfo.CreateReadStream();
        }

        public bool Exists => fileInfo.Exists;
        public long Length => fileInfo.Length;
        public string PhysicalPath => fileInfo.PhysicalPath;
        public string Name => fileInfo.PhysicalPath;
        public DateTimeOffset LastModified => fileInfo.LastModified;
        public bool IsDirectory => fileInfo.IsDirectory;


        public void Dispose()
        {
            if (this.fileInfo.GetType().Name == nameof(OssFileInfo))
            {
                ((OssFileInfo)this.fileInfo).Dispose();
            }
        }
    }
}