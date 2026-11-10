using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Yapılacaklar süzgeci: Açık / Bana ait (açık, kişisi kullanıcı) / Bitenler.</summary>
    public sealed class TaskFilter : ViewModelBase, IFilterOwner
    {
        /// <summary>Açık işler.</summary>
        public const string Open = "open";

        /// <summary>Kullanıcının açık işleri.</summary>
        public const string Mine = "mine";

        /// <summary>Bitenler.</summary>
        public const string Done = "done";

        private readonly List<ICollectionView> views = new List<ICollectionView>();
        private string selected = Open;

        /// <summary>Süzgeci kurar (başta Açık seçili).</summary>
        public TaskFilter()
        {
            Options.Add(new TimelineFilterOption(this, Open, Resources.Task_FilterOpen));
            Options.Add(new TimelineFilterOption(this, Mine, Resources.Task_FilterMine));
            Options.Add(new TimelineFilterOption(this, Done, Resources.Task_FilterDone));
            Options[0].SetSelected(true);
        }

        /// <summary>Seçenekler.</summary>
        public ObservableCollection<TimelineFilterOption> Options { get; } = new ObservableCollection<TimelineFilterOption>();

        /// <summary>Seçili seçeneğin anahtarı.</summary>
        public string Selected
        {
            get { return selected; }
        }

        /// <summary>Seçeneği seçer ve bağlı listeleri yeniden süzer.</summary>
        public void Select(string key)
        {
            if (!Options.Any(o => o.Key == key))
            {
                return;
            }

            selected = key;
            foreach (var option in Options)
            {
                option.SetSelected(option.Key == key);
            }

            OnPropertyChanged(nameof(Selected));
            Refresh();
        }

        /// <summary>Bağlı listeleri yeniden süzer (bir iş kapatılınca / açılınca).</summary>
        public void Refresh()
        {
            foreach (var view in views)
            {
                view.Refresh();
            }
        }

        /// <summary>Satır seçili süzgece uyuyor mu.</summary>
        public bool Matches(object item)
        {
            var row = item as OpenTaskRow;
            if (row == null)
            {
                return true;
            }

            switch (selected)
            {
                case Done:
                    return row.IsDone;
                case Mine:
                    return !row.IsDone && string.Equals(row.PersonName, MainViewModel.CurrentUser, StringComparison.CurrentCultureIgnoreCase);
                default:
                    return !row.IsDone;
            }
        }

        /// <summary>Listenin varsayılan görünümünü bu süzgeçle süzer.</summary>
        public void Attach(IEnumerable list)
        {
            var view = CollectionViewSource.GetDefaultView(list);
            view.Filter = Matches;
            views.Add(view);
        }
    }
}