using System;
using System.Collections.Generic;
using Pusula.Wpf.Mvvm;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Parçalı süzgecin bir seçeneği (Kayıtlar · araç türü: "Jig · 3").</summary>
    public sealed class HomeRecordFilter : ViewModelBase
    {
        private readonly Action<HomeRecordFilter> chosen;
        private bool isSelected;

        /// <summary>Seçeneği kurar; seçilince <paramref name="chosen"/> çağrılır.</summary>
        public HomeRecordFilter(string key, string label, int count, Action<HomeRecordFilter> chosen)
        {
            Key = key;
            Label = label;
            Count = count;
            this.chosen = chosen;
        }

        /// <summary>Tür anahtarı; "Hepsi" için boş.</summary>
        public string Key { get; }

        /// <summary>Görünen ad.</summary>
        public string Label { get; }

        /// <summary>Kayıt sayısı.</summary>
        public int Count { get; }

        /// <summary>Seçili mi.</summary>
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (SetProperty(ref isSelected, value) && value)
                {
                    chosen(this);
                }
            }
        }
    }
}
