using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace Hummingbird.Extensions.FileSystem.Physical
{
    public class PhysicalFileProvider: Microsoft.Extensions.FileProviders.PhysicalFileProvider,Microsoft.Extensions.FileProviders.IFileProvider
    {
        public PhysicalFileProvider(string root) : base(root)
        {
        }

        public PhysicalFileProvider(string root, ExclusionFilters filters) : base(root, filters)
        {
        }
    }
}