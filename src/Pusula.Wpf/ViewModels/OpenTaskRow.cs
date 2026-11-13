using System;
using System.IO;
using Pusula.Core.Notes;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>İş satırı: metin, kişi, son tarih, bitti mi ve nereden geldiği (stokta not adı, projede "stok · not").</summary>
    public sealed class OpenTaskRow
    {
        /// <summary>Satırı kurar; <paramref name="projectFolder"/> verilirse kaynak stok numarasıyla yazılır.</summary>
        public OpenTaskRow(OpenTask task, string projectFolder)
        {
            Task = task;
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
            Meta = task.PersonName.Length == 0 ? label : task.PersonName + " · " + label;
            if (task.IsDone)
            {
                DueText = task.DoneDate.HasValue ? string.Format(Resources.Task_DoneOn, task.DoneDate.Value.ToString("dd.MM")) : string.Empty;
            }
            else if (task.Due.HasValue)
            {
                IsLate = task.Due.Value.Date < DateTime.Today;
                DueText = IsLate ? string.Format(Resources.Task_Late, task.Due.Value.ToString("dd.MM")) : task.Due.Value.ToString("dd.MM");
            }
        }

        /// <summary>Okunan görev (kapatırken satırı ve notu verir).</summary>
        public OpenTask Task { get; }

        /// <summary>Görev metni.</summary>
        public string Text { get; }

        /// <summary>Notun tam yolu.</summary>
        public string NotePath { get; }

        /// <summary>Kaynak ("SNP1_T1", projede "AB-7000-0002 · SNP1_T1").</summary>
        public string Label { get; }

        /// <summary>Kişi ve kaynak ("Ayşe Demir · AB-7000-0003").</summary>
        public string Meta { get; }

        /// <summary>Kişi; yoksa boş.</summary>
        public string PersonName
        {
            get { return Task.PersonName; }
        }

        /// <summary>Bitti mi.</summary>
        public bool IsDone
        {
            get { return Task.IsDone; }
        }

        /// <summary>Son tarih ("14.11", gecikmişse "08.11 · gecikti"; bitende "bitti 08.11"); yoksa boş.</summary>
        public string DueText { get; } = string.Empty;

        /// <summary>Son tarihi geçmiş açık iş mi (kırmızı).</summary>
        public bool IsLate { get; }
    }
}