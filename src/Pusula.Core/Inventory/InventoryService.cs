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
                Pusula.Core.Scanning.DiskWatcher.MarkOwnWrite();
                Directory.CreateDirectory(folder);
                JsonFile.WriteAtomic(RecordFile(folder), record);
                TrackingFiles.HideTrackingFolder(folder);
                ProjectService.AddFolders(folder, type.RecordFolders);
                CreateNote(folder, record);
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

        /// <summary>
        /// Elle açılmış bir klasörü kayıt olarak işaretler: yalnızca gizli <c>_TAKIP\kayit.json</c> eklenir,
        /// klasördeki dosyalara dokunulmaz, klasör adı değişmez. İlk olay verilen durumla yazılır.
        /// </summary>
        public static CreateResult Mark(string root, TypeDefinition type, string folder, string number, string name, string subType, string status, string person, string note = "")
        {
            number = (number ?? string.Empty).Trim().ToUpperInvariant();
            name = (name ?? string.Empty).Trim();
            if (!Directory.Exists(folder))
            {
                return CreateResult.Fail(CreateError.ParentMissing, string.Empty);
            }

            if (File.Exists(RecordFile(folder)))
            {
                return CreateResult.Fail(CreateError.AlreadyMarked, folder);
            }

            if (number.Length == 0)
            {
                return CreateResult.Fail(CreateError.NumberEmpty, folder);
            }

            if (name.Length == 0)
            {
                return CreateResult.Fail(CreateError.NameEmpty, folder);
            }

            if (!type.AcceptsNumber(number))
            {
                return CreateResult.Fail(CreateError.NumberFormat, folder);
            }

            var existing = ReadAll(root, new[] { type }).FirstOrDefault(i => string.Equals(i.Record.Number, number, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                return CreateResult.Fail(CreateError.AlreadyExists, existing.Folder);
            }

            var record = new InventoryRecord { Type = type.Key, Number = number, Name = name, SubType = (subType ?? string.Empty).Trim(), Note = (note ?? string.Empty).Trim() };
            record.Events.Add(InventoryEvent.Create(string.IsNullOrEmpty(status) ? type.InitialStatus : status, DateTime.Today, person, string.Empty, string.Empty));
            try
            {
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

        /// <summary>
        /// Klasör adından numara ve tanım önerir: "JIG-1000-0030_Test_Aparati" → ("JIG-1000-0030", "Test Aparati").
        /// İlk parça biçime uymuyorsa numara boş, bütün ad tanım olur.
        /// </summary>
        public static Tuple<string, string> SuggestFromFolderName(TypeDefinition type, string folderName)
        {
            var underscore = folderName.IndexOf('_');
            var first = underscore > 0 ? folderName.Substring(0, underscore) : folderName;
            if (type.AcceptsNumber(first) && type.NumberFormats.Count > 0)
            {
                var rest = underscore > 0 ? folderName.Substring(underscore + 1).Replace('_', ' ').Trim() : string.Empty;
                return Tuple.Create(first.ToUpperInvariant(), rest);
            }

            return Tuple.Create(string.Empty, folderName.Replace('_', ' ').Trim());
        }

        /// <summary>
        /// Düzey klasörlerinde kaydı olmayan (elle açılmış) klasörleri bulur; <c>_</c> ile başlayanlar ve kayıt şablonunun
        /// alt klasörleri (örn. stoktaki 05_Jigler\Mekanik) hariç.
        /// </summary>
        public static List<InventoryItem> ReadUnmarked(string root, IEnumerable<TypeDefinition> types)
        {
            var result = new List<InventoryItem>();
            foreach (var type in types)
            {
                foreach (var level in LevelFolders(root, type))
                {
                    string[] children;
                    try
                    {
                        children = Directory.Exists(level.Item1) ? Directory.GetDirectories(level.Item1) : new string[0];
                    }
                    catch (IOException)
                    {
                        continue;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        continue;
                    }

                    foreach (var child in children)
                    {
                        var name = Path.GetFileName(child);
                        if (name.StartsWith("_", StringComparison.Ordinal) || File.Exists(RecordFile(child)))
                        {
                            continue;
                        }

                        // Stok düzeyinde yalnızca numara biçimine uyan klasörler (05_Jigler\Mekanik gibi şablon klasörleri değil).
                        if (level.Item2 == InventoryLevel.Stock && SuggestFromFolderName(type, name).Item1.Length == 0)
                        {
                            continue;
                        }

                        result.Add(new InventoryItem
                        {
                            Folder = child,
                            Level = level.Item2,
                            Owner = level.Item3,
                            Record = new InventoryRecord { Type = type.Key, Number = name },
                        });
                    }
                }
            }

            return result.OrderBy(i => i.Record.Number, StringComparer.OrdinalIgnoreCase).ToList();
        }

        // Türün bütün düzey klasörleri: (klasör, düzey, sahip adı).
        private static IEnumerable<Tuple<string, InventoryLevel, string>> LevelFolders(string root, TypeDefinition type)
        {
            yield return Tuple.Create(LevelFolder(type, InventoryLevel.General, root, string.Empty), InventoryLevel.General, string.Empty);
            foreach (var entry in FolderReader.ListChildren(root))
            {
                if (entry.Kind == FolderKind.Project)
                {
                    yield return Tuple.Create(LevelFolder(type, InventoryLevel.Project, root, entry.Path), InventoryLevel.Project, entry.Title);
                    foreach (var stock in FolderReader.ListChildren(entry.Path).Where(c => c.Kind == FolderKind.Stock))
                    {
                        yield return Tuple.Create(LevelFolder(type, InventoryLevel.Stock, root, stock.Path), InventoryLevel.Stock, stock.Title);
                    }
                }
                else if (entry.Kind == FolderKind.Stock)
                {
                    yield return Tuple.Create(LevelFolder(type, InventoryLevel.Stock, root, entry.Path), InventoryLevel.Stock, entry.Title);
                }
            }
        }

        /// <summary>
        /// Stokun kullandığı kayıtlar: stok klasöründekiler (<c>05_Jigler</c> …) ve <c>stok.json</c>'daki bağlar.
        /// Bağlı olup bulunamayan numaralar <paramref name="missing"/> içine yazılır.
        /// </summary>
        public static List<InventoryItem> UsedByStock(string stockFolder, IEnumerable<string> links, IEnumerable<InventoryItem> all, out List<string> missing)
        {
            var items = all.ToList();
            var prefix = stockFolder.TrimEnd('\\') + "\\";
            var result = items.Where(i => i.Level == InventoryLevel.Stock && i.Folder.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
            missing = new List<string>();
            foreach (var number in links.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var linked = items.FirstOrDefault(i => i.Level != InventoryLevel.Stock && string.Equals(i.Record.Number, number, StringComparison.OrdinalIgnoreCase));
                if (linked == null)
                {
                    missing.Add(number);
                }
                else if (!result.Contains(linked))
                {
                    result.Add(linked);
                }
            }

            return result.OrderBy(i => i.Record.Number, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Stoka genel / proje kaydı bağlar ya da bağı kaldırır (<c>stok.json</c>, diğer alanlara dokunmaz).
        /// Kaydın kendisine ve klasörüne dokunulmaz. Dosya yoksa ya da yazılamazsa <c>false</c>.
        /// </summary>
        public static bool SetStockLink(string stockFolder, string number, bool linked)
        {
            var path = TrackingFiles.StockFile(stockFolder);
            var info = JsonFile.TryRead<StockInfo>(path);
            if (info == null)
            {
                return false;
            }

            info.Inventory.RemoveAll(n => string.Equals(n, number, StringComparison.OrdinalIgnoreCase));
            if (linked)
            {
                info.Inventory.Add(number);
                info.Inventory.Sort(StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                JsonFile.WriteAtomic(path, info);
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

        /// <summary>
        /// Kaydın Obsidian notunu (<c>&lt;numara&gt;.md</c>) YAML bloğu ve başlıkla oluşturur. Not zaten varsa dokunmaz.
        /// Oluşan (ya da var olan) notun yolunu döndürür; yazılamazsa boş.
        /// </summary>
        public static string CreateNote(string folder, InventoryRecord record)
        {
            var fields = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("tur", record.Type),
                new KeyValuePair<string, string>("numara", record.Number),
                new KeyValuePair<string, string>("tanim", record.Name),
                new KeyValuePair<string, string>("alt_tur", record.SubType),
            };
            return Notes.NoteFile.Create(RecordContents.NotePath(folder, record.Number), fields, record.Number + " " + record.Name);
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
