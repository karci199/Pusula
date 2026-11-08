using System;

namespace Pusula.Core.Testing
{
    /// <summary>Test koşusunun sonucu (nottaki metinden toleranslı okunur).</summary>
    public enum TestResult
    {
        /// <summary>Sonuç girilmemiş ya da okunamadı.</summary>
        Unknown,

        /// <summary>Geçti.</summary>
        Passed,

        /// <summary>Kaldı.</summary>
        Failed,

        /// <summary>Devam ediyor.</summary>
        InProgress,
    }

    /// <summary>Sonucun nereden okunduğu.</summary>
    public enum TestRunSource
    {
        /// <summary>Yalnızca klasör adından (not yok).</summary>
        Folder,

        /// <summary><c>test.md</c> notundan.</summary>
        Note,

        /// <summary>KIWI'nin <c>_kosu.md</c> dosyasından.</summary>
        Kiwi,
    }

    /// <summary>
    /// Bir test koşusu: <c>&lt;SİSTEM&gt;\&lt;SN&gt;_T&lt;tur&gt;</c> klasörü ve içinden okunan bilgiler.
    /// </summary>
    public sealed class TestRun
    {
        /// <summary>Koşu klasörü.</summary>
        public string Folder { get; set; } = string.Empty;

        /// <summary>Ölçüm sistemi (SATIMO, VNA, CK …), klasör adından.</summary>
        public string System { get; set; } = string.Empty;

        /// <summary>Seri numarası (SN01, SNP1 …), klasör adından.</summary>
        public string SerialNo { get; set; } = string.Empty;

        /// <summary>Tur (deneme sırası), klasör adından.</summary>
        public int Round { get; set; }

        /// <summary>Kalifikasyon klasöründen mi.</summary>
        public bool IsQualification { get; set; }

        /// <summary>Koşu tarihi (nottan; yoksa klasördeki en eski dosyadan).</summary>
        public DateTime Date { get; set; }

        /// <summary>Sonuç.</summary>
        public TestResult Result { get; set; }

        /// <summary>Sonucun nottaki ham metni (örn. "Geçti").</summary>
        public string ResultText { get; set; } = string.Empty;

        /// <summary>Testi yapan.</summary>
        public string Person { get; set; } = string.Empty;

        /// <summary>Test planı adı.</summary>
        public string Plan { get; set; } = string.Empty;

        /// <summary>Kullanılan jig (örn. "JIG-1000-0009 Rev AC"); boş olabilir.</summary>
        public string Jig { get; set; } = string.Empty;

        /// <summary>Kısa açıklama (notun "Sonuç:" satırından ya da ilk paragrafından).</summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>Notun gövdesi (YAML hariç, ham markdown); not yoksa boş.</summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>Koşunun ilk fotoğrafı (<c>Foto\</c> ya da koşu klasöründeki ilk resim); yoksa boş.</summary>
        public string Photo { get; set; } = string.Empty;

        /// <summary>Açılacak not (test.md ya da _kosu.md); yoksa boş.</summary>
        public string NotePath { get; set; } = string.Empty;

        /// <summary>Bilginin kaynağı.</summary>
        public TestRunSource Source { get; set; }
    }
}
