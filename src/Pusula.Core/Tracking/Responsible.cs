using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Pusula.Core.Tracking
{
    /// <summary>
    /// Bir kaydın sorumlusu (25.11.2026, isteğe bağlı): rol ("Tasarım sorumlusu") ve kişi ("Okan Özdemir"). Stokta
    /// <c>stok.json</c> → <c>responsibles</c>, yardımcı araçta <c>kayit.json</c> → <c>responsibles</c>; her kayıt yalnız
    /// kendi sorumlularını gösterir (stokta aracın sorumlusu görünmez).
    /// </summary>
    [DataContract]
    public sealed class Responsible
    {
        /// <summary>Serileştirici kurucuyu çağırmaz; dosyada olmayan alanlar boş metinle başlar.</summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            Role = string.Empty;
            Person = string.Empty;
        }

        /// <summary>Rol (boş olabilir: yalnız "Sorumlu").</summary>
        [DataMember(Name = "role", Order = 0)]
        public string Role { get; set; } = string.Empty;

        /// <summary>Kişinin adı soyadı.</summary>
        [DataMember(Name = "person", Order = 1)]
        public string Person { get; set; } = string.Empty;

        /// <summary>Kişisi boş satırları atar, boşlukları kırpar, aynı rol + kişi tekrarını atar.</summary>
        public static List<Responsible> Clean(IEnumerable<Responsible>? items)
        {
            var result = new List<Responsible>();
            foreach (var item in items ?? Enumerable.Empty<Responsible>())
            {
                var person = (item?.Person ?? string.Empty).Trim();
                var role = (item?.Role ?? string.Empty).Trim();
                if (person.Length > 0 && !result.Any(r => string.Equals(r.Person, person, StringComparison.CurrentCultureIgnoreCase)
                    && string.Equals(r.Role, role, StringComparison.CurrentCultureIgnoreCase)))
                {
                    result.Add(new Responsible { Role = role, Person = person });
                }
            }

            return result;
        }
    }
}
