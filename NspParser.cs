using LibHac;
using LibHac.IO;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NspThumbnailExtract
{
    public class NspParser : INspParser
    {
        public Task<NspMetaData> Get(string nspFile)
        {
            var returnValue = new NspMetaData()
            {
                Path = nspFile,
            };

            using var file = File.OpenRead(nspFile);

            var prodKeys = Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\.switch\prod.keys");
            var titleKeys = Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\.switch\title.keys");

            var externalKeys = ExternalKeys.ReadKeyFile(prodKeys, titleKeys);

            var nspPfs = new Pfs(file.AsStorage());

            var cnmtNcaEntry = nspPfs.Files.SingleOrDefault(s => s.Name.Contains(".cnmt.nca"));
            using var cnmtNceStorage = nspPfs.OpenFile(cnmtNcaEntry);
            using var cnmtNca = new Nca(externalKeys, cnmtNceStorage, false);
            using var cnmtNcaSectionStorage = cnmtNca.OpenSection(0, false, IntegrityCheckLevel.None, true);

            var cnmtPfs = new Pfs(cnmtNcaSectionStorage);
            using var cnmtStorage = cnmtPfs.OpenFile(cnmtPfs.Files.First());

            var cnmt = new Cnmt(cnmtStorage.AsStream());
            //var cnmtProgram = cnmt.ContentEntries.SingleOrDefault(c => c.Type == CnmtContentType.Program);
            var cnmtControl = cnmt.ContentEntries.FirstOrDefault(c => c.Type == CnmtContentType.Control);
            //if (cnmtControl == null)
            //{
            //    cnmtControl = cnmt.ContentEntries.FirstOrDefault(c => c.Type == CnmtContentType.Data);
            //}

            if (cnmtControl != null)
            {
                using var cnmtControlNcaStorage = nspPfs.OpenFile(cnmtControl.NcaId.ToHexString().ToLower() + ".nca");
                var cnmtControlNca = new Nca(externalKeys, cnmtControlNcaStorage, false);
                if (cnmtControlNca != null)
                {
                    var titleId = "0" + cnmtControlNca.Header.TitleId.ToString("X");
                    returnValue.TitleId = titleId;

                    using var cnmtControlNcaSectionStorage = cnmtControlNca.OpenSection(0, false, IntegrityCheckLevel.None, true);

                    var romFs = new Romfs(cnmtControlNcaSectionStorage);

                    var iconRomFsFile = romFs.Files.FirstOrDefault(f => f.Name.Contains("icon"));
                    if (iconRomFsFile != null)
                    {
                        var iconFileStorage = romFs.OpenFile(iconRomFsFile);

                        using var memoryStream = new MemoryStream();
                        iconFileStorage.AsStream().CopyTo(memoryStream);

                        returnValue.Thumbnail = Convert.ToBase64String(memoryStream.ToArray());
                    }

                    var nacpFile = romFs.Files.SingleOrDefault(f => f.Name == "control.nacp");
                    if (nacpFile != null)
                    {
                        using var nacpStorage = romFs.OpenFile(nacpFile);
                        var nacp = new Nacp(nacpStorage.AsStream());

                        var nacpDescriptionWithTitle = nacp.Descriptions.FirstOrDefault(l => l.Title.Length > 1);
                        if (nacpDescriptionWithTitle != null)
                        {
                            returnValue.Title = nacpDescriptionWithTitle.Title;
                            returnValue.Name = nacpDescriptionWithTitle.Title;
                            returnValue.Developer = nacpDescriptionWithTitle.Developer;
                        }
                    }
                }
            }

            return Task.FromResult(returnValue);
        }
    }
}
