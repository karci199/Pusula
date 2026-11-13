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
            Done(string.Format(Resources.Note_SavedAction, Path.GetFileName(path)));
        }

        // Başarılı iş: durum mesajı gösterilir ve son yapılan iş olarak saatiyle saklanır.
        private void Done(string message)
        {
            Status = message;
            var stamp = DateTime.Now;
            var text = string.Format(Resources.LastAction_Format, message, stamp.Date == DateTime.Today ? stamp.ToString("HH:mm") : stamp.ToString(Resources.Format_Date + " HH:mm"));
            LastAction = text;
            settings.LastAction = text;
            settings.Save();
        }
    }
}
