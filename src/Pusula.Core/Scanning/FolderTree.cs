using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Pusula.Core.Scanning
{
    /// <summary>
    /// Bir klasörün alt klasör ağacı; her düğümde (alt klasörleriyle) dosya sayısı.
    /// <c>_TAKIP</c> ve <c>_PUSULA</c> gösterilmez; <c>_ARSIV</c> gibi diğer özel klasörler gösterilir ama işaretlenir.
    /// </summary>
    public sealed class FolderTreeNode
    {
        /// <summary>Klasör adı.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Tam yol.</summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>Alt klasörleriyle birlikte dosya sayısı.</summary>
        public int FileCount { get; set; }

        /// <summary><c>_</c> ile başlayan özel klasör mü.</summary>
        public bool IsSpecial { get; set; }

        /// <summary>Alt klasörler.</summary>
        public List<FolderTreeNode> Children { get; } = new List<FolderTreeNode>();

        /// <summary>Klasörün ağacını kurar; okunamayan klasörler boş geçilir.</summary>
        public static FolderTreeNode Build(string folder, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            var node = new FolderTreeNode
            {
                Name = System.IO.Path.GetFileName(folder),
                Path = folder,
                IsSpecial = System.IO.Path.GetFileName(folder).StartsWith("_", StringComparison.Ordinal),
            };

            try
            {
                node.FileCount = Directory.GetFiles(folder).Length;
                foreach (var child in Directory.GetDirectories(folder).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
                {
                    if (FolderStats.IsAppFolder(System.IO.Path.GetFileName(child)))
                    {
                        continue;
                    }

                    var sub = Build(child, cancellation);
                    node.Children.Add(sub);
                    node.FileCount += sub.FileCount;
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            // Özel klasörler sonda.
            var ordered = node.Children.OrderBy(c => c.IsSpecial).ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
            node.Children.Clear();
            node.Children.AddRange(ordered);
            return node;
        }
    }
}
