using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pusula.Core.Testing;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Testler / Kalifikasyon sekmesinde bir test planı kartı.</summary>
    public sealed class TestPlanRow
    {
        /// <summary>Planı ve ona ait koşuları (test notundaki <c>plan:</c> adıyla) özetler.</summary>
        public TestPlanRow(TestPlan plan, IEnumerable<TestRun> runs, string kindName)
        {
            Path = plan.Path;
            Folder = System.IO.Path.GetDirectoryName(plan.Path) ?? string.Empty;
            Name = plan.Name;
            Description = plan.Description;
            IsQualification = plan.IsQualification;
            var parts = new List<string>();
            if (plan.IsKiwi)
            {
                parts.Add(Resources.Plan_Kiwi);
                parts.Add(string.Format(Resources.Plan_Steps, plan.Steps));
                parts.Add(string.Format(Resources.Plan_Limits, plan.Limits));
                parts.Add(string.Format(Resources.Plan_Revision, plan.Revision));
            }
            else
            {
                parts.Add(kindName);
            }

            if (plan.Modified > DateTime.MinValue)
            {
                parts.Add(plan.Modified.ToString(Resources.Format_Date) + (plan.ModifiedBy.Length > 0 ? " " + plan.ModifiedBy : string.Empty));
            }

            Meta = string.Join(" · ", parts.Where(p => p.Length > 0));
            var count = runs.Count(plan.Matches);
            RunCount = count;
            RunsText = count > 0 ? string.Format(Resources.Plan_Runs, count) : Resources.Plan_NoRuns;
        }

        /// <summary>Dosyanın tam yolu.</summary>
        public string Path { get; }

        /// <summary>Dosyanın klasörü.</summary>
        public string Folder { get; }

        /// <summary>Plan adı.</summary>
        public string Name { get; }

        /// <summary>Açıklama (KIWI); boş olabilir.</summary>
        public string Description { get; }

        /// <summary>Açıklama var mı.</summary>
        public bool HasDescription
        {
            get { return Description.Length > 0; }
        }

        /// <summary>Kalifikasyon planı mı.</summary>
        public bool IsQualification { get; }

        /// <summary>Tür, adım, limit, revizyon, tarih ("KIWI · 3 adım · 3 limit · Rev 0 · 06.11.2026 M. Yılmaz").</summary>
        public string Meta { get; }

        /// <summary>Bu planla yapılan koşu sayısı.</summary>
        public int RunCount { get; }

        /// <summary>"bu planla 3 koşu" / "henüz koşu yok".</summary>
        public string RunsText { get; }
    }
}