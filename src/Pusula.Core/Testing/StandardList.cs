using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pusula.Core.Notes;

namespace Pusula.Core.Testing
{
    /// <summary>Bir test standardı ve yöntemleri ("MIL-STD-810G": "509.5 · Tuz sisi" …).</summary>
    public sealed class TestStandard
    {
        /// <summary>Standart adı, örn. "MIL-STD-810G".</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Yöntemler: "509.5 · Tuz sisi" biçiminde (numara · ad).</summary>
        public List<string> Methods { get; } = new List<string>();
    }

    /// <summary>
    /// Test standartları listesi (ÇK / EMC): tüm projelerde ortak, kökte <c>_TAKIP\Standartlar.md</c>. Biçim:
    /// <c>## MIL-STD-810G</c> başlığı altında <c>- 509.5 · Tuz sisi</c> satırları; Obsidian'dan elle de düzenlenir.
    /// Dosya yoksa başlangıç listesi (<see cref="Defaults"/>) kullanılır; dosya ilk eklemede (başlangıç listesiyle) oluşur.
    /// Ağdan bir şey çekilmez; numaralar kullanıcının elindeki standart nüshasıyla doğrulanmalıdır.
    /// </summary>
    public static class StandardList
    {
        /// <summary>Listenin kökteki yolu.</summary>
        public static string PathFor(string root)
        {
            return Path.Combine(root, "_TAKIP", "Standartlar.md");
        }

        // MIL-STD-810 yöntem adları: 500'den 528'e (numaranın tam kısmına göre).
        private static readonly string[] EnvironmentNames =
        {
            "Düşük basınç (irtifa)", "Yüksek sıcaklık", "Düşük sıcaklık", "Sıcaklık şoku", "Sıvı bulaşması", "Güneş ışınımı", "Yağmur", "Nem", "Küf",
            "Tuz sisi", "Kum ve toz", "Patlayıcı ortam", "Suya daldırma", "İvme", "Titreşim", "Akustik gürültü", "Şok", "Piroteknik şok",
            "Asidik ortam", "Silah atış şoku", "Sıcaklık, nem, titreşim, irtifa", "Buzlanma / donan yağmur", "Balistik şok",
            "Titreşim-akustik / sıcaklık", "Donma / çözülme", "Zaman dalga biçimi yeniden üretimi", "Ray darbesi", "Çok uyarıcılı titreşim",
            "Gemi ekipmanı mekanik titreşimi",
        };

        // "500.4 501.4 …" numaralarından 810 sürümü: her numaraya adı eklenir ("509.4 · Tuz sisi").
        private static TestStandard Environment(string name, string numbers)
        {
            var std = new TestStandard { Name = name };
            foreach (var number in numbers.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var index = int.Parse(number.Split('.')[0], System.Globalization.CultureInfo.InvariantCulture) - 500;
                std.Methods.Add(index >= 0 && index < EnvironmentNames.Length ? number + " · " + EnvironmentNames[index] : number);
            }

            return std;
        }

