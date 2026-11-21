using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Geçici mail okuyucu (Pusula'nın dışında, deneme aracı): Outlook <c>.msg</c> (OLE bileşik dosya, Windows ole32 ile)
/// ve <c>.eml</c> (RFC 822 / MIME, düz metin) dosyalarından konu, tarih, gönderen, alıcılar (ekip = dağıtım listesi),
/// gövde ve ekleri okur. Outlook, lisans ya da kütüphane gerekmez; ağa çıkmaz.
/// </summary>
public static class MailOku
{
    // Bayt ↔ karakter birebir (ISO-8859-1); MIME gövdesini bozmadan metin gibi işlemek için.
    private static readonly Encoding L1 = Encoding.GetEncoding(28591);

    /// <summary>Bir kişi ya da dağıtım listesi.</summary>
    public sealed class Kisi
    {
        /// <summary>Görünen ad.</summary>
        public string Ad = string.Empty;

        /// <summary>E-posta adresi.</summary>
        public string Eposta = string.Empty;

        /// <summary>Dağıtım listesi (ekip) mi.</summary>
        public bool Ekip;

        /// <summary>Alıcı türü: 1 Kime, 2 Bilgi, 3 Gizli.</summary>
        public int Tur = 1;
    }

    /// <summary>Ek dosya.</summary>
    public sealed class Ek
    {
        /// <summary>Dosya adı.</summary>
        public string Ad = string.Empty;

        /// <summary>İçerik.</summary>
        public byte[] Veri = new byte[0];
    }

    /// <summary>Okunan mail.</summary>
    public sealed class Mail
    {
        /// <summary>Konu.</summary>
        public string Konu = string.Empty;

        /// <summary>Gönderen.</summary>
        public Kisi Gonderen = new Kisi();

        /// <summary>Gönderim tarihi (yerel saat).</summary>
        public DateTime? Tarih;

        /// <summary>Düz metin gövde.</summary>
        public string Govde = string.Empty;

        /// <summary>Alıcılar.</summary>
        public List<Kisi> Alicilar = new List<Kisi>();

        /// <summary>Ekler.</summary>
        public List<Ek> Ekler = new List<Ek>();
    }

    // İlk boş olmayan değer; Exchange adresi (/O=…) yerine SMTP adresi tercih edilir.
    private static string Ilk(params string[] degerler)
    {
        var smtp = degerler.FirstOrDefault(d => d != null && d.Contains("@"));
        return smtp ?? degerler.FirstOrDefault(d => !string.IsNullOrEmpty(d)) ?? string.Empty;
    }

    /// <summary>Uzantıya göre .msg ya da .eml okur.</summary>
    public static Mail Oku(string yol)
    {
        return string.Equals(Path.GetExtension(yol), ".msg", StringComparison.OrdinalIgnoreCase) ? Msg.Oku(yol) : Eml.Oku(File.ReadAllBytes(yol));
    }

    // =====================================================================================================  .msg
    /// <summary>Outlook .msg: OLE bileşik dosya; özellikler <c>__substg1.0_ETIKETTUR</c> akışlarında.</summary>
    public static class Msg
    {
        [ComImport, Guid("0000000B-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IStorage
        {
            void CreateStream([MarshalAs(UnmanagedType.LPWStr)] string name, uint mode, uint r1, uint r2, out IStream stream);
            [PreserveSig] int OpenStream([MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr r1, uint mode, uint r2, out IStream stream);
            void CreateStorage([MarshalAs(UnmanagedType.LPWStr)] string name, uint mode, uint r1, uint r2, out IStorage storage);
            [PreserveSig] int OpenStorage([MarshalAs(UnmanagedType.LPWStr)] string name, IStorage priority, uint mode, IntPtr snb, uint r, out IStorage storage);
            void CopyTo(uint ciid, IntPtr rgiid, IntPtr snb, IStorage dest);
            void MoveElementTo(string name, IStorage dest, string newName, uint flags);
            void Commit(uint flags);
            void Revert();
            void EnumElements(uint r1, IntPtr r2, uint r3, out IntPtr e);
            void DestroyElement(string name);
            void RenameElement(string oldName, string newName);
            void SetElementTimes(string name, IntPtr c, IntPtr a, IntPtr m);
            void SetClass(ref Guid clsid);
            void SetStateBits(uint bits, uint mask);
            void Stat(out System.Runtime.InteropServices.ComTypes.STATSTG stat, uint flags);
        }

        [DllImport("ole32.dll")]
        private static extern int StgCreateDocfile([MarshalAs(UnmanagedType.LPWStr)] string name, uint mode, uint r, out IStorage stg);

        [DllImport("ole32.dll")]
        private static extern int StgOpenStorage([MarshalAs(UnmanagedType.LPWStr)] string name, IStorage priority, uint mode, IntPtr snb, uint r, out IStorage stg);

        private const uint Read = 0x0, ReadWrite = 0x2, ShareExclusive = 0x10, ShareDenyWrite = 0x20, Create = 0x1000;

        /// <summary>.msg okur.</summary>
        public static Mail Oku(string yol)
        {
            IStorage root;
            Check(StgOpenStorage(yol, null, Read | ShareDenyWrite, IntPtr.Zero, 0, out root));
            try
            {
                var m = new Mail { Konu = Metin(root, "0037") };
                m.Gonderen.Ad = Metin(root, "0C1A");
                m.Gonderen.Eposta = Ilk(Metin(root, "5D01"), Metin(root, "0C1F"), Metin(root, "0065"));
                var govde = Metin(root, "1000");
                if (govde.Length == 0)
                {
                    var html = Ikili(root, "__substg1.0_10130102");
                    govde = html == null ? string.Empty : Eml.HtmlMetin(Encoding.UTF8.GetString(html));
                }

                m.Govde = govde;
                var props = Ikili(root, "__properties_version1.0");
                long ft;
                if (props != null && (Prop(props, 32, 0x00390040, out ft) || Prop(props, 32, 0x0E060040, out ft)))
                {
                    m.Tarih = DateTime.FromFileTimeUtc(ft).ToLocalTime();
                }

                for (var i = 0; ; i++)
                {
                    IStorage r;
                    if (root.OpenStorage("__recip_version1.0_#" + i.ToString("X8", CultureInfo.InvariantCulture), null, Read | ShareExclusive, IntPtr.Zero, 0, out r) != 0)
                    {
                        break;
                    }

                    var k = new Kisi { Ad = Metin(r, "3001"), Eposta = Ilk(Metin(r, "39FE"), Metin(r, "3003")) };
                    var rp = Ikili(r, "__properties_version1.0");
                    long v;
                    if (rp != null && Prop(rp, 8, 0x39000003, out v))
                    {
                        k.Ekip = v == 1 || v == 5;
                    }

                    if (rp != null && Prop(rp, 8, 0x0C150003, out v))
                    {
                        k.Tur = (int)v;
                    }

                    m.Alicilar.Add(k);
                    Marshal.ReleaseComObject(r);
                }

                for (var i = 0; ; i++)
                {
                    IStorage a;
                    if (root.OpenStorage("__attach_version1.0_#" + i.ToString("X8", CultureInfo.InvariantCulture), null, Read | ShareExclusive, IntPtr.Zero, 0, out a) != 0)
                    {
                        break;
                    }

                    m.Ekler.Add(new Ek { Ad = Ilk(Metin(a, "3707"), Metin(a, "3704"), "ek" + (i + 1)), Veri = Ikili(a, "__substg1.0_37010102") ?? new byte[0] });
                    Marshal.ReleaseComObject(a);
                }

                return m;
            }
            finally
            {
                Marshal.ReleaseComObject(root);
            }
        }

        /// <summary>Deneme için en sade .msg yazar (konu, gönderen, tarih, gövde, alıcılar, ekler).</summary>
        public static void OrnekYaz(string yol, Mail m)
        {
            IStorage root;
            Check(StgCreateDocfile(yol, Create | ReadWrite | ShareExclusive, 0, out root));
            Yaz(root, "001A", "IPM.Note");
            Yaz(root, "0037", m.Konu);
            Yaz(root, "0C1A", m.Gonderen.Ad);
            Yaz(root, "0C1F", m.Gonderen.Eposta);
            Yaz(root, "1000", m.Govde);
            var props = new MemoryStream();
            props.Write(new byte[32], 0, 32);
            PropYaz(props, 0x00390040, (m.Tarih ?? DateTime.Now).ToFileTime());
            Akis(root, "__properties_version1.0", props.ToArray());
            for (var i = 0; i < m.Alicilar.Count; i++)
            {
                IStorage r;
                root.CreateStorage("__recip_version1.0_#" + i.ToString("X8", CultureInfo.InvariantCulture), Create | ReadWrite | ShareExclusive, 0, 0, out r);
                Yaz(r, "3001", m.Alicilar[i].Ad);
                Yaz(r, "39FE", m.Alicilar[i].Eposta);
                var rp = new MemoryStream();
                rp.Write(new byte[8], 0, 8);
                PropYaz(rp, 0x0C150003, m.Alicilar[i].Tur);
                PropYaz(rp, 0x39000003, m.Alicilar[i].Ekip ? 1 : 0);
                Akis(r, "__properties_version1.0", rp.ToArray());
                r.Commit(0);
                Marshal.ReleaseComObject(r);
            }

            for (var i = 0; i < m.Ekler.Count; i++)
            {
                IStorage a;
                root.CreateStorage("__attach_version1.0_#" + i.ToString("X8", CultureInfo.InvariantCulture), Create | ReadWrite | ShareExclusive, 0, 0, out a);
                Yaz(a, "3707", m.Ekler[i].Ad);
                Akis(a, "__substg1.0_37010102", m.Ekler[i].Veri);
                a.Commit(0);
                Marshal.ReleaseComObject(a);
            }

            root.Commit(0);
            Marshal.ReleaseComObject(root);
        }

        // Unicode metin (001F); yoksa ANSI metin (001E).
        private static string Metin(IStorage s, string etiket)
        {
            var u = Ikili(s, "__substg1.0_" + etiket + "001F");
            if (u != null)
            {
                return Encoding.Unicode.GetString(u).TrimEnd('\0');
            }

            var a = Ikili(s, "__substg1.0_" + etiket + "001E");
            return a == null ? string.Empty : Encoding.Default.GetString(a).TrimEnd('\0');
        }

        private static byte[] Ikili(IStorage s, string ad)
        {
            IStream st;
            if (s.OpenStream(ad, IntPtr.Zero, Read | ShareExclusive, 0, out st) != 0)
            {
                return null;
            }

            System.Runtime.InteropServices.ComTypes.STATSTG stat;
            st.Stat(out stat, 1);
            var b = new byte[stat.cbSize];
            st.Read(b, b.Length, IntPtr.Zero);
            Marshal.ReleaseComObject(st);
            return b;
        }

        private static bool Prop(byte[] props, int header, uint tag, out long value)
        {
            value = 0;
            for (var p = header; p + 16 <= props.Length; p += 16)
            {
                if (BitConverter.ToUInt32(props, p) == tag)
                {
                    value = BitConverter.ToInt64(props, p + 8);
                    return true;
                }
            }

            return false;
        }

        private static void Yaz(IStorage s, string etiket, string deger)
        {
            Akis(s, "__substg1.0_" + etiket + "001F", Encoding.Unicode.GetBytes(deger ?? string.Empty));
        }

        private static void Akis(IStorage s, string ad, byte[] veri)
        {
            IStream st;
            s.CreateStream(ad, Create | ReadWrite | ShareExclusive, 0, 0, out st);
            st.Write(veri, veri.Length, IntPtr.Zero);
            Marshal.ReleaseComObject(st);
        }

        private static void PropYaz(MemoryStream ms, uint tag, long value)
        {
            ms.Write(BitConverter.GetBytes(tag), 0, 4);
            ms.Write(BitConverter.GetBytes(6u), 0, 4);
            ms.Write(BitConverter.GetBytes(value), 0, 8);
        }

        private static void Check(int hr)
        {
            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }
    }

    // =====================================================================================================  .eml
    /// <summary>.eml: RFC 822 başlıklar + MIME gövde (multipart, base64, quoted-printable, RFC 2047 başlıklar).</summary>
    public static class Eml
    {
        /// <summary>.eml okur.</summary>
        public static Mail Oku(byte[] veri)
        {
            var parca = Parca.Coz(veri);
            var m = new Mail { Konu = Kodcoz(parca.Baslik("Subject")) };
            var gonderen = Adresler(parca.Baslik("From"));
            if (gonderen.Count > 0)
            {
                m.Gonderen = gonderen[0];
            }

            DateTime tarih;
            if (TarihOku(parca.Baslik("Date"), out tarih))
            {
                m.Tarih = tarih;
            }

            foreach (var k in Adresler(parca.Baslik("To")))
            {
                m.Alicilar.Add(k);
            }

            foreach (var k in Adresler(parca.Baslik("Cc")))
            {
                k.Tur = 2;
                m.Alicilar.Add(k);
            }

            var duz = new List<string>();
            var html = new List<string>();
            Topla(parca, m, duz, html);
            m.Govde = duz.Count > 0 ? string.Join("\n", duz) : string.Join("\n", html.Select(HtmlMetin));
            return m;
        }

        /// <summary>HTML'den düz metin (etiketler atılır, satır sonları korunur).</summary>
        public static string HtmlMetin(string html)
        {
            var t = Regex.Replace(html ?? string.Empty, @"<(style|script)[\s\S]*?</\1>", string.Empty, RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"<(br|/p|/div|/tr|/h\d)[^>]*>", "\n", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, "<[^>]+>", string.Empty);
            t = System.Net.WebUtility.HtmlDecode(t);
            return Regex.Replace(t, @"\n{3,}", "\n\n").Trim();
        }

        private static void Topla(Parca p, Mail m, List<string> duz, List<string> html)
        {
            if (p.Alt.Count > 0)
            {
                foreach (var a in p.Alt)
                {
                    Topla(a, m, duz, html);
                }

                return;
            }

            var tur = p.Tur.ToLowerInvariant();
            var ad = Kodcoz(p.Parametre("Content-Disposition", "filename"));
            if (ad.Length == 0)
            {
                ad = Kodcoz(p.Parametre("Content-Type", "name"));
            }

            var ekMi = p.Baslik("Content-Disposition").StartsWith("attachment", StringComparison.OrdinalIgnoreCase) || (ad.Length > 0 && !tur.StartsWith("text/", StringComparison.Ordinal));
            if (ekMi)
            {
                m.Ekler.Add(new Ek { Ad = ad.Length > 0 ? ad : "ek" + (m.Ekler.Count + 1), Veri = p.Icerik() });
            }
            else if (tur == "text/plain" || tur.Length == 0)
            {
                duz.Add(p.Metin());
            }
            else if (tur == "text/html")
            {
                html.Add(p.Metin());
            }
        }

        // RFC 2047: =?UTF-8?B?…?= ve =?ISO-8859-9?Q?…?=
        private static string Kodcoz(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            // Başlıkta doğrudan UTF-8 yazılmış ad (kodlanmamış) bayt bayt okunduğu için yeniden çözülür.
            if (s.Any(c => c > 127))
            {
                s = Encoding.UTF8.GetString(L1.GetBytes(s));
            }

            var t = Regex.Replace(s, @"\?=\s+=\?", "?==?");
            return Regex.Replace(t, @"=\?([^?]+)\?([bBqQ])\?([^?]*)\?=", mt =>
            {
                var enc = Kodlama(mt.Groups[1].Value);
                var metin = mt.Groups[3].Value;
                var b = mt.Groups[2].Value.ToUpperInvariant() == "B" ? Convert.FromBase64String(metin) : QpCoz(metin.Replace('_', ' '), true);
                return enc.GetString(b);
            }).Trim();
        }

        private static List<Kisi> Adresler(string baslik)
        {
            var sonuc = new List<Kisi>();
            foreach (var parca in Bol(Kodcoz(baslik)))
            {
                var p = parca.Trim();
                if (p.Length == 0)
                {
                    continue;
                }

                var mt = Regex.Match(p, @"^(?:""?(?<ad>[^""<]*)""?\s*)?<(?<ep>[^>]+)>$");
                var k = mt.Success ? new Kisi { Ad = mt.Groups["ad"].Value.Trim(), Eposta = mt.Groups["ep"].Value.Trim() } : new Kisi { Eposta = p };
                if (k.Ad.Length == 0)
                {
                    k.Ad = k.Eposta.Split('@')[0];
                }

                sonuc.Add(k);
            }

            return sonuc;
        }

        // Virgülle ayrılmış adres listesi (tırnak içindeki virgüller bölünmez).
        private static IEnumerable<string> Bol(string s)
        {
            var sb = new StringBuilder();
            var tirnak = false;
            foreach (var c in s)
            {
                if (c == '"')
                {
                    tirnak = !tirnak;
                }

                if (c == ',' && !tirnak)
                {
                    yield return sb.ToString();
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }

            yield return sb.ToString();
        }

        private static bool TarihOku(string s, out DateTime tarih)
        {
            tarih = DateTime.MinValue;
            var t = Regex.Replace(s ?? string.Empty, @"\s*\([^)]*\)\s*$", string.Empty).Trim();
            t = Regex.Replace(t, @"^[A-Za-z]{3},\s*", string.Empty);
            DateTimeOffset dto;
            var bicimler = new[] { "d MMM yyyy HH:mm:ss zzz", "d MMM yyyy HH:mm zzz", "d MMM yyyy HH:mm:ss 'GMT'", "d MMM yyyy HH:mm:ss 'UT'" };
            var duz = Regex.Replace(t, @"([+-]\d{2})(\d{2})$", "$1:$2");
            if (DateTimeOffset.TryParseExact(duz, bicimler, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out dto))
            {
                tarih = dto.LocalDateTime;
                return true;
            }

            return false;
        }

        private static Encoding Kodlama(string ad)
        {
            try
            {
                return Encoding.GetEncoding(string.IsNullOrEmpty(ad) ? "utf-8" : ad.Trim('"'));
            }
            catch (ArgumentException)
            {
                return Encoding.UTF8;
            }
        }

        private static byte[] QpCoz(string s, bool baslik)
        {
            var ms = new MemoryStream();
            var metin = baslik ? s : Regex.Replace(s, "=\r?\n", string.Empty);
            for (var i = 0; i < metin.Length; i++)
            {
                if (metin[i] == '=' && i + 2 < metin.Length && Uri.IsHexDigit(metin[i + 1]) && Uri.IsHexDigit(metin[i + 2]))
                {
                    ms.WriteByte(Convert.ToByte(metin.Substring(i + 1, 2), 16));
                    i += 2;
                }
                else
                {
                    var b = L1.GetBytes(metin[i].ToString());
                    ms.Write(b, 0, b.Length);
                }
            }

            return ms.ToArray();
        }

        /// <summary>MIME parçası: başlıklar, gövde ya da alt parçalar.</summary>
        private sealed class Parca
        {
            private readonly Dictionary<string, string> basliklar = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            private byte[] govde = new byte[0];

            private readonly List<Parca> alt = new List<Parca>();

            public List<Parca> Alt
            {
                get { return alt; }
            }

            public string Tur
            {
                get { return Baslik("Content-Type").Split(';')[0].Trim(); }
            }

            public static Parca Coz(byte[] veri)
            {
                var p = new Parca();
                var metin = L1.GetString(veri);
                var ayrim = Regex.Match(metin, "\r?\n\r?\n");
                var bas = ayrim.Success ? metin.Substring(0, ayrim.Index) : metin;
                var govdeMetin = ayrim.Success ? metin.Substring(ayrim.Index + ayrim.Length) : string.Empty;
                foreach (Match h in Regex.Matches(Regex.Replace(bas, "\r?\n[ \t]+", " "), @"^([^:\r\n]+):\s*(.*)$", RegexOptions.Multiline))
                {
                    var anahtar = h.Groups[1].Value.Trim();
                    if (!p.basliklar.ContainsKey(anahtar))
                    {
                        p.basliklar[anahtar] = h.Groups[2].Value.TrimEnd('\r');
                    }
                }

                p.govde = L1.GetBytes(govdeMetin);
                var sinir = p.Parametre("Content-Type", "boundary");
                if (p.Tur.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase) && sinir.Length > 0)
                {
                    var parcalar = govdeMetin.Split(new[] { "--" + sinir }, StringSplitOptions.None);
                    for (var i = 1; i < parcalar.Length; i++)
                    {
                        var parca = parcalar[i];
                        if (parca.StartsWith("--", StringComparison.Ordinal))
                        {
                            break;
                        }

                        p.Alt.Add(Coz(L1.GetBytes(parca.TrimStart('\r', '\n'))));
                    }
                }

                return p;
            }

            public string Baslik(string ad)
            {
                string v;
                return basliklar.TryGetValue(ad, out v) ? v : string.Empty;
            }

            public string Parametre(string baslik, string ad)
            {
                var mt = Regex.Match(Baslik(baslik), ad + @"\*?=\s*(""(?<v>[^""]*)""|(?<v>[^;\s]+))", RegexOptions.IgnoreCase);
                return mt.Success ? mt.Groups["v"].Value : string.Empty;
            }

            public byte[] Icerik()
            {
                var kod = Baslik("Content-Transfer-Encoding").Trim().ToLowerInvariant();
                var ham = L1.GetString(govde);
                if (kod == "base64")
                {
                    try
                    {
                        return Convert.FromBase64String(Regex.Replace(ham, @"[^A-Za-z0-9+/=]", string.Empty));
                    }
                    catch (FormatException)
                    {
                        return govde;
                    }
                }

                return kod == "quoted-printable" ? QpCoz(ham, false) : govde;
            }

            public string Metin()
            {
                return Kodlama(Parametre("Content-Type", "charset")).GetString(Icerik()).Trim();
            }
        }
    }
}
