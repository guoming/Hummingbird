using System.IO;
using System.Threading.Tasks;
using ZT.RFS.Infrastructure.FileProvider;

namespace Hummingbird.Extensions.FileSystem.Physical
{
    public class PhysicalFileSystemBucket: IFileSystemBucket
    {
        private readonly string rootPath;

        public PhysicalFileSystemBucket(string rootPath)
        {
            this.rootPath = rootPath;
        }
        
        public async Task UploadAsync(string key, string localSrcFullFilePath, bool deleteSrcFile)
        {
            var localDistFullFilePath = System.IO.Path.Combine(rootPath, key);
            var directoryName = System.IO.Path.GetDirectoryName(localDistFullFilePath);

            if (!System.IO.Directory.Exists(directoryName))
            {
                System.IO.Directory.CreateDirectory(directoryName);
            }

            if (!System.IO.File.Exists(localSrcFullFilePath))
            {
                return;
            }
            
            if (System.IO.File.Exists(localDistFullFilePath))
            {
                return;
            }
            
            System.IO.File.Copy(localSrcFullFilePath, System.IO.Path.Combine(rootPath,key));
            
            if (deleteSrcFile)
            {
                File.Delete(localSrcFullFilePath);
            }
        }

        public void Delete(string key)
        {
            if (!System.IO.File.Exists(key))
            {
                return;
            }
            
            File.Delete(key);
        }
    }
}