using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Pusula.Core.Tracking;
using Pusula.Core.Types;

namespace Pusula.Core.Inventory
{
    /// <summary>Kaydın durduğu düzey.</summary>
    public enum InventoryLevel
    {
        /// <summary>Projelerden bağımsız (<c>&lt;kök&gt;\ENVANTER\…</c>).</summary>
        General,

        /// <summary>Projeye ortak (<c>&lt;proje&gt;\00_Proje_Geneli\Envanter\…</c>).</summary>
        Project,

        /// <summary>Stoka özel (<c>&lt;stok&gt;\05_Kablolar</c> …).</summary>
        Stock,
    }

    /// <summary>Okunan bir envanter kaydı: klasörü, düzeyi, sahibi (proje / stok adı) ve içeriği.</summary>
    public sealed class InventoryItem
    {
        /// <summary>Kayıt klasörü.</summary>
        public string Folder { get; set; } = string.Empty;

        /// <summary>Düzey.</summary>
        public InventoryLevel Level { get; set; }

        /// <summary>Proje ya da stok düzeyinde sahibinin klasör adı; genelde boş.</summary>
        public string Owner { get; set; } = string.Empty;

        /// <summary>Kayıt.</summary>
        public InventoryRecord Record { get; set; } = new InventoryRecord();
    }

    /// <summary>
    /// Envanter kayıtlarını oluşturur, durum olayı ekler ve kökteki bütün kayıtları (üç düzeyde) okur.
    /// Var olan hiçbir şeyi ezmez, hiçbir şeyi silmez.
    /// </summary>
    public static class InventoryService
    {
        /// <summary>Kaydın dosya yolu.</summary>
        public static string RecordFile(string folder)
        {
            return Path.Combine(folder, TrackingFiles.FolderName, InventoryRecord.FileName);
        }

        /// <summary>Verilen düzeyde türün klasörü; düzey klasörü tanımlı değilse boş.</summary>
        public static string LevelFolder(TypeDefinition type, InventoryLevel level, string root, string owner)
        {
            switch (level)
            {
                case InventoryLevel.General:
                    return type.GeneralFolder.Length == 0 ? string.Empty : Path.Combine(root, type.GeneralFolder);
                case InventoryLevel.Project:
                    return type.ProjectFolder.Length == 0 ? string.Empty : Path.Combine(owner, type.ProjectFolder);
                default:
                    return type.StockFolder.Length == 0 ? string.Empty : Path.Combine(owner, type.StockFolder);
            }
        }

