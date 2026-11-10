using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Pusula.Core.Tracking;
using Pusula.Core.Types;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels.TypeEditor
{
    /// <summary>Tablolarda tek sütunlu metin satırı (alt tür, şablon klasörü).</summary>
    public sealed class TextRow
    {
        /// <summary>Metin.</summary>
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>Numara biçimi satırı.</summary>
    public sealed class FormatRow
    {
        /// <summary>Biçim (<c>#</c> rakam, <c>@</c> harf).</summary>
        public string Pattern { get; set; } = string.Empty;

        /// <summary>Etiket.</summary>
        public string Label { get; set; } = string.Empty;
    }

    /// <summary>Alan satırı; seçenekler ve alt türler virgülle ayrılmış metin olarak düzenlenir.</summary>
    public sealed class FieldRow
    {
        /// <summary>Değişmez anahtar; yeni alanda kaydederken addan üretilir.</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>Görünen ad.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Çeşit (<see cref="FieldKind"/>).</summary>
        public string Kind { get; set; } = FieldKind.Text;

        /// <summary>Birim (sayı için).</summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>Seçenekler, virgülle ("DB9, SMA, N").</summary>
        public string Choices { get; set; } = string.Empty;

        /// <summary>Geçerli alt türler, virgülle; boşsa hepsi.</summary>
        public string SubTypes { get; set; } = string.Empty;

        /// <summary>Alt türe göre seçenek listeleri (kablo ucu gibi); bu pencerede düzenlenmez, kaydederken korunur.</summary>
        public System.Collections.Generic.List<SubTypeChoiceList> SubTypeChoices { get; set; } = new System.Collections.Generic.List<SubTypeChoiceList>();
    }

    /// <summary>Etiket satırı: ad ve geçerli olduğu yer (metin: "Kablo · RF, Jig"; boşsa her yerde).</summary>
    public sealed class TagRow
    {
        /// <summary>Ad (<c>#</c>'sız; kaydederken boşluk alt çizgi olur).</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Geçerli olduğu yer: "Tür" ya da "Tür · Alt tür", virgülle; boşsa her yerde.</summary>
        public string Scope { get; set; } = string.Empty;
    }

    /// <summary>Durum satırı. Var olan durumun adı değiştirilemez (kayıtların geçmişi bu ada bağlı).</summary>
    public sealed class StatusRow
    {
        /// <summary>Durum adı.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Renk / anlam.</summary>
        public string Tone { get; set; } = StatusTone.Neutral;

        /// <summary>Sıra dışı durum mu.</summary>
        public bool IsSide { get; set; }

        /// <summary>Seçilirken not istenir mi.</summary>
        public bool NeedsNote { get; set; }

        /// <summary>Dosyada zaten olan durum mu (adı kilitli).</summary>
        public bool IsExisting { get; set; }
    }

    /// <summary>Açılır listede seçenek: saklanan anahtar ve görünen ad.</summary>
    public sealed class ChoiceOption
    {
        /// <summary>Seçeneği oluşturur.</summary>
        public ChoiceOption(string key, string label)
        {
            Key = key;
            Label = label;
        }

        /// <summary>Saklanan değer.</summary>
        public string Key { get; }

        /// <summary>Görünen ad.</summary>
        public string Label { get; }
    }

    /// <summary>Simge seçeneği: Segoe Fluent Icons kod noktası.</summary>
    public sealed class IconOption
    {
        /// <summary>Seçeneği oluşturur.</summary>
        public IconOption(string hex)
        {
            Hex = hex;
            Glyph = TreeNodeViewModel.GlyphOf(hex);
        }

        /// <summary>Onaltılık kod ("E703").</summary>
        public string Hex { get; }

        /// <summary>Simge karakteri.</summary>
        public string Glyph { get; }
    }

    /// <summary>Düzenlenen bir envanter türü.</summary>
    public sealed class TypeEditItem : ViewModelBase
    {
        private string name;
        private string pluralName;
        private string icon;
        private string color;
        private string tryNumber = string.Empty;
        private string tryResult = string.Empty;
        private readonly string autoGeneral;
        private readonly string autoProject;
        private readonly string autoStock;

        /// <summary>Türden düzenleme kopyası kurar; <paramref name="isNew"/> ise klasörler düzenlenebilir.</summary>
        public TypeEditItem(TypeDefinition source, bool isNew)
        {
            Source = source;
            IsNew = isNew;
            name = source.Name;
            pluralName = source.PluralName;
            icon = source.Icon;
            color = source.Color;
            GeneralFolder = source.GeneralFolder;
            ProjectFolder = source.ProjectFolder;
            StockFolder = source.StockFolder;
            autoGeneral = GeneralFolder;
            autoProject = ProjectFolder;
            autoStock = StockFolder;
            foreach (var format in source.NumberFormats)
            {
                Formats.Add(new FormatRow { Pattern = format.Pattern, Label = format.Label });
            }

            foreach (var subType in source.SubTypes)
            {
                SubTypes.Add(new TextRow { Text = subType });
            }

            foreach (var field in source.Fields)
            {
                Fields.Add(new FieldRow
                {
                    Key = field.Key,
                    Name = field.Name,
                    Kind = field.Kind,
                    Unit = field.Unit,
                    Choices = string.Join(", ", field.Choices),
                    SubTypes = string.Join(", ", field.SubTypes),
                    SubTypeChoices = field.SubTypeChoices,
                });
            }

            foreach (var status in source.Statuses)
            {
                Statuses.Add(new StatusRow { Name = status.Name, Tone = status.Tone, NeedsNote = status.NeedsNote, IsExisting = !isNew });
            }

            foreach (var status in source.SideStatuses)
            {
                Statuses.Add(new StatusRow { Name = status.Name, Tone = status.Tone, NeedsNote = status.NeedsNote, IsSide = true, IsExisting = !isNew });
            }

            foreach (var folder in source.RecordFolders)
            {
                RecordFolders.Add(new TextRow { Text = folder });
            }
        }

        /// <summary>Kaynak tür (anahtar ve bilinmeyen alanlar korunur).</summary>
        public TypeDefinition Source { get; }

        /// <summary>Bu pencerede eklenen tür mü.</summary>
        public bool IsNew { get; }

        /// <summary>Var olan türün düzey klasörleri kilitli mi (kayıtlar o klasörlerde durur).</summary>
        public bool AreFoldersLocked
        {
            get { return !IsNew; }
        }

        /// <summary>Ad.</summary>
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value ?? string.Empty); }
        }

        /// <summary>Çoğul ad (sekme ve ağaç başlığı).</summary>
        public string PluralName
        {
            get { return pluralName; }
            set { SetProperty(ref pluralName, value ?? string.Empty); }
        }

        /// <summary>Simge (onaltılık).</summary>
        public string Icon
        {
            get { return icon; }
            set
            {
                if (SetProperty(ref icon, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(Glyph));
                }
            }
        }

        /// <summary>Simge karakteri.</summary>
        public string Glyph
        {
            get { return TreeNodeViewModel.GlyphOf(icon); }
        }

        /// <summary>Renk ("#F7A35C").</summary>
        public string Color
        {
            get { return color; }
            set { SetProperty(ref color, value ?? string.Empty); }
        }

        /// <summary>Genel düzey klasörü.</summary>
        public string GeneralFolder { get; set; }

        /// <summary>Proje düzeyi klasörü.</summary>
        public string ProjectFolder { get; set; }

        /// <summary>Stok düzeyi klasörü.</summary>
        public string StockFolder { get; set; }

        /// <summary>Numara biçimleri.</summary>
        public ObservableCollection<FormatRow> Formats { get; } = new ObservableCollection<FormatRow>();

        /// <summary>Alt türler.</summary>
        public ObservableCollection<TextRow> SubTypes { get; } = new ObservableCollection<TextRow>();

        /// <summary>Alanlar.</summary>
        public ObservableCollection<FieldRow> Fields { get; } = new ObservableCollection<FieldRow>();

        /// <summary>Durumlar (sıralılar önce, sonra sıra dışılar).</summary>
        public ObservableCollection<StatusRow> Statuses { get; } = new ObservableCollection<StatusRow>();

        /// <summary>Şablon klasörleri.</summary>
        public ObservableCollection<TextRow> RecordFolders { get; } = new ObservableCollection<TextRow>();

        /// <summary>"Dene" kutusu: yazılan numara hangi biçime uyuyor.</summary>
        public string TryNumber
        {
            get { return tryNumber; }
            set
            {
                if (SetProperty(ref tryNumber, value ?? string.Empty))
                {
                    var number = tryNumber.Trim();
                    var match = Formats.FirstOrDefault(f => f.Pattern.Trim().Length > 0 && NumberPattern.IsMatch(f.Pattern.Trim(), number));
                    TryResult = number.Length == 0 ? string.Empty
                        : match != null ? string.Format(Resources.Types_TryMatch, match.Label.Length == 0 ? match.Pattern : match.Label)
                        : Resources.Types_TryNoMatch;
                }
            }
        }

        /// <summary>Deneme sonucu.</summary>
        public string TryResult
        {
            get { return tryResult; }
            private set { SetProperty(ref tryResult, value); }
        }

        /// <summary>Düzenlenen değerleri türe çevirir; geçersizse hata metni döner.</summary>
        public string Build(out TypeDefinition result)
        {
            result = Source;
            var title = Name.Trim();
            if (title.Length == 0)
            {
                return Resources.Types_ErrorName;
            }

            var formats = Formats.Where(f => f.Pattern.Trim().Length > 0).ToList();
            var badFormat = formats.FirstOrDefault(f => f.Pattern.Trim().Any(c => char.IsWhiteSpace(c)));
            if (badFormat != null)
            {
                return string.Format(Resources.Types_ErrorPattern, title, badFormat.Pattern);
            }

            var fields = Fields.Where(f => f.Name.Trim().Length > 0).ToList();
            var duplicate = fields.GroupBy(f => FieldKeyOf(f), StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
            if (duplicate != null)
            {
                return string.Format(Resources.Types_ErrorField, title, duplicate.First().Name);
            }

            var statuses = Statuses.Where(s => s.Name.Trim().Length > 0).ToList();
            if (!statuses.Any(s => !s.IsSide))
            {
                return string.Format(Resources.Types_ErrorStatus, title);
            }

            // Yeni türde klasörler elle değiştirilmediyse son çoğul addan yeniden üretilir.
            if (IsNew && GeneralFolder == autoGeneral && ProjectFolder == autoProject && StockFolder == autoStock)
            {
                var folder = FolderName(PluralName.Trim().Length == 0 ? title : PluralName.Trim());
                GeneralFolder = @"ENVANTER\" + folder;
                ProjectFolder = @"00_Proje_Geneli\Envanter\" + folder;
                StockFolder = "09_" + folder;
            }

            var type = new TypeDefinition
            {
                Key = Source.Key,
                Name = title,
                PluralName = PluralName.Trim().Length == 0 ? title : PluralName.Trim(),
                Icon = Icon,
                Color = Color,
                IsInventory = true,
                Folder = Source.Folder,
                GeneralFolder = GeneralFolder.Trim(),
                ProjectFolder = ProjectFolder.Trim(),
                StockFolder = StockFolder.Trim(),
            };
            type.NumberFormats.AddRange(formats.Select(f => new NumberFormat { Pattern = f.Pattern.Trim(), Label = f.Label.Trim() }));
            type.SubTypes.AddRange(SubTypes.Select(s => s.Text.Trim()).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase));
            foreach (var field in fields)
            {
                var definition = new FieldDefinition
                {
                    Key = FieldKeyOf(field),
                    Name = field.Name.Trim(),
                    Kind = FieldKind.All.Contains(field.Kind) ? field.Kind : FieldKind.Text,
                    Unit = field.Unit.Trim(),
                };
                definition.Choices.AddRange(SplitList(field.Choices));
                definition.SubTypes.AddRange(SplitList(field.SubTypes));
                definition.SubTypeChoices.AddRange(field.SubTypeChoices);
                type.Fields.Add(definition);
            }

            type.Statuses.AddRange(statuses.Where(s => !s.IsSide).Select(ToDefinition));
            type.SideStatuses.AddRange(statuses.Where(s => s.IsSide).Select(ToDefinition));
            type.RecordFolders.AddRange(RecordFolders.Select(f => f.Text.Trim()).Where(f => f.Length > 0 && ProjectService.IsSafeRelativePath(f)));
            result = type;
            return string.Empty;
        }

        /// <summary>Çoğul addan klasör adı: "Yazılımlar" → "Yazilimlar".</summary>
        public static string FolderName(string plural)
        {
            var key = FieldDefinition.KeyFrom(plural);
            return key.Length == 0 ? "Tur" : char.ToUpperInvariant(key[0]) + key.Substring(1);
        }

        private static StatusDefinition ToDefinition(StatusRow row)
        {
            return new StatusDefinition { Name = row.Name.Trim(), Tone = row.Tone, NeedsNote = row.NeedsNote };
        }

        private static string FieldKeyOf(FieldRow row)
        {
            return row.Key.Length > 0 ? row.Key : FieldDefinition.KeyFrom(row.Name);
        }

        private static IEnumerable<string> SplitList(string text)
        {
            return (text ?? string.Empty).Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Türler penceresi: solda envanter türleri, sağda seçili türün numara biçimleri, alt türleri, alanları,
    /// durumları ve şablon klasörleri. Stok ve proje türleri burada düzenlenmez.
    /// </summary>
    public sealed class TypeEditorViewModel : ViewModelBase
    {
        private readonly string root;
        private readonly TypeSet original;
        private TypeEditItem? selected;
        private string error = string.Empty;

        /// <summary>Kökün tür kalıbından düzenleyici kurar.</summary>
        public TypeEditorViewModel(string root, TypeSet types)
        {
            this.root = root;
            original = types;
            foreach (var type in types.InventoryTypes)
            {
                Types.Add(new TypeEditItem(type, false));
            }

            selected = Types.FirstOrDefault();
            AddTypeCommand = new RelayCommand(AddType);
            foreach (var tag in types.TagList)
            {
                Tags.Add(new TagRow { Name = tag.Name, Scope = string.Join(", ", tag.Scopes.Select(ScopeText)) });
            }
        }

        /// <summary>Etiketler: ad ve geçerli olduğu yer ("Kablo · RF, Jig"; boşsa her yerde).</summary>
        public ObservableCollection<TagRow> Tags { get; } = new ObservableCollection<TagRow>();

        // "Kablo · RF" ← (cable, RF); tür adı yoksa anahtar.
        private string ScopeText(TagScope scope)
        {
            var name = original.Types.FirstOrDefault(t => string.Equals(t.Key, scope.Type, StringComparison.OrdinalIgnoreCase))?.Name ?? scope.Type;
            return scope.SubType.Length > 0 ? name + " · " + scope.SubType : name;
        }

        // Satırlardan etiket listesi: ad düzeltilir (# ve boşluk), aynı ad bir kez; kapsam "Tür · Alt tür" parçalarından.
        private System.Collections.Generic.List<TagDefinition> BuildTags()
        {
            var list = new System.Collections.Generic.List<TagDefinition>();
            foreach (var row in Tags)
            {
                var name = TagDefinition.NameFrom(row.Name);
                if (name.Length == 0 || list.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var tag = new TagDefinition { Name = name };
                foreach (var part in (row.Scope ?? string.Empty).Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var bits = part.Split(new[] { '·', '/' }, 2);
                    var typeText = bits[0].Trim();
                    if (typeText.Length == 0)
                    {
                        continue;
                    }

                    var type = original.Types.FirstOrDefault(t => string.Equals(t.Name, typeText, StringComparison.CurrentCultureIgnoreCase) || string.Equals(t.Key, typeText, StringComparison.OrdinalIgnoreCase));
                    tag.Scopes.Add(new TagScope { Type = type?.Key ?? typeText, SubType = bits.Length > 1 ? bits[1].Trim() : string.Empty });
                }

                list.Add(tag);
            }

            return list;
        }

        /// <summary>Simge seçenekleri.</summary>
        public static IReadOnlyList<IconOption> Icons { get; } = new[]
        {
            "E703", "E90F", "E943", "E74C", "E8F1", "E7EE", "E950", "E964", "E83F", "E7F8", "E80F", "E1D3", "E88E", "E8B7", "E945", "E9F5",
        }.Select(h => new IconOption(h)).ToList();

        /// <summary>Renk seçenekleri.</summary>
        public static IReadOnlyList<string> Colors { get; } = new[]
        {
            "#F7A35C", "#B4A7F5", "#6CCB5F", "#60CDFF", "#E8B04B", "#F08080", "#5FD4C4", "#C9C9C9",
        };

        /// <summary>Alan çeşitleri (anahtar → görünen ad).</summary>
        public static IReadOnlyList<ChoiceOption> FieldKinds { get; } = new[]
        {
            new ChoiceOption(FieldKind.Text, Resources.FieldKind_Text),
            new ChoiceOption(FieldKind.Number, Resources.FieldKind_Number),
            new ChoiceOption(FieldKind.YesNo, Resources.FieldKind_YesNo),
            new ChoiceOption(FieldKind.Choice, Resources.FieldKind_Choice),
        };

        /// <summary>Durum renkleri (anahtar → görünen ad).</summary>
        public static IReadOnlyList<ChoiceOption> Tones { get; } = new[]
        {
            new ChoiceOption(StatusTone.Neutral, Resources.Tone_Neutral),
            new ChoiceOption(StatusTone.Waiting, Resources.Tone_Waiting),
            new ChoiceOption(StatusTone.Active, Resources.Tone_Active),
            new ChoiceOption(StatusTone.Done, Resources.Tone_Done),
            new ChoiceOption(StatusTone.Warning, Resources.Tone_Warning),
            new ChoiceOption(StatusTone.Failed, Resources.Tone_Failed),
        };

        /// <summary>Envanter türleri.</summary>
        public ObservableCollection<TypeEditItem> Types { get; } = new ObservableCollection<TypeEditItem>();

        /// <summary>Seçili tür.</summary>
        public TypeEditItem? Selected
        {
            get { return selected; }
            set
            {
                if (SetProperty(ref selected, value))
                {
                    OnPropertyChanged(nameof(HasSelection));
                }
            }
        }

        /// <summary>Tür seçili mi.</summary>
        public bool HasSelection
        {
            get { return selected != null; }
        }

        /// <summary>Kaydetmede bulunan hata; yoksa boş.</summary>
        public string Error
        {
            get { return error; }
            private set { SetProperty(ref error, value); }
        }

        /// <summary>Yeni tür ekler (kablo / jig ile aynı durumlarla başlar).</summary>
        public ICommand AddTypeCommand { get; }

        private void AddType()
        {
            var name = Resources.Types_NewName;
            var plural = Resources.Types_NewPlural;
            var key = UniqueKey(FieldDefinition.KeyFrom(name));
            var folder = TypeEditItem.FolderName(plural);
            var template = TypeSet.CreateDefault().Find("cable")!;
            var type = new TypeDefinition
            {
                Key = key,
                Name = name,
                PluralName = plural,
                Icon = "E8F1",
                Color = "#6CCB5F",
                IsInventory = true,
                GeneralFolder = @"ENVANTER\" + folder,
                ProjectFolder = @"00_Proje_Geneli\Envanter\" + folder,
                StockFolder = "09_" + folder,
            };
            type.Statuses.AddRange(template.Statuses);
            type.SideStatuses.AddRange(template.SideStatuses);
            type.RecordFolders.AddRange(new[] { "01_Belgeler", "02_Fotograflar" });
            var item = new TypeEditItem(type, true);
            Types.Add(item);
            Selected = item;
        }

        private string UniqueKey(string baseKey)
        {
            return UniqueKey(baseKey, Types.Select(t => t.Source.Key));
        }

        private string UniqueKey(string baseKey, IEnumerable<string> taken)
        {
            var key = baseKey.Length == 0 ? "tur" : baseKey;
            var used = new HashSet<string>(original.Types.Where(t => !t.IsInventory).Select(t => t.Key).Concat(original.InventoryTypes.Select(t => t.Key)).Concat(taken), StringComparer.OrdinalIgnoreCase);
            var candidate = key;
            for (var i = 2; used.Contains(candidate); i++)
            {
                candidate = key + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            return candidate;
        }

        /// <summary>Doğrular ve <c>turler.json</c>'a yazar. Başarılıysa <c>true</c>.</summary>
        public bool Save()
        {
            var built = new List<TypeDefinition>();
            foreach (var item in Types)
            {
                TypeDefinition type;
                var problem = item.Build(out type);
                if (problem.Length > 0)
                {
                    Selected = item;
                    Error = problem;
                    return false;
                }

                // Yeni türün anahtarı son addan ("Yazılım" → yazilim); var olan türün anahtarı değişmez.
                if (item.IsNew)
                {
                    type.Key = UniqueKey(FieldDefinition.KeyFrom(type.Name), built.Select(t => t.Key));
                }

                built.Add(type);
            }

            var clash = built.SelectMany(t => new[] { t.GeneralFolder, t.StockFolder })
                .Where(f => f.Length > 0)
                .GroupBy(f => f, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);
            if (clash != null)
            {
                Error = string.Format(Resources.Types_ErrorFolder, clash.Key);
                return false;
            }

            var sameName = built.GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
            if (sameName != null)
            {
                Error = string.Format(Resources.Types_ErrorDuplicate, sameName.Key);
                return false;
            }

            // Bu pencerenin düzenlemediği her şey olduğu gibi korunur (etiketler, dosya türleri, sürüm).
            var set = new TypeSet { SchemaVersion = original.SchemaVersion };
            set.Tags.AddRange(original.Tags);
            set.TagList.AddRange(BuildTags());
            set.FileTypes.AddRange(original.FileTypes);
            set.Types.AddRange(original.Types.Where(t => !t.IsInventory));
            set.Types.AddRange(built);
            var failure = TypeStore.Save(root, set);
            if (failure.Length > 0)
            {
                Error = string.Format(Resources.Types_ErrorSave, failure);
                return false;
            }

            Error = string.Empty;
            return true;
        }
    }
}
