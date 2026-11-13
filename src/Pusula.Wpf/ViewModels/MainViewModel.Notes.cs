using System;
using System.IO;
using System.Linq;
using Pusula.Core.Tracking;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Kısa notun yazılacağı yer.</summary>
    public enum QuickNoteTarget
    {
        /// <summary>Proje (<c>proje.json</c>).</summary>
        Project,

        /// <summary>Stok (<c>stok.json</c>).</summary>
        Stock,

        /// <summary>Envanter kaydı (<c>kayit.json</c>).</summary>
        Record,
    }

    /// <summary>Ana görünüm: uygulamadan yazılan kısa notlar ve tablodan düğüme gitme.</summary>
    public sealed partial class MainViewModel
    {
        private bool isQuickNoteOpen;
        private string quickNoteTitle = string.Empty;
        private string quickNoteText = string.Empty;
        private QuickNoteTarget quickNoteTarget;
        private string quickNoteFolder = string.Empty;

        /// <summary>Tablodan "Not ekle" diyaloğu açık mı.</summary>
        public bool IsQuickNoteOpen
        {
            get { return isQuickNoteOpen; }
            set { SetDialogOpen(ref isQuickNoteOpen, value); }
        }

        /// <summary>Diyalog başlığı ("Not ekle · AB-7000-0002").</summary>
        public string QuickNoteTitle
        {
            get { return quickNoteTitle; }
            private set { SetProperty(ref quickNoteTitle, value); }
        }

        /// <summary>Diyalogda yazılan not.</summary>
        public string QuickNoteText
        {
            get { return quickNoteText; }
            set { SetProperty(ref quickNoteText, value ?? string.Empty); }
        }

        /// <summary>Tablodaki bir satır için "Not ekle" diyaloğunu açar.</summary>
        public void OpenQuickNote(QuickNoteTarget target, string folder, string label)
        {
            quickNoteTarget = target;
            quickNoteFolder = folder;
            QuickNoteTitle = string.Format(Resources.QuickNote_DialogTitle, label);
            QuickNoteText = string.Empty;
            IsQuickNoteOpen = true;
        }

        /// <summary>Diyalog onaylandı: notu yazar.</summary>
        public void ConfirmQuickNote()
        {
            AddQuickNote(quickNoteTarget, quickNoteFolder, QuickNoteText);
        }

        /// <summary>Gösterilen sayfaya (proje, stok ya da envanter kaydı) kısa not ekler. Yazıldıysa <c>true</c>.</summary>
        public bool AddQuickNoteHere(QuickNoteTarget target, string text)
        {
            string folder;
            switch (target)
            {
                case QuickNoteTarget.Stock:
                    folder = Detail.StockPath;
                    break;
                case QuickNoteTarget.Record:
                    folder = Inventory.Item?.Folder ?? string.Empty;
                    break;
                default:
                    folder = selectedNode != null && selectedNode.Kind == Core.Tracking.FolderKind.Project ? selectedNode.Path : string.Empty;
                    break;
            }

            return folder.Length > 0 && AddQuickNote(target, folder, text);
        }

        /// <summary>Verilen klasöre kısa not ekler ve gösterilen sayfayı yeniler. Yazıldıysa <c>true</c>.</summary>
        public bool AddQuickNote(QuickNoteTarget target, string folder, string text)
        {
            var note = QuickNote.Create(text, CurrentUser, DateTime.Now);
            if (note.Text.Length == 0)
            {
                return false;
            }

            bool written;
            switch (target)
            {
                case QuickNoteTarget.Stock:
                    written = QuickNotes.AddToStock(folder, note);
                    break;
                case QuickNoteTarget.Record:
                    written = QuickNotes.AddToRecord(folder, note);
                    break;
                default:
                    written = QuickNotes.AddToProject(folder, note);
                    break;
            }

            if (!written)
            {
                Status = string.Format(Resources.Error_Io, folder);
                return false;
            }

            Done(string.Format(Resources.QuickNote_Added, Path.GetFileName(folder)));
            if (target == QuickNoteTarget.Record)
            {
                // Kayıt listesi önbellekte; yeniden okunur, gösterilen kayıt seçili kalır.
                var shown = Inventory.Item?.Folder;
                Refresh();
                if (shown != null)
                {
                    ShowInventory(shown);
                }
            }
            else
            {
                Detail.Reload();
            }

            return true;
        }

        /// <summary>Ağaçta yolu verilen düğümü bulur, üstlerini açar ve seçer (tablodan stoka gitmek için).</summary>
        public void SelectNodeByPath(string path)
        {
            foreach (var root in Nodes)
            {
                if (Select(root, path))
                {
                    return;
                }
            }
        }

        private bool Select(TreeNodeViewModel node, string path)
        {
            if (string.Equals(node.Path, path, StringComparison.OrdinalIgnoreCase))
            {
                node.IsSelected = true;
                SelectedNode = node;
                return true;
            }

            if (node.Children.Any(child => Select(child, path)))
            {
                node.IsExpanded = true;
                return true;
            }

            return false;
        }
    }
}
