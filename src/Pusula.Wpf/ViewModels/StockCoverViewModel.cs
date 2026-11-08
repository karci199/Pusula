using System.Collections.ObjectModel;
using System.Linq;
using Pusula.Core.Notes;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Stok Genel sekmesinin üstündeki kart: kapak fotoğrafı, notun ilk satırları, fotoğraf şeridi.</summary>
    public sealed class StockCoverViewModel : ViewModelBase
    {
        private const int StripCount = 4;
        private string stockFolder = string.Empty;
        private string cover = string.Empty;
        private string noteFile = string.Empty;
        private string noteExcerpt = string.Empty;
        private string photosFolder = string.Empty;
        private string photosText = string.Empty;

        /// <summary>Stok klasörü.</summary>
        public string StockFolder
        {
            get { return stockFolder; }
        }

        /// <summary>Kapak fotoğrafı (fotoğraf klasöründeki ilk resim); yoksa boş.</summary>
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

        /// <summary>Kapak var mı.</summary>
        public bool HasCover
        {
            get { return cover.Length > 0; }
        }

        /// <summary>Kapaktan sonraki birkaç fotoğraf.</summary>
        public ObservableCollection<string> Strip { get; } = new ObservableCollection<string>();

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

        /// <summary>Fotoğraf klasörü; yoksa boş.</summary>
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

        /// <summary>"Fotoğraflar · 5".</summary>
        public string PhotosText
        {
            get { return photosText; }
            private set { SetProperty(ref photosText, value); }
        }

        /// <summary>Kartı temizler.</summary>
        public void Clear()
        {
            stockFolder = string.Empty;
            CoverPhoto = string.Empty;
            NoteFile = string.Empty;
            NoteExcerpt = string.Empty;
            PhotosFolder = string.Empty;
            PhotosText = string.Empty;
            Strip.Clear();
        }

        /// <summary>Okunan kapağı gösterir.</summary>
        public void Show(StockCover read, string folder)
        {
            Clear();
            stockFolder = folder;
            NoteFile = read.NoteFile;
            NoteExcerpt = read.NoteFile.Length == 0 ? string.Empty : read.NoteExcerpt.Length == 0 ? Resources.Inv_NoteEmpty : read.NoteExcerpt;
            PhotosFolder = read.PhotosFolder;
            CoverPhoto = read.Photos.FirstOrDefault() ?? string.Empty;
            foreach (var photo in read.Photos.Skip(1).Take(StripCount))
            {
                Strip.Add(photo);
            }

            PhotosText = string.Format(Resources.Stock_Photos, read.Photos.Count);
        }
    }
}
