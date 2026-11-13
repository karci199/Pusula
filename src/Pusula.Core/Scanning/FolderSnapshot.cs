using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Pusula.Core.Scanning
{
    /// <summary>Taramadaki bir dosya ya da klasör.</summary>
    public sealed class FsEntry
    {
        /// <summary>Tam yol.</summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>Ad.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Klasör mü.</summary>
        public bool IsDirectory { get; set; }

        /// <summary>Boyut (bayt; klasörde 0).</summary>
        public long Length { get; set; }

        /// <summary>Son yazılma zamanı.</summary>
        public DateTime LastWriteTime { get; set; }
    }

    /// <summary>
    /// Bir klasörün (stok) tek seferlik taraması: bütün alt klasörler ve dosyalar, boyut ve tarihleriyle belleğe alınır;
    /// md gibi metinler de ilk okunuşta saklanır. Stok sayfasını dolduran okuyucular aynı anda çalışırken klasörü ayrı
    /// ayrı gezmek yerine bu görüntüyü kullanır (ağ paylaşımında her klasör listelemesi bir gidiş-dönüştür).
    /// Etkinleştirmek için <see cref="Activate"/>; okuyucular <see cref="Fs"/> üzerinden sorar.
    /// </summary>
    public sealed class FolderSnapshot
    {
        private static readonly AsyncLocal<FolderSnapshot?> ActiveSnapshot = new AsyncLocal<FolderSnapshot?>();

        private readonly Dictionary<string, List<FsEntry>> children = new Dictionary<string, List<FsEntry>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FsEntry> entries = new Dictionary<string, FsEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, string?> texts = new ConcurrentDictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        private FolderSnapshot(string root)
        {
            Root = Normalize(root);
        }

        /// <summary>Taranan kök.</summary>
        public string Root { get; }

        /// <summary>Etkin tarama (bu yükleme boyunca); yoksa <c>null</c>.</summary>
        public static FolderSnapshot? Active
        {
            get { return ActiveSnapshot.Value; }
        }

        /// <summary>Kaç klasör tarandı (ölçüm için).</summary>
        public int DirectoryCount
        {
            get { return children.Count; }
        }

        /// <summary>Klasörü tek seferde tarar (her klasör için tek listeleme). Okunamayan klasörler boş geçilir.</summary>
        public static FolderSnapshot Take(string root, CancellationToken cancellation)
        {
            var snapshot = new FolderSnapshot(root);
            var pending = new Stack<string>();
            pending.Push(snapshot.Root);
            while (pending.Count > 0)
            {
                cancellation.ThrowIfCancellationRequested();
                var folder = pending.Pop();
                var list = new List<FsEntry>();
                snapshot.children[folder] = list;
                FileSystemInfo[] infos;
                try
                {
                    infos = new DirectoryInfo(folder).GetFileSystemInfos();
                }
                catch (IOException)
                {
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }

                foreach (var info in infos)
                {
                    var isDirectory = (info.Attributes & FileAttributes.Directory) != 0;
                    var entry = new FsEntry
                    {
                        FullName = Normalize(info.FullName),
                        Name = info.Name,
                        IsDirectory = isDirectory,
                        Length = isDirectory ? 0 : ((FileInfo)info).Length,
                        LastWriteTime = info.LastWriteTime,
                    };
                    list.Add(entry);
                    snapshot.entries[entry.FullName] = entry;
                    if (isDirectory)
                    {
                        pending.Push(entry.FullName);
                    }
                }
            }

            return snapshot;
        }

        /// <summary>Taramayı etkinleştirir; dönen nesne bırakılınca (using sonu) eski duruma döner.</summary>
        public static IDisposable Activate(FolderSnapshot snapshot)
        {
            var previous = ActiveSnapshot.Value;
            ActiveSnapshot.Value = snapshot;
            return new Restore(previous);
        }

        /// <summary>Yol bu taramanın içinde mi.</summary>
        public bool Covers(string path)
        {
            var full = Normalize(path);
            return string.Equals(full, Root, StringComparison.OrdinalIgnoreCase)
                || full.StartsWith(Root + "\\", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Klasörün doğrudan içindekiler; klasör yoksa <c>null</c>.</summary>
        public List<FsEntry>? ChildrenOf(string folder)
        {
            List<FsEntry>? list;
            return children.TryGetValue(Normalize(folder), out list) ? list : null;
        }

        /// <summary>Yoldaki kayıt; yoksa <c>null</c>.</summary>
        public FsEntry? Find(string path)
        {
            FsEntry? entry;
            return entries.TryGetValue(Normalize(path), out entry) ? entry : null;
        }

        /// <summary>Metni bir kez okur, sonra saklananı verir; okunamazsa <c>null</c>.</summary>
        public string? Text(string path)
        {
            return texts.GetOrAdd(Normalize(path), p =>
            {
                try
                {
                    return File.ReadAllText(p, Encoding.UTF8);
                }
                catch (IOException)
                {
                    return null;
                }
                catch (UnauthorizedAccessException)
                {
                    return null;
                }
            });
        }

        internal static string Normalize(string path)
        {
            return (path ?? string.Empty).Replace('/', '\\').TrimEnd('\\');
        }

        private sealed class Restore : IDisposable
        {
            private readonly FolderSnapshot? previous;

            public Restore(FolderSnapshot? previous)
            {
                this.previous = previous;
            }

            public void Dispose()
            {
                ActiveSnapshot.Value = previous;
            }
        }
    }

    /// <summary>
    /// Okuyucuların dosya sistemi kapısı: etkin bir <see cref="FolderSnapshot"/> yolu kapsıyorsa cevap bellekten,
    /// yoksa diskten gelir. Yalnızca okuma içindir; yazma işlemleri her zaman doğrudan diske gider.
    /// </summary>
    public static class Fs
    {
        private static readonly ConcurrentDictionary<string, Regex> Patterns = new ConcurrentDictionary<string, Regex>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Klasörün dosya ve alt klasörleri (boyut, tarih ile).</summary>
        public static List<FsEntry> Entries(string folder)
        {
            var snapshot = For(folder);
            if (snapshot != null)
            {
                return snapshot.ChildrenOf(folder) ?? throw new DirectoryNotFoundException(folder);
            }

            return new DirectoryInfo(folder).GetFileSystemInfos().Select(i => new FsEntry
            {
                FullName = i.FullName,
                Name = i.Name,
                IsDirectory = (i.Attributes & FileAttributes.Directory) != 0,
                Length = (i.Attributes & FileAttributes.Directory) != 0 ? 0 : ((FileInfo)i).Length,
                LastWriteTime = i.LastWriteTime,
            }).ToList();
        }

        /// <summary><see cref="Directory.GetFiles(string)"/> gibi.</summary>
        public static string[] GetFiles(string folder)
        {
            return GetFiles(folder, "*", SearchOption.TopDirectoryOnly);
        }

        /// <summary><see cref="Directory.GetFiles(string, string)"/> gibi.</summary>
        public static string[] GetFiles(string folder, string pattern)
        {
            return GetFiles(folder, pattern, SearchOption.TopDirectoryOnly);
        }

        /// <summary><see cref="Directory.GetFiles(string, string, SearchOption)"/> gibi (kalıp: <c>*</c>, <c>*.md</c>, tam ad).</summary>
        public static string[] GetFiles(string folder, string pattern, SearchOption option)
        {
            var snapshot = For(folder);
            if (snapshot == null)
            {
                return Directory.GetFiles(folder, pattern, option);
            }

            var regex = Patterns.GetOrAdd(pattern, p => new Regex("^" + Regex.Escape(p).Replace(@"\*", ".*").Replace(@"\?", ".") + "$", RegexOptions.IgnoreCase));
            var result = new List<string>();
            var pending = new Stack<string>();
            pending.Push(folder);
            var first = true;
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                var list = snapshot.ChildrenOf(current);
                if (list == null)
                {
                    if (first)
                    {
                        throw new DirectoryNotFoundException(folder);
                    }

                    continue;
                }

                first = false;
                foreach (var entry in list)
                {
                    if (entry.IsDirectory)
                    {
                        if (option == SearchOption.AllDirectories)
                        {
                            pending.Push(entry.FullName);
                        }
                    }
                    else if (regex.IsMatch(entry.Name))
                    {
                        result.Add(entry.FullName);
                    }
                }
            }

            return result.ToArray();
        }

        /// <summary><see cref="Directory.GetDirectories(string)"/> gibi.</summary>
        public static string[] GetDirectories(string folder)
        {
            var snapshot = For(folder);
            if (snapshot == null)
            {
                return Directory.GetDirectories(folder);
            }

            var list = snapshot.ChildrenOf(folder) ?? throw new DirectoryNotFoundException(folder);
            return list.Where(e => e.IsDirectory).Select(e => e.FullName).ToArray();
        }

        /// <summary><see cref="Directory.Exists"/> gibi.</summary>
        public static bool DirectoryExists(string folder)
        {
            var snapshot = For(folder);
            return snapshot != null ? snapshot.ChildrenOf(folder) != null : Directory.Exists(folder);
        }

        /// <summary><see cref="File.Exists"/> gibi.</summary>
        public static bool FileExists(string path)
        {
            var snapshot = For(path);
            if (snapshot == null)
            {
                return File.Exists(path);
            }

            var entry = snapshot.Find(path);
            return entry != null && !entry.IsDirectory;
        }

        /// <summary><see cref="File.GetLastWriteTime"/> gibi.</summary>
        public static DateTime GetLastWriteTime(string path)
        {
            var entry = For(path)?.Find(path);
            return entry != null ? entry.LastWriteTime : File.GetLastWriteTime(path);
        }

        /// <summary>Metni okur (taramada bir kez okunup saklanır). Okunamazsa <see cref="IOException"/>.</summary>
        public static string ReadAllText(string path)
        {
            var snapshot = For(path);
            if (snapshot == null)
            {
                return File.ReadAllText(path, Encoding.UTF8);
            }

            return snapshot.Text(path) ?? throw new IOException(path);
        }

        /// <summary>Satırları okur (bkz. <see cref="ReadAllText"/>).</summary>
        public static string[] ReadAllLines(string path)
        {
            var text = ReadAllText(path);
            var lines = text.Replace("\r\n", "\n").Split('\n');
            return lines.Length > 0 && lines[lines.Length - 1].Length == 0 ? lines.Take(lines.Length - 1).ToArray() : lines;
        }

        private static FolderSnapshot? For(string path)
        {
            var snapshot = FolderSnapshot.Active;
            return snapshot != null && snapshot.Covers(path) ? snapshot : null;
        }
    }
}
