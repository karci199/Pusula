using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Pusula.Core.Scanning
{
    /// <summary>
    /// Bir klasörün (alt klasörleriyle) dosya sayısı, toplam boyutu ve en son değişen dosyası.
    /// </summary>
    public sealed class FolderStats
    {
        /// <summary>Boş istatistik.</summary>
        public static readonly FolderStats Empty = new FolderStats(0, 0, string.Empty, null);

        /// <summary>İstatistiği oluşturur.</summary>
        public FolderStats(int fileCount, long totalBytes, string lastFile, DateTime? lastWrite)
        {
            FileCount = fileCount;
            TotalBytes = totalBytes;
            LastFile = lastFile;
            LastWrite = lastWrite;
        }

        /// <summary>Dosya sayısı.</summary>
        public int FileCount { get; }

        /// <summary>Toplam boyut (bayt).</summary>
        public long TotalBytes { get; }

        /// <summary>En son değişen dosyanın tam yolu; dosya yoksa boş.</summary>
        public string LastFile { get; }

        /// <summary>En son değişen dosyanın değişme zamanı; dosya yoksa <c>null</c>.</summary>
        public DateTime? LastWrite { get; }

        /// <summary>İki istatistiği toplar (son dosya, daha yeni olan).</summary>
        public FolderStats Add(FolderStats other)
        {
            var newer = other.LastWrite.HasValue && (!LastWrite.HasValue || other.LastWrite.Value > LastWrite.Value);
            return new FolderStats(
                FileCount + other.FileCount,
                TotalBytes + other.TotalBytes,
                newer ? other.LastFile : LastFile,
                newer ? other.LastWrite : LastWrite);
        }

        /// <summary>
        /// Klasörü alt klasörleriyle birlikte sayar. Okunamayan klasörler atlanır, hata fırlatılmaz.
        /// Sayılmayan klasörler ve önemsiz dosyalar (Thumbs.db, Office kilitleri …) atlanır; bkz. <see cref="FileRules"/>.
        /// </summary>
        public static FolderStats Compute(string folder, CancellationToken cancellation)
        {
            var count = 0;
            long bytes = 0;
            var lastFile = string.Empty;
            DateTime? lastWrite = null;

            var pending = new Stack<string>();
            pending.Push(folder);
            while (pending.Count > 0)
            {
                cancellation.ThrowIfCancellationRequested();
                var current = pending.Pop();
                List<FsEntry> entries;
                try
                {
                    entries = Fs.Entries(current);
                }
                catch (IOException)
                {
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }

                foreach (var entry in entries)
                {
                    if (entry.IsDirectory)
                    {
                        // _TAKIP, _ARSIV, Arsiv, .obsidian … sayılara girmez (kökün kendisi hariç).
                        if (!FileRules.IsSkippedFolder(entry.Name))
                        {
                            pending.Push(entry.FullName);
                        }

                        continue;
                    }

                    var file = entry;
                    if (FileRules.IsJunkFile(file.Name))
                    {
                        continue;
                    }

                    count++;
                    bytes += file.Length;
                    if (!lastWrite.HasValue || file.LastWriteTime > lastWrite.Value)
                    {
                        lastWrite = file.LastWriteTime;
                        lastFile = file.FullName;
                    }
                }
            }

            return new FolderStats(count, bytes, lastFile, lastWrite);
        }

        /// <summary>Uygulamanın kendi klasörü mü (sayılara girmez).</summary>
        public static bool IsAppFolder(string name)
        {
            return string.Equals(name, Tracking.TrackingFiles.FolderName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, Templates.TemplateStore.FolderName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