        /// <summary>
        /// Kayıt açar: <c>&lt;düzey klasörü&gt;\&lt;numara&gt;_&lt;tanım&gt;\_TAKIP\kayit.json</c> (gizli _TAKIP) ve ilk olay.
        /// Numara biçime uymuyorsa ya da aynı numara kökte zaten varsa açmaz.
        /// </summary>
        public static CreateResult Create(string root, TypeDefinition type, InventoryLevel level, string owner, string number, string name, string subType, string note, string person)
        {
            number = (number ?? string.Empty).Trim().ToUpperInvariant();
            name = (name ?? string.Empty).Trim();
            if (number.Length == 0)
            {
                return CreateResult.Fail(CreateError.NumberEmpty, string.Empty);
            }

            if (name.Length == 0)
            {
                return CreateResult.Fail(CreateError.NameEmpty, string.Empty);
            }

            if (!type.AcceptsNumber(number))
            {
                return CreateResult.Fail(CreateError.NumberFormat, string.Empty);
            }

            var existing = ReadAll(root, new[] { type }).FirstOrDefault(i => string.Equals(i.Record.Number, number, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                return CreateResult.Fail(CreateError.AlreadyExists, existing.Folder);
            }

            var parent = LevelFolder(type, level, root, owner);
            if (parent.Length == 0)
            {
                return CreateResult.Fail(CreateError.ParentMissing, string.Empty);
            }

            var folderName = number + (name.Length > 0 ? "_" + ToFolderName(name) : string.Empty);
            var folder = Path.Combine(parent, folderName);
            if (Directory.Exists(folder) || File.Exists(folder))
            {
                return CreateResult.Fail(CreateError.AlreadyExists, folder);
            }

            var record = new InventoryRecord
            {
                Type = type.Key,
                Number = number,
                Name = name,
                SubType = (subType ?? string.Empty).Trim(),
                Note = (note ?? string.Empty).Trim(),
            };
            if (type.InitialStatus.Length > 0)
            {
                record.Events.Add(InventoryEvent.Create(type.InitialStatus, DateTime.Today, person, string.Empty, string.Empty));
            }

            try
            {
                Directory.CreateDirectory(folder);
                JsonFile.WriteAtomic(RecordFile(folder), record);
                TrackingFiles.HideTrackingFolder(folder);
                return CreateResult.Success(folder);
            }
            catch (IOException ex)
            {
                return CreateResult.Fail(CreateError.IoFailure, folder, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateResult.Fail(CreateError.IoFailure, folder, ex.Message);
            }
        }

        /// <summary>Kayda bir durum olayı ekler (diğer alanlara dokunmaz). Okunamaz ya da yazılamazsa <c>false</c>.</summary>
        public static bool AddEvent(string folder, InventoryEvent inventoryEvent)
        {
            var path = RecordFile(folder);
            var record = JsonFile.TryRead<InventoryRecord>(path);
            if (record == null)
            {
                return false;
            }

            record.Events.Add(inventoryEvent);
            try
            {
                JsonFile.WriteAtomic(path, record);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>Kökteki bütün kayıtları üç düzeyde okur: genel, her proje, her stok.</summary>
        public static List<InventoryItem> ReadAll(string root, IEnumerable<TypeDefinition> types)
        {
            var result = new List<InventoryItem>();
            var typeList = types.ToList();
            foreach (var type in typeList)
            {
                Collect(LevelFolder(type, InventoryLevel.General, root, string.Empty), type, InventoryLevel.General, string.Empty, result);
            }

            foreach (var entry in FolderReader.ListChildren(root))
            {
                if (entry.Kind == FolderKind.Project)
                {
                    foreach (var type in typeList)
                    {
                        Collect(LevelFolder(type, InventoryLevel.Project, root, entry.Path), type, InventoryLevel.Project, entry.Title, result);
                    }

                    foreach (var stock in FolderReader.ListChildren(entry.Path).Where(c => c.Kind == FolderKind.Stock))
                    {
                        foreach (var type in typeList)
                        {
                            Collect(LevelFolder(type, InventoryLevel.Stock, root, stock.Path), type, InventoryLevel.Stock, stock.Title, result);
                        }
                    }
                }
                else if (entry.Kind == FolderKind.Stock)
                {
                    foreach (var type in typeList)
                    {
                        Collect(LevelFolder(type, InventoryLevel.Stock, root, entry.Path), type, InventoryLevel.Stock, entry.Title, result);
                    }
                }
            }

            return result.OrderBy(i => i.Record.Number, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static void Collect(string folder, TypeDefinition type, InventoryLevel level, string owner, List<InventoryItem> result)
        {
            if (folder.Length == 0 || !Directory.Exists(folder))
            {
                return;
            }

            string[] children;
            try
            {
                children = Directory.GetDirectories(folder);
            }
            catch (IOException)
            {
                return;
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }

            foreach (var child in children)
            {
                var record = JsonFile.TryRead<InventoryRecord>(RecordFile(child));
                if (record == null || !string.Equals(record.Type, type.Key, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.Add(new InventoryItem { Folder = child, Level = level, Owner = owner, Record = record });
            }
        }

        // "SATIMO adaptör plakası" → "SATIMO_adaptor_plakasi"
        private static string ToFolderName(string name)
        {
            var builder = new StringBuilder();
            foreach (var c in name)
            {
                switch (c)
                {
                    case 'ç': builder.Append('c'); break;
                    case 'Ç': builder.Append('C'); break;
                    case 'ğ': builder.Append('g'); break;
                    case 'Ğ': builder.Append('G'); break;
                    case 'ı': builder.Append('i'); break;
                    case 'İ': builder.Append('I'); break;
                    case 'ö': builder.Append('o'); break;
                    case 'Ö': builder.Append('O'); break;
                    case 'ş': builder.Append('s'); break;
                    case 'Ş': builder.Append('S'); break;
                    case 'ü': builder.Append('u'); break;
                    case 'Ü': builder.Append('U'); break;
                    case ' ': builder.Append('_'); break;
                    default:
                        if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
                        {
                            builder.Append(c);
                        }

                        break;
                }
            }

            return builder.ToString();
        }
    }
}
