using System;
using System.IO;
using System.Linq;
using Pusula.Core.Inventory;
using Pusula.Core.Notes;
using Pusula.Core.Tracking;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>
    /// Ana görünüm: yapılacak iş ekleme ve kapatma (stok / proje sayfası). İş, sayfanın ana notundaki
    /// <c>## Yapılacaklar</c> bölümüne yazılır (bkz. <see cref="TaskList"/>).
    /// </summary>
    public sealed partial class MainViewModel
    {
        private string newTaskText = string.Empty;
        private string newTaskDueText = string.Empty;

        /// <summary>Eklenecek işin metni (<c>@kişi</c>, <c>@NUMARA</c>, <c>#etiket</c> yazılabilir).</summary>
        public string NewTaskText
        {
            get { return newTaskText; }
            set { SetProperty(ref newTaskText, value ?? string.Empty); }
        }

        /// <summary>Son tarih (gg.aa.yyyy, isteğe bağlı).</summary>
        public string NewTaskDueText
        {
            get { return newTaskDueText; }
            set { SetProperty(ref newTaskDueText, value ?? string.Empty); }
        }

        /// <summary>
        /// İşi gösterilen stokun ya da projenin ana notuna ekler. Metindeki <c>@Ad Soyad</c> kişi olarak ayrılır
        /// (<c>@Ad_Soyad</c> yazılır). Eklendiyse <c>true</c>.
        /// </summary>
        public bool AddTask()
        {
            var folder = selectedNode?.Path ?? string.Empty;
            var kind = KindOf(folder);
            var text = NewTaskText.Trim();
            if (text.Length == 0 || !kind.HasValue)
            {
                return false;
            }

            DateTime? due = null;
            if (NewTaskDueText.Trim().Length > 0)
            {
                due = ParseDay(NewTaskDueText);
                if (!due.HasValue)
                {
                    Status = Resources.Task_BadDate;
                    return false;
                }
            }

            // Bilinen kişi adı (en uzun önce: "Ayşe Demir", "Ayşe" değil) metinden ayrılır.
            var person = string.Empty;
            foreach (var name in PersonSuggestions().Select(s => s.Key).OrderByDescending(n => n.Length))
            {
                var at = text.IndexOf("@" + name, StringComparison.CurrentCultureIgnoreCase);
                if (at >= 0)
                {
                    person = name;
                    text = (text.Substring(0, at) + text.Substring(at + name.Length + 1)).Trim();
                    break;
                }
            }

            var name2 = Path.GetFileName(folder.TrimEnd('\\'));
            string notePath;
            if (kind.Value == RecordKind.Inventory)
            {
                var record = JsonFile.TryRead<InventoryRecord>(InventoryService.RecordFile(folder));
                var number = record?.Number ?? name2;
                var found = NoteFile.Find(folder, number);
                notePath = found.Length > 0 ? found : RecordContents.NotePath(folder, number);
            }
            else
            {
                var found = NoteFile.Find(folder, name2);
                notePath = found.Length > 0 ? found : Path.Combine(folder, name2 + ".md");
            }

            var error = TaskList.Add(notePath, text, person, due);
            if (error.Length > 0)
            {
                Status = string.Format(Resources.Error_Io, error);
                return false;
            }

            NewTaskText = string.Empty;
            NewTaskDueText = string.Empty;
            Done(string.Format(Resources.Task_Added, text));
            RefreshShowing(folder);
            return true;
        }

        /// <summary>İşi bitti / açık yapar (yalnız o satırın kutusu değişir). Değiştiyse <c>true</c>.</summary>
        public bool SetTaskDone(OpenTaskRow row, bool done)
        {
            if (row == null || row.IsDone == done)
            {
                return false;
            }

            var error = TaskList.SetDone(row.NotePath, row.Task.Line, row.Task.RawLine, done, DateTime.Today);
            var folder = selectedNode?.Path ?? string.Empty;
            if (error == "changed")
            {
                Status = Resources.Task_Changed;
                RefreshShowing(folder);
                return false;
            }

            if (error.Length > 0)
            {
                Status = string.Format(Resources.Error_Io, error);
                return false;
            }

            Done(string.Format(done ? Resources.Task_Closed : Resources.Task_Reopened, row.Text));
            RefreshShowing(folder);
            return true;
        }
    }
}