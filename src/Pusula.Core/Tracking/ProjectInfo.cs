using System.Runtime.Serialization;

namespace Pusula.Core.Tracking
{
    /// <summary>
    /// <c>_TAKIP\proje.json</c> içeriği.
    /// </summary>
    [DataContract]
    public sealed class ProjectInfo
    {
        /// <summary>Serileştirici kurucuyu çağırmaz; dosyada olmayan alanlar boş değerle başlar.</summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            SchemaVersion = 1;
            ProjectNo = string.Empty;
            Name = string.Empty;
            Notes = new System.Collections.Generic.List<QuickNote>();
        }

        /// <summary>Uygulamadan yazılan kısa notlar (eskiden yeniye).</summary>
        [DataMember(Name = "notes", Order = 3, EmitDefaultValue = false)]
        public System.Collections.Generic.List<QuickNote> Notes { get; set; } = new System.Collections.Generic.List<QuickNote>();

        /// <summary>Dosya biçiminin sürümü.</summary>
        [DataMember(Name = "schemaVersion", Order = 0)]
        public int SchemaVersion { get; set; } = 1;

        /// <summary>Proje numarası, örn. 7000.</summary>
        [DataMember(Name = "projectNo", Order = 1)]
        public string ProjectNo { get; set; } = string.Empty;

        /// <summary>Proje adı, örn. MILKON.</summary>
        [DataMember(Name = "name", Order = 2)]
        public string Name { get; set; } = string.Empty;
    }
}
