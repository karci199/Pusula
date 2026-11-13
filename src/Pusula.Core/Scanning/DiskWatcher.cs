using System;
using System.IO;
using System.Threading;

namespace Pusula.Core.Scanning
{
    /// <summary>
    /// Kök klasörü izler; dışarıdan (Obsidian, Gezgin …) bir değişiklik olunca <see cref="Changed"/> bir kez tetiklenir.
    /// Uygulamanın kendi yazdıkları (<see cref="MarkOwnWrite"/> sonrası kısa süre), gizli klasörler (<c>.obsidian</c>),
    /// takip klasörleri ve geçici dosyalar sayılmaz. Bildirim yoktur; arayüz yalnızca Yenile düğmesini renklendirir.
    /// </summary>
    public sealed class DiskWatcher : IDisposable
    {
        /// <summary>Uygulamanın kendi yazımından sonra olayların yok sayıldığı süre.</summary>
        public static readonly TimeSpan OwnWriteWindow = TimeSpan.FromSeconds(3);

        private static long lastOwnWrite;
        private readonly FileSystemWatcher? watcher;
        private int raised;

        /// <summary>Kökü izlemeye başlar; kök yoksa ya da izlenemiyorsa sessizce hiçbir şey yapmaz.</summary>
        public DiskWatcher(string root)
        {
            Root = root ?? string.Empty;
            if (Root.Length == 0 || !Directory.Exists(Root))
            {
                return;
            }

            try
            {
                watcher = new FileSystemWatcher(Root)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                    InternalBufferSize = 64 * 1024,
                };
                watcher.Created += (s, e) => OnEvent(e.FullPath);
                // Klasörün yalnız tarihi değişmesi (içindeki bir dosya yazılınca) sayılmaz; içerik olayları zaten gelir.
                watcher.Changed += (s, e) =>
                {
                    if (!Directory.Exists(e.FullPath))
                    {
                        OnEvent(e.FullPath);
                    }
                };
                watcher.Deleted += (s, e) => OnEvent(e.FullPath);
                watcher.Renamed += (s, e) => OnEvent(e.FullPath);
                watcher.Error += (s, e) => Raise();
                watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is UnauthorizedAccessException)
            {
                watcher?.Dispose();
                watcher = null;
            }
        }

        /// <summary>Dışarıdan değişiklik oldu (arka plan iş parçacığından; <see cref="Reset"/> çağrılana dek bir kez).</summary>
        public event EventHandler? Changed;

        /// <summary>İzlenen kök.</summary>
        public string Root { get; }

        /// <summary>Sayılan son değişikliğin yolu (bilgi için).</summary>
        public string LastPath { get; private set; } = string.Empty;

        /// <summary>İzleme çalışıyor mu.</summary>
        public bool IsActive
        {
            get { return watcher != null; }
        }

        /// <summary>Uygulama diske yazdı: bundan sonraki kısa süredeki olaylar dışarıdan sayılmaz.</summary>
        public static void MarkOwnWrite()
        {
            Interlocked.Exchange(ref lastOwnWrite, DateTime.UtcNow.Ticks);
        }

        /// <summary>Yenilendi: bir sonraki değişiklik yine bildirilir.</summary>
        public void Reset()
        {
            Interlocked.Exchange(ref raised, 0);
        }

        /// <summary>İzlemeyi bırakır.</summary>
        public void Dispose()
        {
            watcher?.Dispose();
        }

        /// <summary>Yolun dışarıdan değişiklik sayılıp sayılmayacağı (gizli / takip / geçici olanlar sayılmaz).</summary>
        public static bool Counts(string root, string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath) || string.IsNullOrEmpty(root))
            {
                return false;
            }

            var relative = fullPath.Length > root.Length ? fullPath.Substring(root.Length).TrimStart('\\', '/') : string.Empty;
            foreach (var part in relative.Split('\\', '/'))
            {
                if (part.Length == 0)
                {
                    continue;
                }

                if (part.StartsWith(".", StringComparison.Ordinal) || part.StartsWith("~", StringComparison.Ordinal)
                    || string.Equals(part, "_TAKIP", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(part, "_PUSULA", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            var extension = Path.GetExtension(fullPath);
            return !string.Equals(extension, ".tmp", StringComparison.OrdinalIgnoreCase)
                && Path.GetFileName(fullPath).IndexOf(".tmp-", StringComparison.OrdinalIgnoreCase) < 0;
        }

        private void OnEvent(string fullPath)
        {
            var sinceOwn = DateTime.UtcNow.Ticks - Interlocked.Read(ref lastOwnWrite);
            if (sinceOwn >= 0 && sinceOwn < OwnWriteWindow.Ticks)
            {
                return;
            }

            if (Counts(Root, fullPath))
            {
                LastPath = fullPath;
                Raise();
            }
        }

        private void Raise()
        {
            if (Interlocked.Exchange(ref raised, 1) == 0)
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
