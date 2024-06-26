using System.Threading.Tasks;

namespace ZT.RFS.Infrastructure.FileProvider
{
    public interface IFileSystemBucket
    {
        Task UploadAsync(string key, string localSrcFullFilePath, bool deleteSrcFile);
        
        void Delete(string key);

    }
}