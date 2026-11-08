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
        public static CreateResult CreateStock(string projectFolder, string stockNo, string name, string template, IEnumerable<string> subFolders, string initialStatus = "")
        {
            stockNo = (stockNo ?? string.Empty).Trim();
            name = (name ?? string.Empty).Trim();

            if (stockNo.Length == 0)
            {
                return CreateResult.Fail(CreateError.NumberEmpty, string.Empty);
            }

            var info = new StockInfo { StockNo = stockNo, Name = name, Template = template ?? string.Empty };
            if (!string.IsNullOrEmpty(initialStatus))
            {
                info.SetStatus(initialStatus, string.Empty, DateTime.Today);
            }

            return CreateFolder(projectFolder, stockNo, subFolders, folder => JsonFile.WriteAtomic(TrackingFiles.StockFile(folder), info));
        }

        /// <summary>
        /// Elle açılmış bir klasörü proje olarak işaretler: yalnızca gizli <c>_TAKIP\proje.json</c> eklenir,
        /// klasördeki hiçbir şeye dokunulmaz.
        /// </summary>
        public static CreateResult MarkAsProject(string folder, string projectNo, string name)
        {
            projectNo = (projectNo ?? string.Empty).Trim();
            name = (name ?? string.Empty).Trim();
            if (projectNo.Length == 0)
            {
                return CreateResult.Fail(CreateError.NumberEmpty, folder);
            }

            if (name.Length == 0)
            {
                return CreateResult.Fail(CreateError.NameEmpty, folder);
            }

            var info = new ProjectInfo { ProjectNo = projectNo, Name = name };
            return Mark(folder, () => JsonFile.WriteAtomic(TrackingFiles.ProjectFile(folder), info));
        }

        /// <summary>
        /// Elle açılmış bir klasörü stok olarak işaretler: yalnızca gizli <c>_TAKIP\stok.json</c> eklenir,
        /// klasördeki hiçbir şeye dokunulmaz. Şablon boş kalır (klasörler elle açılmıştır).
        /// </summary>
        public static CreateResult MarkAsStock(string folder, string stockNo, string name, string initialStatus)
        {
            stockNo = (stockNo ?? string.Empty).Trim();
            name = (name ?? string.Empty).Trim();
            if (stockNo.Length == 0)
            {
                return CreateResult.Fail(CreateError.NumberEmpty, folder);
            }

            var info = new StockInfo { StockNo = stockNo, Name = name };
            if (!string.IsNullOrEmpty(initialStatus))
            {
                info.SetStatus(initialStatus, string.Empty, DateTime.Today);
            }

            return Mark(folder, () => JsonFile.WriteAtomic(TrackingFiles.StockFile(folder), info));
        }

        private static CreateResult Mark(string folder, Action writeMarker)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                return CreateResult.Fail(CreateError.ParentMissing, string.Empty);
            }

            if (FolderReader.KindOf(folder) != FolderKind.Plain)
            {
                return CreateResult.Fail(CreateError.AlreadyMarked, folder);
            }

            try
            {
                writeMarker();
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

        /// <summary>
        /// Stokun durumunu <c>stok.json</c> içine yazar (diğer alanlara dokunmaz). Dosya yoksa ya da yazılamazsa <c>false</c>.
        /// </summary>
        public static bool SetStockStatus(string stockFolder, string status, string note)
        {
            var path = TrackingFiles.StockFile(stockFolder);
            var info = JsonFile.TryRead<StockInfo>(path);
            if (info == null)
            {
                return false;
            }

            info.SetStatus(status, note, DateTime.Today);
            try
            {
                JsonFile.WriteAtomic(path, info);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
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