        /// <summary>Başlangıç listesi: MIL-STD-810F, 810G, 810G CHG1, 810H ve MIL-STD-461G'nin yöntemleri.</summary>
        public static IList<TestStandard> Defaults()
        {
            var f = Environment("MIL-STD-810F", "500.4 501.4 502.4 503.4 504 505.4 506.4 507.4 508.5 509.4 510.4 511.4 512.4 513.5 514.5 515.5 516.5 517 518 519.5 520.2 521.2 522");
            var g = Environment("MIL-STD-810G", "500.5 501.5 502.5 503.5 504.1 505.5 506.5 507.5 508.6 509.5 510.5 511.5 512.5 513.6 514.6 515.6 516.6 517.1 518.1 519.6 520.3 521.3 522.1 523.3 524 525 526 527 528");
            var g1 = Environment("MIL-STD-810G CHG1", "500.6 501.6 502.6 503.6 504.2 505.6 506.6 507.6 508.7 509.6 510.6 511.6 512.6 513.7 514.7 515.7 516.7 517.2 518.2 519.7 520.4 521.4 522.2 523.4 524.1 525.1 526.1 527.1 528.1");
            var h = Environment("MIL-STD-810H", "500.6 501.7 502.7 503.7 504.3 505.7 506.6 507.6 508.8 509.7 510.7 511.7 512.6 513.8 514.8 515.8 516.8 517.3 518.2 519.8 520.5 521.4 522.2 523.4 524.1 525.2 526.2 527.2 528.1");
            var emc = new TestStandard { Name = "MIL-STD-461G" };
            emc.Methods.AddRange(new[]
            {
                "CE101 · İletim emisyonu, güç hatları, 30 Hz – 10 kHz", "CE102 · İletim emisyonu, güç hatları, 10 kHz – 10 MHz",
                "CE106 · İletim emisyonu, anten ucu", "CS101 · İletim duyarlılığı, güç hatları, 30 Hz – 150 kHz",
                "CS103 · İletim duyarlılığı, anten girişi, ara modülasyon", "CS104 · İletim duyarlılığı, anten girişi, istenmeyen sinyal",
                "CS105 · İletim duyarlılığı, anten girişi, çapraz modülasyon", "CS106 · İletim duyarlılığı, geçici, güç hatları",
                "CS109 · İletim duyarlılığı, yapısal akım", "CS114 · İletim duyarlılığı, kablo demeti akım enjeksiyonu",
                "CS115 · İletim duyarlılığı, kablo demeti, darbe", "CS116 · İletim duyarlılığı, sönümlü sinüs geçici",
                "CS117 · Yıldırım dolaylı etkileri", "CS118 · Personel elektrostatik boşalma",
                "RE101 · Işıma emisyonu, manyetik alan", "RE102 · Işıma emisyonu, elektrik alan", "RE103 · Işıma emisyonu, anten sahte ve harmonik",
                "RS101 · Işıma duyarlılığı, manyetik alan", "RS103 · Işıma duyarlılığı, elektrik alan", "RS105 · Işıma duyarlılığı, geçici elektromanyetik alan",
            });
            return new List<TestStandard> { f, g, g1, h, emc };
        }

        /// <summary>Standartlar (dosyadaki sırayla); dosya yoksa başlangıç listesi.</summary>
        public static IList<TestStandard> Read(string root)
        {
            var path = PathFor(root);
            if (string.IsNullOrEmpty(root) || !File.Exists(path))
            {
                return Defaults();
            }

            try
            {
                return Parse(File.ReadAllLines(path));
            }
            catch (IOException)
            {
                return Defaults();
            }
            catch (UnauthorizedAccessException)
            {
                return Defaults();
            }
        }

        /// <summary>md satırlarından standartlar: "## ad" başlıkları ve altlarındaki "- " satırları.</summary>
        public static IList<TestStandard> Parse(IEnumerable<string> lines)
        {
            var list = new List<TestStandard>();
            TestStandard? current = null;
            foreach (var raw in lines ?? Enumerable.Empty<string>())
            {
                var line = raw.Trim();
                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    var name = line.Substring(3).Trim();
                    current = list.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
                    if (current == null && name.Length > 0)
                    {
                        current = new TestStandard { Name = name };
                        list.Add(current);
                    }
                }
                else if (current != null && (line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal)))
                {
                    var method = line.Substring(2).Trim();
                    if (method.Length > 0 && !current.Methods.Contains(method, StringComparer.OrdinalIgnoreCase))
                    {
                        current.Methods.Add(method);
                    }
                }
            }

