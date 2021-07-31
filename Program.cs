using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace NspThumbnailExtract
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
                throw new ApplicationException("FileName not specified.");

            var fileName = Path.GetFullPath(args[0]);
            
            if (!File.Exists(fileName))
                throw new ApplicationException($"FileName `{fileName}` not found.");

            var nspParser = new NspParser();

            var nspMetaData = await nspParser.Get(fileName);

            Console.WriteLine($"NSP File: {nspMetaData.Path}");
            Console.WriteLine($"TitleId: {nspMetaData.TitleId}");
            Console.WriteLine($"Title: {nspMetaData.Title}");
            Console.WriteLine($"Developer: {nspMetaData.Developer}");
            Console.WriteLine($"Thumbnail: {!string.IsNullOrEmpty(nspMetaData.Thumbnail)}");

            using var thumbnailImage = Base64StringToImage(nspMetaData.Thumbnail);

            var thumbnailPath = fileName + ".jpg";

            Console.WriteLine($"Saved thumbnail to: {thumbnailPath}!");

            thumbnailImage.Save(thumbnailPath);
        }

        static Image Base64StringToImage(string input)
        {
            var bytes = Convert.FromBase64String(input);
            var stream = new MemoryStream(bytes);
            return Image.FromStream(stream);
        }
    }
}
