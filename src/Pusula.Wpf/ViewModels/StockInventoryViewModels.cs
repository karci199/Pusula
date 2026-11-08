using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Pusula.Core.Inventory;
using Pusula.Core.Types;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Stok sayfasında bir envanter türünün bölümü (örn. Jigler): kullanılan kayıtlar ve kısa özet.</summary>
    public sealed class StockInventoryGroup
    {
        /// <summary>Bölümü kurar; <paramref name="rows"/> bu türün satırlarıdır.</summary>
        public StockInventoryGroup(TypeDefinition type, IEnumerable<InventoryRow> rows)
        {
            Type = type;
            foreach (var row in rows)
            {
                Rows.Add(row);
            }

            var real = Rows.Where(r => !r.IsMissing).ToList();
            var side = real.Count(r => type.SideStatuses.Any(s => string.Equals(s.Name, r.Status.Name, StringComparison.OrdinalIgnoreCase)));
            var parts = new List<string> { type.Name + " " + Rows.Count.ToString(System.Globalization.CultureInfo.CurrentCulture) };
            if (side > 0)
            {
                parts.Add(string.Format(Resources.StockInv_Attention, side));
            }

            var missing = Rows.Count - real.Count;
            if (missing > 0)
            {
                parts.Add(string.Format(Resources.StockInv_Missing, missing));
            }

            Summary = string.Join(" · ", parts);
            HasWarning = side > 0 || missing > 0;
            Header = type.PluralName + " (" + Rows.Count.ToString(System.Globalization.CultureInfo.CurrentCulture) + ")";
        }

        /// <summary>Tür.</summary>
        public TypeDefinition Type { get; }

        /// <summary>"Jigler (2)".</summary>
        public string Header { get; }

        /// <summary>"Jig 2 · 1 dikkat".</summary>
        public string Summary { get; }

        /// <summary>Sıra dışı durumda ya da bulunamayan kayıt var mı.</summary>
        public bool HasWarning { get; }

        /// <summary>Türün simgesi.</summary>
        public string Glyph
        {
            get { return TreeNodeViewModel.GlyphOf(Type.Icon); }
        }

        /// <summary>Türün rengi.</summary>
        public string Color
        {
            get { return Type.Color; }
        }

        /// <summary>Satırlar.</summary>
        public ObservableCollection<InventoryRow> Rows { get; } = new ObservableCollection<InventoryRow>();

        /// <summary>Hiç kayıt yok mu.</summary>
        public bool IsEmpty
        {
            get { return Rows.Count == 0; }
        }
    }
}
