using System;
using System.IO;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Stok sayfasında TEÜ görünümü: fotoğraf klasörü (<c>01_Ortak\Fotograflar</c>) yoksa boş açılır.</summary>
    public sealed partial class MainViewModel
    {
        /// <summary>Gösterilen stokun fotoğraf klasörünü hazırlar (yoksa boş oluşturur); yolu, olmadıysa boş döner.</summary>
        public string EnsureStockPhotosFolder()
        {
            var folder = Detail.Cover.PhotosHome;
            if (folder.Length == 0)
            {
                return string.Empty;
            }

            try
            {
                Pusula.Core.Scanning.DiskWatcher.MarkOwnWrite();
                Directory.CreateDirectory(folder);
                return folder;
            }
            catch (IOException ex)
            {
                Status = string.Format(Resources.Error_Io, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                Status = string.Format(Resources.Error_Io, ex.Message);
            }

            return string.Empty;
        }

        /// <summary>Gösterilen stokun fotoğraf klasörünü Gezgin'de açar (yoksa boş oluşturur).</summary>
        public void OpenStockPhotosFolder()
        {
            var folder = EnsureStockPhotosFolder();
            if (folder.Length > 0)
            {
                OpenPath(folder);
            }
        }
    }
}
