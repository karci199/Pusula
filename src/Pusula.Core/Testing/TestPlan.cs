using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Pusula.Core.Scanning;

namespace Pusula.Core.Testing
{
    /// <summary>
    /// Stokun test planı: <c>02_Test_Planlari</c> (ve kalifikasyondaki <c>Test_Plani</c>) klasöründeki bir dosya.
    /// KIWI planlarının (<c>*.atd.json</c>) adı, açıklaması, adım/limit sayısı ve revizyonu okunur; diğer dosyalar
    /// (docx, xlsx …) adıyla listelenir. Uygulama yalnızca okur.
    /// </summary>
    public sealed class TestPlan
    {
        /// <summary>KIWI plan dosyasının sonu.</summary>
        public const string KiwiExtension = ".atd.json";

        /// <summary>Dosyanın tam yolu.</summary>
        public string Path { get; private set; } = string.Empty;

        /// <summary>Plan adı (KIWI'de plan adı, değilse dosya adı).</summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>Açıklama (yalnız KIWI); boş olabilir.</summary>
        public string Description { get; private set; } = string.Empty;

        /// <summary>KIWI planı mı.</summary>
        public bool IsKiwi { get; private set; }

        /// <summary>Adım sayısı (KIWI); bilinmiyorsa 0.</summary>
        public int Steps { get; private set; }

        /// <summary>Limit sayısı (KIWI); bilinmiyorsa 0.</summary>
        public int Limits { get; private set; }

        /// <summary>Revizyon (KIWI); bilinmiyorsa 0.</summary>
        public int Revision { get; private set; }

        /// <summary>Son değişiklik zamanı (KIWI kaydı, yoksa dosyanın).</summary>
        public DateTime Modified { get; private set; }

        /// <summary>Son değiştiren (KIWI); boş olabilir.</summary>
        public string ModifiedBy { get; private set; } = string.Empty;

        /// <summary>Kalifikasyon planı mı (<c>04_Kalifikasyon</c> altında).</summary>
        public bool IsQualification { get; private set; }

