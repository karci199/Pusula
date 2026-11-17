using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Data;

namespace MeDitor
{
    /// <summary>Kenar çubuğu ağacında bir klasör ya da md dosyası.</summary>
    public sealed class FolderNode
    {
        /// <summary>Md dosyası sayılan uzantılar.</summary>
        public static readonly string[] Extensions = { ".md", ".markdown" };

        private FolderNode(string path, bool isFolder)
        {
            Path = path;
            IsFolder = isFolder;
            Name = isFolder ? System.IO.Path.GetFileName(path.TrimEnd('\\')) : System.IO.Path.GetFileNameWithoutExtension(path);
        }

        /// <summary>Tam yol.</summary>
        public string Path { get; }

        /// <summary>Görünen ad (dosyada uzantısız).</summary>
        public string Name { get; }

        /// <summary>Klasör mü.</summary>
        public bool IsFolder { get; }

        /// <summary>İkon.</summary>
        public string Glyph
        {
            get { return IsFolder ? AppIcons.Folder : AppIcons.Note; }
        }

        /// <summary>Alt klasörler (önce) ve md dosyaları; md içermeyen klasörler gösterilmez.</summary>
        public ObservableCollection<FolderNode> Children { get; } = new ObservableCollection<FolderNode>();

        /// <summary>Md dosyası mı.</summary>
        public static bool IsMarkdown(string path)
        {
            var extension = System.IO.Path.GetExtension(path ?? string.Empty);
            return Extensions.Any(e => string.Equals(e, extension, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Klasörün ağacını kurar: gizli ve <c>.</c> ile başlayan klasörler atlanır, en çok <paramref name="depth"/> düzey iner.
        /// Kökün kendisi listelenmez; çocukları döner.
        /// </summary>
        public static List<FolderNode> Scan(string root, int depth = 8)
        {
            var node = new FolderNode(root, true);
            Fill(node, depth);
            return node.Children.ToList();
        }

        /// <summary>Klasördeki bütün md dosyaları (alt klasörler dahil, ada göre).</summary>
        public static List<string> AllFiles(IEnumerable<FolderNode> nodes)
        {
            var list = new List<string>();
            foreach (var node in nodes)
            {
                if (node.IsFolder)
                {
                    list.AddRange(AllFiles(node.Children));
                }
                else
                {
                    list.Add(node.Path);
                }
            }

            return list;
        }

        private static bool Fill(FolderNode node, int depth)
        {
            string[] folders;
            string[] files;
            try
            {
                folders = Directory.GetDirectories(node.Path);
                files = Directory.GetFiles(node.Path);
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }

            if (depth > 0)
            {
                foreach (var folder in folders.OrderBy(f => f, StringComparer.CurrentCultureIgnoreCase))
                {
                    var name = System.IO.Path.GetFileName(folder);
                    if (name.StartsWith(".", StringComparison.Ordinal) || (new DirectoryInfo(folder).Attributes & FileAttributes.Hidden) != 0)
                    {
                        continue;
                    }

                    var child = new FolderNode(folder, true);
                    if (Fill(child, depth - 1))
                    {
                        node.Children.Add(child);
                    }
                }
            }

            foreach (var file in files.Where(IsMarkdown).OrderBy(f => f, StringComparer.CurrentCultureIgnoreCase))
            {
                node.Children.Add(new FolderNode(file, false));
            }

            return node.Children.Count > 0;
        }
    }

    /// <summary>Yoldan dosya adını (uzantısız) ya da klasörünü gösterir (son açılanlar listesi).</summary>
    public sealed class FileNameConverter : IValueConverter
    {
        /// <summary>Dosya adı.</summary>
        public static readonly FileNameConverter Instance = new FileNameConverter(false);

        /// <summary>Klasör.</summary>
        public static readonly FileNameConverter FolderInstance = new FileNameConverter(true);

        private readonly bool folder;

        private FileNameConverter(bool folder)
        {
            this.folder = folder;
        }

        /// <summary>Yolu çevirir.</summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value as string ?? string.Empty;
            return folder ? (Path.GetDirectoryName(path) ?? string.Empty) : Path.GetFileNameWithoutExtension(path);
        }

        /// <summary>Kullanılmaz.</summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
