using System;
using System.IO;

namespace Pusula.Core.Tracking
{
    /// <summary>
    /// Proje ve stok klasörlerini işaret dosyalarıyla birlikte oluşturur.
    /// Var olan hiçbir klasörü ya da dosyayı ezmez.
    /// </summary>
    public static class ProjectService
    {
        /// <summary>
        /// <c>&lt;projelerKökü&gt;\&lt;no&gt;_&lt;ad&gt;\_TAKIP\proje.json</c> oluşturur.
        /// </summary>
        public static CreateResult CreateProject(string projectsRoot, string projectNo, string name)
        {
            projectNo = (projectNo ?? string.Empty).Trim();
            name = (name ?? string.Empty).Trim();

            if (projectNo.Length == 0)
            {
                return CreateResult.Fail(CreateError.NumberEmpty, string.Empty);
            }

            if (name.Length == 0)
            {
                return CreateResult.Fail(CreateError.NameEmpty, string.Empty);
            }

            var info = new ProjectInfo { ProjectNo = projectNo, Name = name };
            return CreateFolder(projectsRoot, projectNo + "_" + name, TrackingFiles.ProjectFile, folder => JsonFile.WriteAtomic(TrackingFiles.ProjectFile(folder), info));
        }

        /// <summary>
        /// <c>&lt;projeKlasörü&gt;\&lt;stokNo&gt;\_TAKIP\stok.json</c> oluşturur. Ad boş olabilir.
        /// </summary>
        public static CreateResult CreateStock(string projectFolder, string stockNo, string name)
        {
            stockNo = (stockNo ?? string.Empty).Trim();
            name = (name ?? string.Empty).Trim();

            if (stockNo.Length == 0)
            {
                return CreateResult.Fail(CreateError.NumberEmpty, string.Empty);
            }

            var info = new StockInfo { StockNo = stockNo, Name = name };
            return CreateFolder(projectFolder, stockNo, TrackingFiles.StockFile, folder => JsonFile.WriteAtomic(TrackingFiles.StockFile(folder), info));
        }

        private static CreateResult CreateFolder(string parent, string folderName, Func<string, string> markerPath, Action<string> writeMarker)
        {
            if (string.IsNullOrWhiteSpace(parent) || !Directory.Exists(parent))
            {
                return CreateResult.Fail(CreateError.ParentMissing, string.Empty);
            }

            if (folderName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return CreateResult.Fail(CreateError.InvalidCharacters, string.Empty);
            }

            var folder = Path.Combine(parent, folderName);
            if (Directory.Exists(folder) || File.Exists(folder))
            {
                return CreateResult.Fail(CreateError.AlreadyExists, folder);
            }

            try
            {
                Directory.CreateDirectory(folder);
                if (!File.Exists(markerPath(folder)))
                {
                    writeMarker(folder);
                }

                TrackingFiles.HideTrackingFolder(folder);

                return CreateResult.Success(folder);
            }
            catch (IOException ex)
            {
                return CreateResult.Fail(CreateError.IoFailure, folder, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateResult.Fail(CreateError.IoFailure, folder, ex.Message);
            }
        }
    }
}