        /// <summary>
        /// Stoktaki planlar: adı (numara ön eki atılınca) <c>Test Planlari</c> ya da <c>Test Plani</c> olan klasörlerdeki
        /// dosyalar (alt klasörler dahil; önemsiz dosyalar ve arşivler hariç). Ada göre sıralı.
        /// </summary>
        public static List<TestPlan> ReadStock(string stockFolder)
        {
            var result = new List<TestPlan>();
            foreach (var folder in PlanFolders(stockFolder, 0))
            {
                var qualification = folder.IndexOf("Kalifikasyon", StringComparison.OrdinalIgnoreCase) >= 0;
                foreach (var file in Files(folder))
                {
                    var plan = Read(file);
                    plan.IsQualification = qualification;
                    result.Add(plan);
                }
            }

            return result.OrderBy(p => p.IsQualification).ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        /// <summary>Tek dosyayı okur; KIWI planı okunamazsa dosya adıyla döner.</summary>
        public static TestPlan Read(string path)
        {
            var plan = new TestPlan
            {
                Path = path,
                Name = DisplayName(path),
                Modified = SafeWriteTime(path),
            };

            if (!path.EndsWith(KiwiExtension, StringComparison.OrdinalIgnoreCase))
            {
                return plan;
            }

            try
            {
                // KIWI dosyaları BOM'lu UTF-8; JSON okuyucusu BOM'u tanımadığı için metin BOM'suz yeniden kodlanır.
                var text = Fs.ReadAllText(path);
                using (var stream = new MemoryStream(new System.Text.UTF8Encoding(false).GetBytes(text)))
                {
                    var kiwi = (KiwiPlan?)new DataContractJsonSerializer(typeof(KiwiPlan)).ReadObject(stream);
                    if (kiwi == null)
                    {
                        return plan;
                    }

                    plan.IsKiwi = true;
                    plan.Name = string.IsNullOrWhiteSpace(kiwi.Name) ? plan.Name : kiwi.Name!.Trim();
                    plan.Description = (kiwi.Description ?? string.Empty).Trim();
                    plan.Steps = kiwi.Header?.Counts?.Steps ?? 0;
                    plan.Limits = kiwi.Header?.Counts?.Limits ?? 0;
                    plan.Revision = kiwi.Header?.Revision ?? 0;
                    plan.ModifiedBy = kiwi.Header?.Modified?.Person ?? string.Empty;
                    DateTimeOffset at;
                    if (DateTimeOffset.TryParse(kiwi.Header?.Modified?.At ?? string.Empty, CultureInfo.InvariantCulture, DateTimeStyles.None, out at))
                    {
                        plan.Modified = at.LocalDateTime;
                    }
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (SerializationException)
            {
            }

            return plan;
        }

        /// <summary>Koşunun bu plana ait olup olmadığı (test notundaki <c>plan:</c> adı plan adıyla aynıysa).</summary>
        public bool Matches(TestRun run)
        {
            return run.Plan.Trim().Length > 0 && string.Equals(run.Plan.Trim(), Name, StringComparison.CurrentCultureIgnoreCase);
        }

        private static string DisplayName(string path)
        {
            var name = System.IO.Path.GetFileName(path);
            return name.EndsWith(KiwiExtension, StringComparison.OrdinalIgnoreCase)
                ? name.Substring(0, name.Length - KiwiExtension.Length)
                : System.IO.Path.GetFileNameWithoutExtension(path);
        }

        private static DateTime SafeWriteTime(string path)
        {
            try
            {
                return Fs.GetLastWriteTime(path);
            }
            catch (IOException)
            {
                return DateTime.MinValue;
            }
            catch (UnauthorizedAccessException)
            {
                return DateTime.MinValue;
            }
        }

        private static IEnumerable<string> PlanFolders(string folder, int depth)
        {
            string[] children;
            try
            {
                children = Fs.GetDirectories(folder);
            }
            catch (IOException)
            {
                yield break;
            }
            catch (UnauthorizedAccessException)
            {
                yield break;
            }

            foreach (var child in children.OrderBy(c => c, StringComparer.OrdinalIgnoreCase))
            {
                if (FileRules.IsSkippedFolder(child))
                {
                    continue;
                }

                var label = Inventory.RecordContents.LabelOf(System.IO.Path.GetFileName(child)).Replace('ı', 'i').Replace('İ', 'I');
                if (label.StartsWith("Test Plan", StringComparison.OrdinalIgnoreCase))
                {
                    yield return child;
                }
                else if (depth < 1)
                {
                    // Bir alt düzey de aranır (04_Kalifikasyon\02_Test_Plani).
                    foreach (var sub in PlanFolders(child, depth + 1))
                    {
                        yield return sub;
                    }
                }
            }
        }

        private static IEnumerable<string> Files(string folder)
        {
            string[] files;
            try
            {
                files = Fs.GetFiles(folder, "*", SearchOption.AllDirectories);
            }
            catch (IOException)
            {
                yield break;
            }
            catch (UnauthorizedAccessException)
            {
                yield break;
            }

            foreach (var file in files)
            {
                var relative = file.Substring(folder.Length).TrimStart('\\');
                if (FileRules.IsJunkFile(file) || relative.Split('\\').Take(relative.Split('\\').Length - 1).Any(FileRules.IsSkippedFolder))
                {
                    continue;
                }

                yield return file;
            }
        }

        [DataContract]
        private sealed class KiwiPlan
        {
            [DataMember(Name = "Name")]
            public string? Name { get; set; }

            [DataMember(Name = "Description")]
            public string? Description { get; set; }

            [DataMember(Name = "Header")]
            public KiwiHeader? Header { get; set; }
        }

        [DataContract]
        private sealed class KiwiHeader
        {
            [DataMember(Name = "Revision")]
            public int Revision { get; set; }

            [DataMember(Name = "Modified")]
            public KiwiStamp? Modified { get; set; }

            [DataMember(Name = "Counts")]
            public KiwiCounts? Counts { get; set; }
        }

        [DataContract]
        private sealed class KiwiStamp
        {
            [DataMember(Name = "At")]
            public string? At { get; set; }

            [DataMember(Name = "Person")]
            public string? Person { get; set; }
        }

        [DataContract]
        private sealed class KiwiCounts
        {
            [DataMember(Name = "Steps")]
            public int Steps { get; set; }

            [DataMember(Name = "Limits")]
            public int Limits { get; set; }
        }
    }
}
