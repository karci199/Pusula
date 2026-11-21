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

        /// <summary>Büyük kutuda gösterilen fotoğraf (başta kapak, şeritten seçilen).</summary>
        public string SelectedPhoto
        {
            get { return selected; }
            private set { SetProperty(ref selected, value); }
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

        /// <summary>Şerit görünür mü (birden çok fotoğraf).</summary>
        public bool HasStrip
        {
            get { return Thumbs.Count > 1; }
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

        /// <summary>Şeritten bir fotoğrafı büyük kutuda gösterir.</summary>
        public void Select(PhotoThumb thumb)
        {
            if (thumb == null)
            {
                return;
            }

            foreach (var t in Thumbs)
            {
                t.IsSelected = ReferenceEquals(t, thumb);
            }

            SelectedPhoto = thumb.Path;
            SelectedLabel = thumb.Label;
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
            CoverPhoto = read.Photos.FirstOrDefault() ?? string.Empty;
            foreach (var photo in read.Photos.Take(StripCount))
            {
                Thumbs.Add(new PhotoThumb(photo, LabelOf(photo)));
            }

            if (Thumbs.Count > 0)
            {
                Select(Thumbs[0]);
            }

            MoreText = read.Photos.Count > StripCount ? "+" + (read.Photos.Count - StripCount).ToString(CultureInfo.CurrentCulture) : string.Empty;
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
