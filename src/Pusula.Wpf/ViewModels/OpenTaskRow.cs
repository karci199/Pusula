using System;
using System.IO;
using Pusula.Core.Notes;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Açık iş satırı: metin ve nereden geldiği (stokta not adı, projede "stok · not").</summary>
    public sealed class OpenTaskRow
    {
        /// <summary>Satırı kurar; <paramref name="projectFolder"/> verilirse kaynak stok numarasıyla yazılır.</summary>
        public OpenTaskRow(OpenTask task, string projectFolder)
        {
            Text = task.Text;
            NotePath = task.NotePath;
            var label = task.NoteName;
            if (projectFolder.Length > 0 && task.NotePath.StartsWith(projectFolder.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase))
            {
                var relative = task.NotePath.Substring(projectFolder.TrimEnd('\\').Length + 1);
                var stock = relative.Split('\\')[0];
                if (relative.IndexOf('\\') > 0 && !string.Equals(stock, label, StringComparison.OrdinalIgnoreCase))
                {
                    label = stock + " · " + label;
                }
            }

            Label = label;
        }

        /// <summary>Görev metni.</summary>
        public string Text { get; }

        /// <summary>Notun tam yolu.</summary>
        public string NotePath { get; }

        /// <summary>Kaynak ("SNP1_T1", projede "AB-7000-0002 · SNP1_T1").</summary>
        public string Label { get; }
    }
}