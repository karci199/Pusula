using System;
using System.Collections.Generic;
using System.IO;

namespace Pusula.Core.Testing
{
    /// <summary>
    /// Doğrulama dosyasının metni için küçük önbellek: stok açılınca gereksinim, plan ve rapor tabloları aynı dosyadan
    /// ayrı ayrı okunur; dosya değişmedikçe (son yazma zamanı ve boyut aynı) disk bir kez okunur.
    /// Uygulamanın kendi yazımı <see cref="Forget"/> ile önbelleği boşaltır.
    /// </summary>
    internal static class VerificationText
    {
        private const int Capacity = 8;
        private static readonly object Gate = new object();
        private static readonly LinkedList<Entry> Entries = new LinkedList<Entry>();

        /// <summary>Dosyanın metnini döndürür; yoksa null. Okuma hataları (IOException, erişim) çağırana gider.</summary>
        public static string? Read(string path)
        {
            var info = new FileInfo(path);
            if (!info.Exists)
            {
                return null;
            }

            var stamp = info.LastWriteTimeUtc;
            var length = info.Length;
            lock (Gate)
            {
                for (var node = Entries.First; node != null; node = node.Next)
                {
                    if (string.Equals(node.Value.Path, path, StringComparison.OrdinalIgnoreCase))
                    {
                        if (node.Value.Stamp == stamp && node.Value.Length == length)
                        {
                            Entries.Remove(node);
                            Entries.AddFirst(node);
                            return node.Value.Text;
                        }

                        Entries.Remove(node);
                        break;
                    }
                }
            }

            var text = File.ReadAllText(path);
            lock (Gate)
            {
                Entries.AddFirst(new Entry(path, stamp, length, text));
                while (Entries.Count > Capacity)
                {
                    Entries.RemoveLast();
                }
            }

            return text;
        }

        /// <summary>Dosyanın önbellekteki metnini atar (yazımdan önce ve sonra çağrılır).</summary>
        public static void Forget(string path)
        {
            lock (Gate)
            {
                for (var node = Entries.First; node != null; node = node.Next)
                {
                    if (string.Equals(node.Value.Path, path, StringComparison.OrdinalIgnoreCase))
                    {
                        Entries.Remove(node);
                        return;
                    }
                }
            }
        }

        private sealed class Entry
        {
            public Entry(string path, DateTime stamp, long length, string text)
            {
                Path = path;
                Stamp = stamp;
                Length = length;
                Text = text;
            }

            public string Path { get; }

            public DateTime Stamp { get; }

            public long Length { get; }

            public string Text { get; }
        }
    }
}