            return list;
        }

        /// <summary>Yöntem metninin numarası: "509.5 · Tuz sisi" → "509.5"; ayraç yoksa ilk sözcük.</summary>
        public static string NumberOf(string method)
        {
            var text = (method ?? string.Empty).Trim();
            var dot = text.IndexOf('·');
            if (dot >= 0)
            {
                return text.Substring(0, dot).Trim();
            }

            var space = text.IndexOf(' ');
            return space < 0 ? text : text.Substring(0, space);
        }

        /// <summary>Kısa gösterim: "MIL-STD-810G" + "509.5" → "810G 509.5" (önek atılır).</summary>
        public static string ShortText(string standard, string method)
        {
            var name = (standard ?? string.Empty).Trim();
            if (name.StartsWith("MIL-STD-", StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring("MIL-STD-".Length);
            }

            return string.Join(" ", new[] { name, NumberOf(method) }.Where(s => s.Length > 0));
        }

        /// <summary>
        /// Standart ya da yöntem listede yoksa ekler (dosya yoksa <paramref name="newFileHeader"/> ve başlangıç listesiyle oluşturulur). Var olan satırlara
        /// dokunulmaz; yeni yöntem kendi başlığının son maddesinden sonra, yeni standart dosyanın sonuna eklenir.
        /// Hata metni ya da boş döndürür.
        /// </summary>
        public static string Add(string root, string standard, string method, string newFileHeader)
        {
            var name = (standard ?? string.Empty).Trim();
            var item = (method ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(root) || name.Length == 0)
            {
                return string.Empty;
            }

            var path = PathFor(root);
            try
            {
                var exists = File.Exists(path);
                var known = Read(root);
                var std = known.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
                if (std != null && (item.Length == 0 || std.Methods.Contains(item, StringComparer.OrdinalIgnoreCase)))
                {
                    return string.Empty;
                }

                List<string> lines;
                string newline;
                if (exists)
                {
                    var text = File.ReadAllText(path);
                    newline = text.Contains("\r\n") ? "\r\n" : "\n";
                    lines = text.Replace("\r\n", "\n").Split('\n').ToList();
                }
                else
                {
                    newline = "\r\n";
                    lines = (newFileHeader ?? string.Empty).Replace("\r\n", "\n").Split('\n').ToList();
                    lines.Add(string.Empty);
                    foreach (var s in Defaults())
                    {
                        lines.Add("## " + s.Name);
                        lines.AddRange(s.Methods.Select(m => "- " + m));
                        lines.Add(string.Empty);
                    }
                }

                var head = lines.FindIndex(l => l.Trim().StartsWith("## ", StringComparison.Ordinal) && string.Equals(l.Trim().Substring(3).Trim(), name, StringComparison.OrdinalIgnoreCase));
                if (head < 0)
                {
                    while (lines.Count > 0 && lines[lines.Count - 1].Trim().Length == 0)
                    {
                        lines.RemoveAt(lines.Count - 1);
                    }

                    lines.Add(string.Empty);
                    lines.Add("## " + name);
                    if (item.Length > 0)
                    {
                        lines.Add("- " + item);
                    }

                    lines.Add(string.Empty);
                }
                else if (item.Length > 0 && !lines.Skip(head + 1).TakeWhile(l => !l.Trim().StartsWith("#", StringComparison.Ordinal))
                    .Any(l => string.Equals(l.Trim().TrimStart('-', '*').Trim(), item, StringComparison.OrdinalIgnoreCase)))
                {
                    var at = head + 1;
                    for (var i = head + 1; i < lines.Count && !lines[i].Trim().StartsWith("#", StringComparison.Ordinal); i++)
                    {
                        if (lines[i].Trim().StartsWith("- ", StringComparison.Ordinal) || lines[i].Trim().StartsWith("* ", StringComparison.Ordinal))
                        {
                            at = i + 1;
                        }
                    }

                    lines.Insert(at, "- " + item);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? root);
                return VerificationList.WriteAtomic(path, string.Join(newline, lines), !exists || Journal.HasBom(path));
            }
            catch (IOException ex)
            {
                return ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                return ex.Message;
            }
        }
    }
}
