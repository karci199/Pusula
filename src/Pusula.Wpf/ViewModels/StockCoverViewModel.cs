using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Pusula.Core.Notes;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Stok sayfasındaki fotoğraf şeridinde bir küçük resim: yol, görünüş adı ("Ön", "Arka 2"), seçili mi.</summary>
    public sealed class PhotoThumb : ViewModelBase
    {
        private bool isSelected;

        /// <summary>Küçük resmi kurar.</summary>
        public PhotoThumb(string path, string label)
        {
            Path = path;
            Label = label;
        }

        /// <summary>Şeride sığmayanları gösteren "+N" kutusu (tıklayınca klasör açılır).</summary>
        public static PhotoThumb More(string text)
        {
            return new PhotoThumb(string.Empty, text) { IsMore = true };
        }

        /// <summary>"+N" kutusu mu.</summary>
        public bool IsMore { get; private set; }

        /// <summary>Fotoğrafın tam yolu.</summary>
        public string Path { get; }

        /// <summary>Altındaki yazı: görünüş adı, kurala uymuyorsa dosya adı.</summary>
        public string Label { get; }

        /// <summary>Büyük kutuda gösterilen bu mu.</summary>
        public bool IsSelected
        {
            get { return isSelected; }
            set { SetProperty(ref isSelected, value); }
        }
    }

    /// <summary>
    /// Stok sayfasının sol üstündeki TEÜ görünümü: fotoğraflar (<c>01_Ortak\Fotograflar</c>), büyükte kapak ya da seçilen,
    /// altında görünüş adlı şerit; ayrıca notun ilk satırları ve doküman klasörü.
    /// </summary>
    public sealed class StockCoverViewModel : ViewModelBase
    {
        private const int StripCount = 4;
        private string stockFolder = string.Empty;
        private string cover = string.Empty;
        private string selected = string.Empty;
        private string selectedLabel = string.Empty;
        private string noteFile = string.Empty;
        private string noteExcerpt = string.Empty;
        private string photosFolder = string.Empty;
        private string photosHome = string.Empty;
        private string photosText = string.Empty;
        private string moreText = string.Empty;
        private string documentsFolder = string.Empty;
        private string documentsText = string.Empty;
        private string hiddenText = string.Empty;
        private string noCoverText = string.Empty;

        /// <summary>Stok klasörü.</summary>
        public string StockFolder
        {
            get { return stockFolder; }
        }

        /// <summary>Kapak fotoğrafı (sıradaki ilk fotoğraf); yoksa boş.</summary>
        public string CoverPhoto
        {
            get { return cover; }
            private set
            {
                if (SetProperty(ref cover, value))
                {
                    OnPropertyChanged(nameof(HasCover));
                }
            }
        }

        /// <summary>Kapak var mı (en az bir fotoğraf).</summary>
        public bool HasCover
        {
            get { return cover.Length > 0; }
        }

        /// <summary>Büyük kutuda gösterilen fotoğraf (başta kapak, şeritten seçilen); kapak yoksa boş (genel resim görünür).</summary>
        public string SelectedPhoto
        {
            get { return selected; }
            private set
            {
                if (SetProperty(ref selected, value))
                {
                    OnPropertyChanged(nameof(HasSelectedPhoto));
                }
            }
        }

        /// <summary>Büyük kutuda fotoğraf var mı; yoksa genel resim ve kapak adının ipucu görünür.</summary>
        public bool HasSelectedPhoto
        {
            get { return selected.Length > 0; }
        }

        /// <summary>Kapak yokken büyük kutudaki ipucu: "Kapak: AB-7000-0002_kapak.jpg".</summary>
        public string NoCoverText
        {
            get { return noCoverText; }
            private set { SetProperty(ref noCoverText, value); }
        }

        /// <summary>Arayüzde görünmeyen (numarasız) fotoğraflar: "3 fotoğraf daha klasörde"; yoksa boş.</summary>
        public string HiddenText
        {
            get { return hiddenText; }
            private set
            {
                if (SetProperty(ref hiddenText, value))
                {
                    OnPropertyChanged(nameof(HasHidden));
                }
            }
        }

        /// <summary>Görünmeyen fotoğraf var mı.</summary>
        public bool HasHidden
        {
            get { return hiddenText.Length > 0; }
        }

        /// <summary>Büyük kutudaki fotoğrafın görünüş adı ("Kapak").</summary>
        public string SelectedLabel
        {
            get { return selectedLabel; }
            private set { SetProperty(ref selectedLabel, value); }
        }

        /// <summary>Şeritteki küçük resimler (kapak dahil ilk dördü).</summary>
        public ObservableCollection<PhotoThumb> Thumbs { get; } = new ObservableCollection<PhotoThumb>();

        /// <summary>Şeride sığmayanların sayısı ("+2"); yoksa boş.</summary>
        public string MoreText
        {
            get { return moreText; }
            private set
            {
                if (SetProperty(ref moreText, value))
                {
                    OnPropertyChanged(nameof(HasMore));
                }
            }
        }

        /// <summary>Şeride sığmayan fotoğraf var mı.</summary>
        public bool HasMore
        {
            get { return moreText.Length > 0; }
        }

        /// <summary>Şerit görünür mü (en az bir numaralı fotoğraf).</summary>
        public bool HasStrip
        {
            get { return Thumbs.Count > 0; }
        }

        /// <summary>Notun yolu; yoksa boş.</summary>
        public string NoteFile
        {
            get { return noteFile; }
            private set
            {
                if (SetProperty(ref noteFile, value))
                {
                    OnPropertyChanged(nameof(HasNote));
                }
            }
        }

        /// <summary>Not var mı.</summary>
        public bool HasNote
        {
            get { return noteFile.Length > 0; }
        }

        /// <summary>Not gövdesinin başı; boşsa "Not henüz boş.".</summary>
        public string NoteExcerpt
        {
            get { return noteExcerpt; }
            private set { SetProperty(ref noteExcerpt, value); }
        }

        /// <summary>Fotoğrafların okunduğu klasör; yoksa boş.</summary>
        public string PhotosFolder
        {
            get { return photosFolder; }
            private set
            {
                if (SetProperty(ref photosFolder, value))
                {
                    OnPropertyChanged(nameof(HasPhotosFolder));
                }
            }
        }

        /// <summary>Fotoğraf klasörü var mı.</summary>
        public bool HasPhotosFolder
        {
            get { return photosFolder.Length > 0; }
        }

        /// <summary>Fotoğrafların durması gereken klasör (<c>01_Ortak\Fotograflar</c>); 📁 bunu açar (yoksa açılır).</summary>
        public string PhotosHome
        {
            get { return photosHome; }
            private set { SetProperty(ref photosHome, value); }
        }

        /// <summary>"Fotoğraflar · 5".</summary>
        public string PhotosText
        {
            get { return photosText; }
            private set { SetProperty(ref photosText, value); }
        }

        /// <summary>Doküman klasörü; yoksa boş.</summary>
        public string DocumentsFolder
        {
            get { return documentsFolder; }
            private set
            {
                if (SetProperty(ref documentsFolder, value))
                {
                    OnPropertyChanged(nameof(HasDocumentsFolder));
                }
            }
        }

        /// <summary>Doküman klasörü var mı.</summary>
        public bool HasDocumentsFolder
        {
            get { return documentsFolder.Length > 0; }
        }

        /// <summary>"Dokümanlar · 2".</summary>
        public string DocumentsText
        {
            get { return documentsText; }
            private set { SetProperty(ref documentsText, value); }
        }

        /// <summary>Fotoğrafın görünüş adı: "Kapak", "Ön", "Arka 2"; kurala uymuyorsa dosya adı.</summary>
        public static string LabelOf(string path)
        {
            int number;
            var key = StockCover.ViewOf(path, out number);
            var index = Array.IndexOf(StockCover.ViewKeys, key);
            var names = new[]
            {
                Resources.Photo_Cover, Resources.Photo_Front, Resources.Photo_Back, Resources.Photo_Side,
                Resources.Photo_Top, Resources.Photo_Assembly, Resources.Photo_Inside, Resources.Photo_Label,
            };
            if (index < 0)
            {
                return Path.GetFileNameWithoutExtension(path);
            }

            return number > 0 ? names[index] + " " + number.ToString(CultureInfo.CurrentCulture) : names[index];
        }

        /// <summary>Şeritten bir fotoğrafı büyük kutuda gösterir; seçili olana yeniden basılırsa kapağa döner.</summary>
        public void Select(PhotoThumb thumb)
        {
            if (thumb == null)
            {
                return;
            }

            if (thumb.IsSelected)
            {
                ShowCover();
                return;
            }

            foreach (var t in Thumbs)
            {
                t.IsSelected = ReferenceEquals(t, thumb);
            }

            SelectedPhoto = thumb.Path;
            SelectedLabel = thumb.Label;
        }

        // Büyük kutuda kapak (yoksa genel resim); şeritte seçim yok.
        private void ShowCover()
        {
            foreach (var t in Thumbs)
            {
                t.IsSelected = false;
            }

            SelectedPhoto = cover;
            SelectedLabel = cover.Length > 0 ? Resources.Photo_Cover : string.Empty;
        }

        /// <summary>Kartı temizler.</summary>
        public void Clear()
        {
            stockFolder = string.Empty;
            CoverPhoto = string.Empty;
            SelectedPhoto = string.Empty;
            SelectedLabel = string.Empty;
            NoteFile = string.Empty;
            NoteExcerpt = string.Empty;
            PhotosFolder = string.Empty;
            PhotosHome = string.Empty;
            PhotosText = string.Empty;
            MoreText = string.Empty;
            DocumentsFolder = string.Empty;
            DocumentsText = string.Empty;
            HiddenText = string.Empty;
            NoCoverText = string.Empty;
            Thumbs.Clear();
            OnPropertyChanged(nameof(HasStrip));
        }

        /// <summary>Okunan kapağı gösterir.</summary>
        public void Show(StockCover read, string folder)
        {
            Clear();
            stockFolder = folder;
            NoteFile = read.NoteFile;
            NoteExcerpt = read.NoteFile.Length == 0 ? string.Empty : read.NoteExcerpt.Length == 0 ? Resources.Inv_NoteEmpty : read.NoteExcerpt;
            PhotosFolder = read.PhotosFolder;
            PhotosHome = read.PhotosHome.Length > 0 ? read.PhotosHome : folder;
            // 21.11.2026: büyükte kapak (<stok>_kapak); şeritte numaralı fotoğraflar; numarasızlar görünmez.
            CoverPhoto = read.Cover;
            NoCoverText = string.Format(CultureInfo.CurrentCulture, Resources.Stock_NoCoverPattern, Path.GetFileName(folder.TrimEnd('\\')));
            foreach (var photo in read.Strip.Take(StripCount))
            {
                Thumbs.Add(new PhotoThumb(photo, StockCover.StripLabelOf(photo)));
            }

            ShowCover();
            MoreText = read.Strip.Count > StripCount ? "+" + (read.Strip.Count - StripCount).ToString(CultureInfo.CurrentCulture) : string.Empty;
            HiddenText = read.HiddenCount > 0 ? string.Format(CultureInfo.CurrentCulture, Resources.Stock_PhotosHidden, read.HiddenCount) : string.Empty;
            if (HasMore)
            {
                Thumbs.Add(PhotoThumb.More(MoreText));
            }

            OnPropertyChanged(nameof(HasStrip));
            PhotosText = string.Format(Resources.Stock_Photos, read.Photos.Count);
            DocumentsFolder = read.DocumentsFolder;
            DocumentsText = string.Format(Resources.Stock_Documents, read.DocumentCount);
        }
    }
}
