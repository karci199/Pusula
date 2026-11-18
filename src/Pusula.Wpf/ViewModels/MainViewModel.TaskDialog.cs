using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Pusula.Core.Inventory;
using Pusula.Core.Notes;
using Pusula.Core.Tracking;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>
    /// "Yapılacak iş ekle" penceresi: iş, işlem türü, açıklama, kişiler (varsayılan kullanıcının kendisi), son tarih,
    /// ilgili doğrulama maddesi ve etiketler. İş, kaydın ana notundaki <c>## Yapılacaklar</c> bölümüne tek satır
    /// (<c>- [ ] Hazırlık: iş @kişi #etiket [[DGÖ-1]] 📅 tarih</c>) ve altında girintili açıklama olarak yazılır.
    /// </summary>
    public sealed partial class MainViewModel
    {
        private bool isTaskDialogOpen;
        private string taskFolder = string.Empty;
        private string taskTitle = string.Empty;
        private string taskKind = string.Empty;
        private string taskBody = string.Empty;
        private string taskPeople = string.Empty;
        private string taskDueText = string.Empty;
        private string taskItem = string.Empty;
        private string taskTags = string.Empty;
        private string taskTarget = string.Empty;

        /// <summary>Pencere açık mı.</summary>
        public bool IsTaskDialogOpen
        {
            get { return isTaskDialogOpen; }
            set { SetProperty(ref isTaskDialogOpen, value); }
        }

        /// <summary>Alt başlık: hangi kaydın notuna yazılacağı.</summary>
        public string TaskTarget
        {
            get { return taskTarget; }
            private set { SetProperty(ref taskTarget, value); }
        }

        /// <summary>İş (zorunlu, tek satır).</summary>
        public string TaskTitle
        {
            get
            {
                return taskTitle;
            }

            set
            {
                if (SetProperty(ref taskTitle, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(TaskPrimaryText));
                }
            }
        }

        /// <summary>İşlem türü (isteğe bağlı; iş bitince çizelgeye bu türle düşer).</summary>
        public string TaskKind
        {
            get
            {
                return taskKind;
            }

            set
            {
                if (SetProperty(ref taskKind, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(TaskKindOption));
                }
            }
        }

        /// <summary>Seçili işlem türü (açılır liste).</summary>
        public EventKindOption? TaskKindOption
        {
            get { return EventKinds.Find(taskKind); }
            set { TaskKind = value?.Label ?? string.Empty; }
        }

        /// <summary>Açıklama (isteğe bağlı; notta işin altına girintili yazılır).</summary>
        public string TaskBody
        {
            get { return taskBody; }
            set { SetProperty(ref taskBody, value ?? string.Empty); }
        }

        /// <summary>Kişiler metni (<c>@katan @Ali_Demirci</c>); ilki sorumludur.</summary>
        public string TaskPeople
        {
            get { return taskPeople; }
            set { SetProperty(ref taskPeople, value ?? string.Empty); }
        }

        /// <summary>Son tarih (gg.aa.yyyy, boş olabilir).</summary>
        public string TaskDueText
        {
            get
            {
                return taskDueText;
            }

            set
            {
                if (SetProperty(ref taskDueText, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(TaskDueChoices));
                    OnPropertyChanged(nameof(TaskPrimaryText));
                }
            }
        }

        /// <summary>Son tarih kısayolları: Bugün · Yarın · Bu hafta (cuma) · Gelecek hafta · Yok; yazılan tarihe uyan seçili görünür.</summary>
        public IList<ChoiceOption> TaskDueChoices
        {
            get
            {
                var today = DateTime.Today;
                var friday = today.AddDays(((int)DayOfWeek.Friday - (int)today.DayOfWeek + 7) % 7);

                var choices = new List<Tuple<string, DateTime?>>
                {
                    Tuple.Create(Resources.Task_DueToday, (DateTime?)today),
                    Tuple.Create(Resources.Task_DueTomorrow, (DateTime?)today.AddDays(1)),
                    Tuple.Create(Resources.Task_DueWeek, (DateTime?)friday),
                    Tuple.Create(Resources.Task_DueNext, (DateTime?)today.AddDays(7)),
                    Tuple.Create(Resources.Task_DueNone, (DateTime?)null),
                };
                var current = taskDueText.Trim().Length == 0 ? (DateTime?)null : ParseDay(taskDueText);
                var matched = false;
                var result = new List<ChoiceOption>();
                foreach (var choice in choices)
                {
                    var text = choice.Item2.HasValue ? choice.Item2.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) : string.Empty;
                    var on = !matched && (choice.Item2.HasValue ? current.HasValue && current.Value == choice.Item2.Value : taskDueText.Trim().Length == 0);
                    matched = matched || on;
                    result.Add(new ChoiceOption(text, choice.Item1, on, o => TaskDueText = o.Key));
                }

                return result;
            }
        }

        /// <summary>İlgili madde seçenekleri: "— Yok" ve bu stoktaki TG no'lu maddeler ("DGÖ-1 · Yanca hüzme genişliği").</summary>
        public ObservableCollection<ChoiceOption> TaskItemOptions { get; } = new ObservableCollection<ChoiceOption>();

        /// <summary>Seçili ilgili madde (anahtarı TG no; boş = yok).</summary>
        public ChoiceOption? TaskItemOption
        {
            get { return TaskItemOptions.FirstOrDefault(o => o.Key == taskItem) ?? TaskItemOptions.FirstOrDefault(); }
            set { taskItem = value?.Key ?? string.Empty; }
        }

        /// <summary>Etiketler (<c>#SATIMO #VNA</c>).</summary>
        public string TaskTags
        {
            get { return taskTags; }
            set { SetProperty(ref taskTags, value ?? string.Empty); }
        }

        /// <summary>Birincil düğme metni: iş yazıldıysa ve tarih geçerliyse "Ekle", değilse boş (düğme gizli).</summary>
        public string TaskPrimaryText
        {
            get { return taskTitle.Trim().Length > 0 && (taskDueText.Trim().Length == 0 || ParseDay(taskDueText).HasValue) ? Resources.Journal_Add : string.Empty; }
        }

        /// <summary>Gösterilen kaydın (stok, proje ya da envanter kaydı) notuna iş eklemek için pencereyi açar.</summary>
        public bool OpenTaskDialogHere()
        {
            var folder = Inventory.Item?.Folder ?? (selectedNode?.Path ?? string.Empty);
            if (!KindOf(folder).HasValue)
            {
                return false;
            }

            taskFolder = folder;
            ReloadKinds();
            TaskTarget = string.Format(Resources.TaskDialog_Target, Path.GetFileName(folder.TrimEnd('\\')));
            TaskTitle = string.Empty;
            TaskKind = string.Empty;
            TaskBody = string.Empty;
            TaskPeople = CurrentUser.Length > 0 ? "@" + CurrentUser.Replace(' ', '_') : string.Empty;
            TaskDueText = string.Empty;
            TaskTags = string.Empty;
            taskItem = string.Empty;
            TaskItemOptions.Clear();
            TaskItemOptions.Add(new ChoiceOption(string.Empty, Resources.Task_NoItem, true, o => { }));
            var items = string.Equals(verificationStock, folder, StringComparison.OrdinalIgnoreCase) ? verificationItems : new List<Pusula.Core.Testing.VerificationItem>();
            foreach (var item in items.Where(i => i.TgNo.Length > 0))
            {
                var title = item.Text.Length > 0 ? item.Text : item.SubGroup;
                TaskItemOptions.Add(new ChoiceOption(item.TgNo, title.Length > 0 ? item.TgNo + " · " + title : item.TgNo, false, o => { }));
            }

            OnPropertyChanged(nameof(TaskItemOption));
            OnPropertyChanged(nameof(TaskPrimaryText));
            IsTaskDialogOpen = true;
            return true;
        }

        /// <summary>İşi nota yazar; yazıldıysa <c>true</c> (pencere kapanır).</summary>
        public bool ConfirmTaskDialog()
        {
            var title = string.Join(" ", taskTitle.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
            if (title.Length == 0 || taskFolder.Length == 0)
            {
                return false;
            }

            DateTime? due = null;
            if (taskDueText.Trim().Length > 0)
            {
                due = ParseDay(taskDueText);
                if (!due.HasValue)
                {
                    Status = Resources.Task_BadDate;
                    return false;
                }
            }

            // Satır: "Hazırlık: iş @kişi … #etiket … [[DGÖ-1]]" (ilk kişi sorumlu; bağ satırın sonunda).
            var parts = new List<string>();
            parts.Add((taskKind.Trim().Length > 0 ? taskKind.Trim() + ": " : string.Empty) + title);
            parts.AddRange(taskPeople.Split(new[] { ' ', ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim().TrimStart('@', '#')).Where(p => p.Length > 0)
                .Distinct(StringComparer.CurrentCultureIgnoreCase).Select(p => "@" + p));
            parts.AddRange(Pusula.Core.Markdown.MarkdownEditing.ParseTags(taskTags).Select(t => "#" + t));
            if (taskItem.Length > 0)
            {
                parts.Add("[[" + taskItem + "]]");
            }

            var notePath = TaskNotePath(taskFolder);
            var error = TaskList.Add(notePath, string.Join(" ", parts), string.Empty, due, taskBody);
            if (error.Length > 0)
            {
                Status = string.Format(Resources.Error_Io, error);
                return false;
            }

            Done(string.Format(Resources.Task_Added, title), notePath);
            RefreshShowing(taskFolder);
            return true;
        }

        // Kaydın ana notu: envanter kaydında numarasıyla, stok / projede klasör adıyla (yoksa o adla açılır).
        private string TaskNotePath(string folder)
        {
            var name = Path.GetFileName(folder.TrimEnd('\\'));
            if (KindOf(folder) == RecordKind.Inventory)
            {
                var record = JsonFile.TryRead<InventoryRecord>(InventoryService.RecordFile(folder));
                var number = record?.Number ?? name;
                var found = NoteFile.Find(folder, number);
                return found.Length > 0 ? found : RecordContents.NotePath(folder, number);
            }

            var note = NoteFile.Find(folder, name);
            return note.Length > 0 ? note : Path.Combine(folder, name + ".md");
        }
    }
}
