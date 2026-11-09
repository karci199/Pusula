using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf
{
    /// <summary>Notu açan programın seçeneği (Ayarlar).</summary>
    public sealed class NoteAppOption
    {
        /// <summary>Seçeneği kurar.</summary>
        public NoteAppOption(string key, string name)
        {
            Key = key;
            Name = name;
        }

        /// <summary>Ayarlarda saklanan anahtar.</summary>
        public string Key { get; }

        /// <summary>Görünen ad.</summary>
        public string Name { get; }

        /// <summary>Görünen ad.</summary>
        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// <c>.md</c> notlarını seçilen programla açar: Obsidian (<c>obsidian://</c>), VS Code (<c>vscode://</c>) ya da
    /// Windows'un varsayılanı. Program kurulu değilse varsayılanla açılır. Not olmayan dosyalar (fotoğraf, belge)
    /// her zaman varsayılanla açılır.
    /// </summary>
    public static class NoteOpener
    {
        /// <summary>Obsidian.</summary>
        public const string Obsidian = "obsidian";

        /// <summary>Visual Studio Code.</summary>
        public const string VsCode = "vscode";

        /// <summary>Windows'un .md için varsayılan programı.</summary>
        public const string Default = "default";

        private static NoteAppOption[]? options;

        /// <summary>Seçenekler (Ayarlar'daki liste); her zaman aynı örnekler döner (seçim kutusu eşleşsin diye).</summary>
        public static NoteAppOption[] Options
        {
            get
            {
                return options ?? (options = new[]
                {
                    new NoteAppOption(Obsidian, Resources.NoteApp_Obsidian),
                    new NoteAppOption(VsCode, Resources.NoteApp_VsCode),
                    new NoteAppOption(Default, Resources.NoteApp_Default),
                });
            }
        }

        /// <summary>Anahtarı verilen seçenek; tanınmıyorsa Obsidian.</summary>
        public static NoteAppOption Find(string? key)
        {
            return Options.FirstOrDefault(o => string.Equals(o.Key, key, StringComparison.OrdinalIgnoreCase)) ?? Options[0];
        }

        /// <summary>
        /// Dosyayı açar. Not seçilen programla açıldıysa boş, program bulunamayıp varsayılana düşüldüyse programın adı döner.
        /// Hiç açılamazsa <see cref="Win32Exception"/> fırlar.
        /// </summary>
        public static string Open(string path, string app)
        {
            if (!string.Equals(Path.GetExtension(path), ".md", StringComparison.OrdinalIgnoreCase) || app == Default)
            {
                Shell(path);
                return string.Empty;
            }

            var uri = UriFor(path, app);
            if (uri.Length > 0 && HasProtocol(app))
            {
                Shell(uri);
                return string.Empty;
            }

            Shell(path);
            return Find(app).Name;
        }

        /// <summary>Programın açma bağlantısı (Obsidian: <c>obsidian://open?path=…</c>, VS Code: <c>vscode://file/…</c>).</summary>
        public static string UriFor(string path, string app)
        {
            if (app == Obsidian)
            {
                return "obsidian://open?path=" + Uri.EscapeDataString(path);
            }

            if (app == VsCode)
            {
                // C:\a b\x.md → vscode://file/C:/a%20b/x.md ; \\sunucu\pay\x.md → vscode://file//sunucu/pay/x.md
                var parts = path.Replace('\\', '/').Split('/').Select(p => Uri.EscapeDataString(p).Replace("%3A", ":"));
                var joined = string.Join("/", parts);
                return "vscode://file/" + joined.TrimStart('/').Insert(0, path.StartsWith(@"\\", StringComparison.Ordinal) ? "/" : string.Empty);
            }

            return string.Empty;
        }

        private static bool HasProtocol(string scheme)
        {
            try
            {
                using (var key = Registry.ClassesRoot.OpenSubKey(scheme))
                {
                    return key != null && key.GetValue("URL Protocol") != null;
                }
            }
            catch (System.Security.SecurityException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static void Shell(string target)
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
    }
}
