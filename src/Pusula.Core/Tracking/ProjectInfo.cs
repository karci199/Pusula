using System.Runtime.Serialization;

namespace Pusula.Core.Tracking
{
    /// <summary>
    /// <c>_TAKIP\proje.json</c> içeriği.
    /// </summary>
    [DataContract]
    public sealed class ProjectInfo
    {
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
