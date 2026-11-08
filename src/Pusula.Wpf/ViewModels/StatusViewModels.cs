using System.Collections.Generic;
using System.Linq;
using Pusula.Core.Types;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Durum etiketi: ad ve renk anlamı (neutral, waiting, active, done, warning).</summary>
    public sealed class StatusChip
    {
        /// <summary>Etiketi oluşturur.</summary>
        public StatusChip(string name, string tone)
        {
            Name = name;
            Tone = tone;
        }

        /// <summary>Durum adı (dağılımda "2 Testte" gibi sayı içerebilir).</summary>
        public string Name { get; }

        /// <summary>Renk anlamı.</summary>
        public string Tone { get; }

        /// <summary>Tür kalıbına göre durum etiketi; durum boş ya da tanımsızsa "—" nötr.</summary>
        public static StatusChip For(TypeDefinition type, string status)
        {
            if (string.IsNullOrEmpty(status))
            {
                return new StatusChip("—", StatusTone.Neutral);
            }

            var definition = type.FindStatus(status);
            return new StatusChip(status, definition?.Tone ?? StatusTone.Neutral);
        }
    }

    /// <summary>Aşama çubuğunda bir adım.</summary>
    public sealed class StatusStep
    {
        /// <summary>Geçilmiş adım.</summary>
        public const string Past = "past";

        /// <summary>Şu anki adım.</summary>
        public const string Current = "current";

        /// <summary>Henüz gelinmemiş adım.</summary>
        public const string Future = "future";

        /// <summary>Adımı oluşturur.</summary>
        public StatusStep(string name, string state)
        {
            Name = name;
            State = state;
        }

        /// <summary>Durum adı.</summary>
        public string Name { get; }

        /// <summary>past, current, future.</summary>
        public string State { get; }

        /// <summary>
        /// Sıralı durumlardan aşama çubuğu kurar. Durum sıra dışıysa (Beklemede gibi) ya da tanımsızsa
        /// bütün adımlar "gelecek" görünür; sıra dışı durum etiketin kendisinde gösterilir.
        /// </summary>
        public static List<StatusStep> Build(TypeDefinition type, string status)
        {
            var index = type.Statuses.FindIndex(s => string.Equals(s.Name, status, System.StringComparison.OrdinalIgnoreCase));
            return type.Statuses
                .Select((s, i) => new StatusStep(s.Name, index < 0 ? Future : i < index ? Past : i == index ? Current : Future))
                .ToList();
        }
    }
}
