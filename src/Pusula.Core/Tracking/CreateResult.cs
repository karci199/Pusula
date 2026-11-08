namespace Pusula.Core.Tracking
{
    /// <summary>
    /// Oluşturma işleminin neden başarısız olduğunu söyler. Kullanıcıya gösterilecek metin arayüzde seçilir.
    /// </summary>
    public enum CreateError
    {
        /// <summary>Hata yok.</summary>
        None,

        /// <summary>Üst klasör yok.</summary>
        ParentMissing,

        /// <summary>Numara boş.</summary>
        NumberEmpty,

        /// <summary>Ad boş.</summary>
        NameEmpty,

        /// <summary>Numara ya da ad klasör adında kullanılamayan karakter içeriyor.</summary>
        InvalidCharacters,

        /// <summary>Aynı adda klasör zaten var; ezilmez.</summary>
        AlreadyExists,

        /// <summary>Disk hatası (izin, yol uzunluğu vb.).</summary>
        IoFailure,

        /// <summary>Klasör zaten proje ya da stok olarak işaretli.</summary>
        AlreadyMarked,
    }

    /// <summary>
    /// Proje ya da stok oluşturmanın sonucu. Hata fırlatmak yerine bu nesne döner.
    /// </summary>
    public sealed class CreateResult
    {
        private CreateResult(CreateError error, string folder, string detail)
        {
            Error = error;
            Folder = folder;
            Detail = detail;
        }

        /// <summary>Hata türü; başarılıysa <see cref="CreateError.None"/>.</summary>
        public CreateError Error { get; }

        /// <summary>Oluşturulan (ya da oluşturulmak istenen) klasör.</summary>
        public string Folder { get; }

        /// <summary>Disk hatasında sistemin verdiği ayrıntı; diğer durumlarda boş.</summary>
        public string Detail { get; }

        /// <summary>İşlem başarılı mı.</summary>
        public bool Succeeded
        {
            get { return Error == CreateError.None; }
        }

        /// <summary>Başarılı sonuç.</summary>
        public static CreateResult Success(string folder)
        {
            return new CreateResult(CreateError.None, folder, string.Empty);
        }

        /// <summary>Başarısız sonuç.</summary>
        public static CreateResult Fail(CreateError error, string folder, string detail = "")
        {
            return new CreateResult(error, folder, detail);
        }
    }
}
