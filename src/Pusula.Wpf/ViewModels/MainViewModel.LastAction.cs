using System;
using System.IO;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>
    /// Durum çubuğunda son yapılan iş ("AB-7000-0002 kaydedildi · 21:55"): başarılı her yazma işinde güncellenir, geçici
    /// mesaj silinse de kalır, ayarlarda saklanır (uygulama yeniden açılınca da görünür). Hata ve uyarılar buraya düşmez.
    /// </summary>
    public sealed partial class MainViewModel
    {
        private string lastAction = string.Empty;

        /// <summary>Son yapılan iş ve saati; yoksa boş.</summary>
        public string LastAction
        {
            get
            {
                if (lastAction.Length == 0 && settings.LastAction.Length > 0)
                {
                    lastAction = settings.LastAction;
                }

                return lastAction;
            }

            private set
            {
                if (SetProperty(ref lastAction, value))
                {
                    OnPropertyChanged(nameof(HasLastAction));
                }
            }
        }

        /// <summary>Gösterilecek son iş var mı.</summary>
        public bool HasLastAction
        {
            get { return LastAction.Length > 0; }
        }

        /// <summary>Not penceresinde not kaydedildi (son yapılan iş olarak yazılır).</summary>
        public void NoteSaved(string path)
        {
            Done(string.Format(Resources.Note_SavedAction, Path.GetFileName(path)), path);
        }

        /// <summary>Son işin yazdığı dosya var mı (durum çubuğunda tıklanabilir).</summary>
        public bool HasLastActionPath
        {
            get { return (settings.LastActionPath ?? string.Empty).Length > 0 && File.Exists(settings.LastActionPath); }
        }

        /// <summary>Son işin yazdığı dosyayı açar (not uygulamasında).</summary>
        public void OpenLastAction()
        {
            var path = settings.LastActionPath ?? string.Empty;
            if (path.Length > 0)
            {
                OpenNoteOrFolder(path, Path.GetDirectoryName(path) ?? string.Empty);
            }
        }

        /// <summary>Soldaki geçici mesaj: son iş değilse (hata, uyarı) gösterilir; son işle aynıysa tekrar yazılmaz.</summary>
        public string StatusNote
        {
            get { return Status.Length > 0 && !lastActionMessage.Equals(Status) ? Status : string.Empty; }
        }

        /// <summary>Geçici mesaj var mı.</summary>
        public bool HasStatusNote
        {
            get { return StatusNote.Length > 0; }
        }

        private string lastActionMessage = string.Empty;

        // Başarılı iş: son yapılan iş olarak saatiyle (ve yazdığı dosyayla) saklanır.
        private void Done(string message, string path = "")
        {
            lastActionMessage = message;
            settings.LastActionPath = path ?? string.Empty;
            Status = message;
            OnPropertyChanged(nameof(HasLastActionPath));
            var stamp = DateTime.Now;
            var text = string.Format(Resources.LastAction_Format, message, stamp.Date == DateTime.Today ? stamp.ToString("HH:mm") : stamp.ToString(Resources.Format_Date + " HH:mm"));
            LastAction = text;
            settings.LastAction = text;
            settings.Save();
        }
    }
}
