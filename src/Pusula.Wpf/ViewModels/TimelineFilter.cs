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
    /// <summary>Zaman çizelgesinde bir olay: tarihi ve hangi süzgece girdiği.</summary>
    public interface ITimelinePoint
    {
        /// <summary>Olayın tarihi.</summary>
        DateTime SortDate { get; }




        /// <summary>Aşama (durum) olayı mı.</summary>
        bool IsStage { get; }

        /// <summary>Revizyon olayı mı.</summary>
        bool IsRevision { get; }

        /// <summary>Konum değişikliği var mı.</summary>
        bool IsLocationChange { get; }

        /// <summary>Not mu (kısa not ya da revizyon dışındaki günlük olayı).</summary>
        bool IsNoteEntry { get; }

        /// <summary>Bağlı bir kayıttan (stoka bağlı jig / kablo) mı geldi.</summary>
        bool IsLinked { get; }
    }

    /// <summary>Süzgeç seçeneği ("Hepsi", "Aşama" …); bir anda biri seçilidir.</summary>
    public sealed class TimelineFilterOption : ViewModelBase
    {
        private readonly TimelineFilter owner;
        private bool isSelected;

        /// <summary>Seçeneği kurar.</summary>
        public TimelineFilterOption(TimelineFilter owner, string key, string label)
        {
            this.owner = owner;
            Key = key;
            Label = label;
        }

        /// <summary>Anahtar (all, stage, revision, location, note, own).</summary>
        public string Key { get; }

        /// <summary>Görünen ad.</summary>
        public string Label { get; }

        /// <summary>Seçili mi; seçilince ötekiler bırakılır, seçili olan bırakılamaz.</summary>
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (value)
                {
                    owner.Select(Key);
                }
                else
                {
                    // Seçili seçeneğe yeniden tıklanınca seçim kalkmaz (bir seçenek hep seçili).
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>Seçimi süzgeç sınıfı değiştirir.</summary>
        internal void SetSelected(bool value)
        {
            SetProperty(ref isSelected, value, nameof(IsSelected));
        }
    }

    /// <summary>
    /// Zaman çizelgesi süzgeci: Hepsi / Aşama / Revizyon / Konum / Not (stokta ayrıca "Yalnız stok"). Bağlandığı listenin
    /// varsayılan görünümünü süzer; liste değişmez, yalnızca gösterilen satırlar değişir.
    /// </summary>
    public sealed class TimelineFilter : ViewModelBase
    {
        /// <summary>Hepsi.</summary>
        public const string All = "all";

        /// <summary>Aşama olayları.</summary>
        public const string Stage = "stage";

        /// <summary>Revizyonlar.</summary>
        public const string Revision = "revision";

        /// <summary>Konum değişiklikleri.</summary>
        public const string Location = "location";

        /// <summary>Notlar.</summary>
        public const string Note = "note";

        /// <summary>Yalnız sayfanın kendi olayları (bağlı kayıtlarınki değil).</summary>
        public const string Own = "own";

        private readonly List<ICollectionView> views = new List<ICollectionView>();
        private string selected = All;

        /// <summary>Süzgeci kurar; <paramref name="withOwn"/> ise "Yalnız stok" seçeneği de olur.</summary>
        public TimelineFilter(bool withOwn)
        {
            Options.Add(new TimelineFilterOption(this, All, Resources.Timeline_FilterAll));
            Options.Add(new TimelineFilterOption(this, Stage, Resources.Timeline_FilterStage));
            Options.Add(new TimelineFilterOption(this, Revision, Resources.Timeline_FilterRevision));
            Options.Add(new TimelineFilterOption(this, Location, Resources.Timeline_FilterLocation));
            Options.Add(new TimelineFilterOption(this, Note, Resources.Timeline_FilterNote));
            if (withOwn)
            {
                Options.Add(new TimelineFilterOption(this, Own, Resources.Timeline_FilterOwn));
            }

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
            foreach (var view in views)
            {
                view.Refresh();
            }
        }

        /// <summary>Olay seçili süzgece uyuyor mu.</summary>
        public bool Matches(object item)
        {
            var point = item as ITimelinePoint;
            if (point == null)
            {
                return true;
            }

            switch (selected)
            {
                case Stage:
                    return point.IsStage;
                case Revision:
                    return point.IsRevision;
                case Location:
                    return point.IsLocationChange;
                case Note:
                    return point.IsNoteEntry;
                case Own:
                    return !point.IsLinked;
                default:
                    return true;
            }
        }

        /// <summary>Listenin varsayılan görünümünü bu süzgeçle süzer (listeye bağlanan her yer süzülmüş görür).</summary>
        public void Attach(IEnumerable list)
        {
            var view = CollectionViewSource.GetDefaultView(list);
            view.Filter = Matches;
            views.Add(view);
        }
    }
}