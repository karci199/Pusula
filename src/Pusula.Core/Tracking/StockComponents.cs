using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pusula.Core.Tracking
{
    /// <summary>
    /// Alt / üst stok (25.11.2026): üst stok kendi bileşenlerini <c>stok.json</c> → <c>components</c> içinde tutar; alt stokun
    /// kullanıldığı yerler bütün stokların listelerinden hesaplanır. Bir bileşen birden çok üst stokta olabilir; döngü kurulamaz.
    /// </summary>
    public static class StockComponents
    {
        /// <summary>Üst stokun bileşen listesine numarayı ekler ya da çıkarır (atomik yazım). Başarılıysa <c>true</c>.</summary>
        public static bool Set(string stockFolder, string number, bool add)
        {
            if (string.IsNullOrWhiteSpace(number) || !File.Exists(TrackingFiles.StockFile(stockFolder)))
            {
                return false;
            }

            return JsonFile.Update<StockInfo>(TrackingFiles.StockFile(stockFolder), info =>
            {
                info.Components.RemoveAll(n => string.Equals(n, number, StringComparison.OrdinalIgnoreCase));
                if (add)
                {
                    info.Components.Add(number.Trim());
                    info.Components.Sort(StringComparer.OrdinalIgnoreCase);
                }

                return true;
            }).Length == 0;
        }

        /// <summary>
        /// <paramref name="number"/> stokunu içeren bütün üst stoklar (doğrudan ve dolaylı). <paramref name="components"/>:
        /// stok numarası → bileşenleri.
        /// </summary>
        public static HashSet<string> AncestorsOf(string number, IDictionary<string, List<string>> components)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pending = new Queue<string>();
            pending.Enqueue(number);
            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                foreach (var pair in components)
                {
                    if (pair.Value.Contains(current, StringComparer.OrdinalIgnoreCase) && result.Add(pair.Key))
                    {
                        pending.Enqueue(pair.Key);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// <paramref name="parent"/> stokuna <paramref name="child"/> bileşen olarak eklenebilir mi: kendisi değil, zaten
        /// bileşeni değil ve <paramref name="parent"/>'ın üstlerinden biri değil (döngü olmaz).
        /// </summary>
        public static bool CanAdd(string parent, string child, IDictionary<string, List<string>> components)
        {
            if (string.Equals(parent, child, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            List<string> own;
            if (components.TryGetValue(parent, out own) && own.Contains(child, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            return !AncestorsOf(parent, components).Contains(child);
        }
    }
}
