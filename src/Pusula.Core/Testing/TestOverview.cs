using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Pusula.Core.Testing
{
    /// <summary>Genel sekmesindeki bir dikkat maddesi.</summary>
    public sealed class TestAlert
    {
        /// <summary>Dikkat türü.</summary>
        public TestAlertKind Kind { get; set; }

        /// <summary>İlgili koşu (açık / kalan test) ya da boş.</summary>
        public TestRun? Run { get; set; }

        /// <summary>Jig numarası (revizyon uyarısında).</summary>
        public string Jig { get; set; } = string.Empty;

        /// <summary>Jig'in görülen revizyonları, tarih sırasıyla (revizyon uyarısında).</summary>
        public List<string> Revisions { get; set; } = new List<string>();

        /// <summary>Sonucu girilmemiş koşu sayısı.</summary>
        public int Count { get; set; }
    }

    /// <summary>Dikkat türleri.</summary>
    public enum TestAlertKind
    {
        /// <summary>Bir SN'in son turu devam ediyor.</summary>
        Open,

        /// <summary>Bir SN'in son turu kaldı.</summary>
        Failed,

        /// <summary>Aynı jig farklı revizyonlarla kullanılmış.</summary>
        JigRevision,

        /// <summary>Sonucu girilmemiş koşular var.</summary>
        Unknown,
    }

    /// <summary>
    /// Stokun koşularından Genel sekmesi için özet: dikkat maddeleri, "her SN'in son turu" tablosu, zaman çizelgesi.
    /// </summary>
    public sealed class TestOverview
    {
        private static readonly Regex JigWithRevision = new Regex(@"^(\S+)\s+Rev\s+(\S+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>Dikkat maddeleri.</summary>
        public List<TestAlert> Alerts { get; } = new List<TestAlert>();

        /// <summary>Tablonun sütunları: (sistem, kalifikasyon mu); önce geliştirme, sonra kalifikasyon.</summary>
        public List<Tuple<string, bool>> Columns { get; } = new List<Tuple<string, bool>>();

        /// <summary>Tablonun satırları: seri numaraları (SN01, SN02 …, sonra SNP1 …).</summary>
        public List<string> Serials { get; } = new List<string>();

        /// <summary>Her (sistem, kalifikasyon, SN) için son tur; anahtar <see cref="Key"/>.</summary>
        public Dictionary<string, TestRun> Latest { get; } = new Dictionary<string, TestRun>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Bütün koşular, en yeni üstte.</summary>
        public List<TestRun> Timeline { get; } = new List<TestRun>();

        /// <summary>Tablo hücresinin anahtarı.</summary>
        public static string Key(string system, bool qualification, string serial)
        {
            return system + "|" + (qualification ? "Q" : "D") + "|" + serial;
        }

        /// <summary>Koşulardan özeti hesaplar.</summary>
        public static TestOverview Build(IEnumerable<TestRun> runs)
        {
            var overview = new TestOverview();
            var list = runs.OrderByDescending(r => r.Date).ToList();
            overview.Timeline.AddRange(list);

            foreach (var group in list.GroupBy(r => Key(r.System, r.IsQualification, r.SerialNo), StringComparer.OrdinalIgnoreCase))
            {
                var last = group.OrderByDescending(r => r.Round).ThenByDescending(r => r.Date).First();
                overview.Latest[group.Key] = last;
            }

            overview.Columns.AddRange(list
                .Select(r => Tuple.Create(r.System, r.IsQualification))
                .Distinct()
                .OrderBy(c => c.Item2)
                .ThenBy(c => c.Item1, StringComparer.OrdinalIgnoreCase));

            overview.Serials.AddRange(list
                .Select(r => r.SerialNo)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s.StartsWith("SNP", StringComparison.OrdinalIgnoreCase))
                .ThenBy(s => s, StringComparer.OrdinalIgnoreCase));

            // Dikkat: son turu devam eden ya da kalan SN'ler.
            foreach (var last in overview.Latest.Values.OrderBy(r => r.IsQualification).ThenBy(r => r.System).ThenBy(r => r.SerialNo))
            {
                if (last.Result == TestResult.InProgress)
                {
                    overview.Alerts.Add(new TestAlert { Kind = TestAlertKind.Open, Run = last });
                }
                else if (last.Result == TestResult.Failed)
                {
                    overview.Alerts.Add(new TestAlert { Kind = TestAlertKind.Failed, Run = last });
                }
            }

            // Dikkat: aynı jig farklı revizyonlarla ("JIG-1000-0009 Rev AB" / "Rev AC").
            var jigUses = list
                .Select(r => new { r.Date, Match = JigWithRevision.Match(r.Jig ?? string.Empty) })
                .Where(x => x.Match.Success)
                .Select(x => new { Jig = x.Match.Groups[1].Value, Rev = x.Match.Groups[2].Value, x.Date });
            foreach (var jig in jigUses.GroupBy(u => u.Jig, StringComparer.OrdinalIgnoreCase))
            {
                var revisions = jig.OrderBy(u => u.Date).Select(u => u.Rev).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (revisions.Count > 1)
                {
                    overview.Alerts.Add(new TestAlert { Kind = TestAlertKind.JigRevision, Jig = jig.Key, Revisions = revisions });
                }
            }

            var unknown = list.Count(r => r.Result == TestResult.Unknown);
            if (unknown > 0)
            {
                overview.Alerts.Add(new TestAlert { Kind = TestAlertKind.Unknown, Count = unknown });
            }

            return overview;
        }
    }
}
