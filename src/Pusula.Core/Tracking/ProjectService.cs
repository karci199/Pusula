using System;
using System.Collections.Generic;
using System.IO;

namespace Pusula.Core.Tracking
{
    /// <summary>
    /// Proje ve stok klasörlerini işaret dosyaları ve şablon klasörleriyle birlikte oluşturur.
    /// Var olan hiçbir klasörü ya da dosyayı ezmez.
    /// </summary>
    public static class ProjectService
    {
        /// <summary>
        /// <c>&lt;projelerKökü&gt;\&lt;no&gt;_&lt;ad&gt;</c> klasörünü, <c>_TAKIP\proje.json</c> dosyasını
        /// ve verilen alt klasörleri oluşturur.
        /// </summary>
        public static CreateResult CreateProject(string projectsRoot, string projectNo, string name, IEnumerable<string> subFolders)
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
            return CreateFolder(projectsRoot, projectNo + "_" + name, subFolders, folder => JsonFile.WriteAtomic(TrackingFiles.ProjectFile(folder), info));
        }

        /// <summary>
        /// <c>&lt;projeKlasörü&gt;\&lt;stokNo&gt;</c> klasörünü, <c>_TAKIP\stok.json</c> dosyasını
        /// ve şablondan gelen alt klasörleri oluşturur. Ad boş olabilir.
        /// </summary>
        public static CreateResult CreateStock(string projectFolder, string stockNo, string name, string template, IEnumerable<string> subFolders)
        {
            stockNo = (stockNo ?? string.Empty).Trim();
            name = (name ?? string.Empty).Trim();

            if (stockNo.Length == 0)
            {
                return CreateResult.Fail(CreateError.NumberEmpty, string.Empty);
            }

            var info = new StockInfo { StockNo = stockNo, Name = name, Template = template ?? string.Empty };
            return CreateFolder(projectFolder, stockNo, subFolders, folder => JsonFile.WriteAtomic(TrackingFiles.StockFile(folder), info));
        }

        /// <summary>
        /// Var olan bir klasörün altında eksik alt klasörleri açar; var olanlara dokunmaz.
        /// Açılan klasör sayısını döner.
        /// </summary>
        public static int AddFolders(string folder, IEnumerable<string> subFolders)
        {
            var created = 0;
            foreach (var relative in subFolders)
            {
                if (!IsSafeRelativePath(relative))
                {
                    continue;
                }

                var path = Path.Combine(folder, relative);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    created++;
                }
            }

            return created;
        }

        /// <summary>Göreli yol güvenli mi: köklü değil, <c>..</c> yok, geçersiz karakter yok.</summary>
        public static bool IsSafeRelativePath(string relative)
        {
            if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative))
            {
                return false;
            }

            foreach (var part in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            {
                if (part.Length == 0 || part == "." || part == ".." || part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static CreateResult CreateFolder(string parent, string folderName, IEnumerable<string> subFolders, Action<string> writeMarker)
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
                writeMarker(folder);
                TrackingFiles.HideTrackingFolder(folder);
                AddFolders(folder, subFolders);
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
