using System.Threading.Tasks;

namespace NspThumbnailExtract
{
    public interface INspParser
    {
        Task<NspMetaData> Get(string nspFile);
    }
}
