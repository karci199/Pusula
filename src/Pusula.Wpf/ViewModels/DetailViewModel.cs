using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pusula.Core.Inventory;
using Pusula.Core.Notes;
using Pusula.Core.Scanning;
using Pusula.Core.Templates;
using Pusula.Core.Testing;
using Pusula.Core.Tracking;
using Pusula.Core.Types;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Özet kutucuğu: etiket ve değer.</summary>
    public sealed class SummaryTile
    {
        /// <summary>Kutucuğu oluşturur.</summary>
        public SummaryTile(string label, string value)
        {
            Label = label;
            Value = value;
        }

        /// <summary>Etiket.</summary>
        public string Label { get; }

        /// <summary>Değer.</summary>
        public string Value { get; }
    }

    /// <summary>Özet tablosunun bir satırı (görünüm için biçimlenmiş).</summary>
    public sealed class DetailRowViewModel
    {
        /// <summary>Satırı bir özet satırından oluşturur.</summary>
        public DetailRowViewModel(SummaryRow row, TypeDefinition stockType)
        {
            Name = row.Name;
            Path = row.Path;
            Template = row.Template.Length == 0 ? "—" : row.Template;
            Status = StatusChip.For(stockType, row.Status);
            IsMissing = row.IsMissing;
            IsDim = row.IsMissing || row.IsSpecial || row.Stats.FileCount == 0;
            FileCount = row.IsMissing ? "—" : row.Stats.FileCount.ToString();
            Size = row.Stats.FileCount == 0 ? "—" : Formats.Size(row.Stats.TotalBytes);
            LastChange = row.Stats.LastWrite.HasValue ? Formats.Date(row.Stats.LastWrite.Value) : Resources.Row_Empty;

            if (row.IsMissing)
            {
                LastFile = Resources.Row_Missing;
            }
            else if (row.IsSpecial)
            {
                LastFile = Resources.Row_Special;
            }
            else if (row.Stats.LastWrite.HasValue)
            {
                LastFile = System.IO.Path.GetFileName(row.Stats.LastFile) + " · " + Formats.ShortDate(row.Stats.LastWrite.Value);
            }
            else
            {
                LastFile = Resources.Row_Empty;
            }
        }

        /// <summary>Klasör ya da stok adı.</summary>
        public string Name { get; }

        /// <summary>Tam yol (çift tıklayınca açılır).</summary>
        public string Path { get; }

        /// <summary>Stok satırında şablon.</summary>
        public string Template { get; }

        /// <summary>Stok satırında ana durum.</summary>
        public StatusChip Status { get; }

        /// <summary>Dosya sayısı.</summary>
        public string FileCount { get; }

        /// <summary>Boyut.</summary>
        public string Size { get; }

        /// <summary>Son değişiklik tarihi (stok satırı).</summary>
        public string LastChange { get; }

        /// <summary>Son değişen dosya ve tarihi (klasör satırı).</summary>
        public string LastFile { get; }

        /// <summary>Soluk gösterilir (boş, eksik ya da özel klasör).</summary>
        public bool IsDim { get; }

        /// <summary>Şablonda var, diskte yok.</summary>
        public bool IsMissing { get; }
    }

    /// <summary>
    /// Sağ paneldeki özet: proje seçiliyse stok listesi ve durum dağılımı, stok seçiliyse durum ve klasör listesi.
    /// Sayım arka planda yapılır; seçim değişince önceki sayım iptal edilir.
    /// </summary>
    public sealed class DetailViewModel : ViewModelBase
    {
        private CancellationTokenSource? cancellation;
        private TreeNodeViewModel? node;
        private TemplateSet templates = TemplateSet.CreateDefault();
        private TypeSet types = TypeSet.CreateDefault();
        private bool isLoading;
        private bool isProject;
        private bool isStock;
        private string subtitle = string.Empty;
        private string note = string.Empty;
        private StatusChip status = new StatusChip("—", StatusTone.Neutral);
        private string statusInfo = string.Empty;

        /// <summary>Özet kutucukları.</summary>
        public ObservableCollection<SummaryTile> Tiles { get; } = new ObservableCollection<SummaryTile>();

        /// <summary>Tablo satırları.</summary>
        public ObservableCollection<DetailRowViewModel> Rows { get; } = new ObservableCollection<DetailRowViewModel>();

        /// <summary>Stokta aşama çubuğu.</summary>
        public ObservableCollection<StatusStep> Steps { get; } = new ObservableCollection<StatusStep>();

        /// <summary>Genel sekmesi: üstteki özet kartları (koşu, geçti, kaldı, son koşu).</summary>
        public ObservableCollection<TestKpi> TestKpis { get; } = new ObservableCollection<TestKpi>();

        /// <summary>Genel sekmesi: dikkat maddeleri.</summary>
        public ObservableCollection<AlertItem> Alerts { get; } = new ObservableCollection<AlertItem>();

        /// <summary>Stokun md notlarındaki açık görevler (<c>- [ ]</c>).</summary>
        public ObservableCollection<OpenTaskRow> OpenTasks { get; } = new ObservableCollection<OpenTaskRow>();

        private bool openTasksExpanded = true;

        /// <summary>Açık işler bölümü açık mı (stoktan stoka geçince korunur).</summary>
        public bool OpenTasksExpanded
        {
            get { return openTasksExpanded; }
            set { SetProperty(ref openTasksExpanded, value); }
        }

        /// <summary>Açık görev var mı.</summary>
        public bool HasOpenTasks
        {
            get { return OpenTasks.Count > 0; }
        }

        /// <summary>Açık görevler başlığı ("Açık işler · 4").</summary>
        public string OpenTasksHeader
        {
            get { return string.Format(Resources.General_OpenTasks, OpenTasks.Count); }
        }

        /// <summary>Genel sekmesi: "son sonuçlar" tablosunun sütun başlıkları.</summary>
        public ObservableCollection<string> MatrixHeaders { get; } = new ObservableCollection<string>();

        /// <summary>Genel sekmesi: "son sonuçlar" tablosunun satırları.</summary>
        public ObservableCollection<MatrixRow> MatrixRows { get; } = new ObservableCollection<MatrixRow>();

        /// <summary>Genel sekmesi: zaman çizelgesi.</summary>
        public ObservableCollection<TimelineItem> Timeline { get; } = new ObservableCollection<TimelineItem>();

        /// <summary>Testler sekmesi: geliştirme koşuları.</summary>
        public ObservableCollection<TestRunRow> DevelopmentRuns { get; } = new ObservableCollection<TestRunRow>();

        /// <summary>Kalifikasyon sekmesi: kalifikasyon koşuları.</summary>
        public ObservableCollection<TestRunRow> QualificationRuns { get; } = new ObservableCollection<TestRunRow>();

        /// <summary>Geliştirme test planları (<c>02_Test_Planlari</c>).</summary>
        public ObservableCollection<TestPlanRow> TestPlans { get; } = new ObservableCollection<TestPlanRow>();

        /// <summary>Kalifikasyon test planları (<c>04_Kalifikasyon\…\Test_Plani</c>).</summary>
        public ObservableCollection<TestPlanRow> QualificationPlans { get; } = new ObservableCollection<TestPlanRow>();

        /// <summary>Klasörler sekmesi: stokun klasör ağacı (üst düzey).</summary>
        public ObservableCollection<FolderTreeItem> FolderTree { get; } = new ObservableCollection<FolderTreeItem>();

        /// <summary>Projenin kısa notları (yeniden eskiye).</summary>
        public ObservableCollection<HistoryRow> ProjectNotes { get; } = new ObservableCollection<HistoryRow>();

        /// <summary>Stokun notu ve fotoğrafları (Genel sekmesinin üstü).</summary>
        public StockCoverViewModel Cover { get; } = new StockCoverViewModel();

        /// <summary>Stokun jig, kablo … bölümleri (türe göre ayrı).</summary>
        public ObservableCollection<StockInventoryGroup> InventoryGroups { get; } = new ObservableCollection<StockInventoryGroup>();

        /// <summary>Bütün envanter kayıtları (ana görünüm modeli verir); stokun kullandıkları bunlardan seçilir.</summary>
        public IEnumerable<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();


        /// <summary>Sekme başlığı: "Testler · 4".</summary>
        public string TestsHeader
        {
            get { return string.Format(Resources.Tab_Tests, DevelopmentRuns.Count); }
        }

        /// <summary>Sekme başlığı: "Kalifikasyon · 1".</summary>
        public string QualificationHeader
        {
            get { return string.Format(Resources.Tab_Qualification, QualificationRuns.Count); }
        }

        /// <summary>Hiç test koşusu var mı.</summary>
        public bool HasRuns
        {
            get { return DevelopmentRuns.Count + QualificationRuns.Count > 0; }
        }

        /// <summary>Dikkat edilecek madde var mı.</summary>
        public bool HasAlerts
        {
            get { return Alerts.Count > 0; }
        }

        /// <summary>Projede durum dağılımı ("2 Testte" …).</summary>
        public ObservableCollection<StatusChip> Distribution { get; } = new ObservableCollection<StatusChip>();

        /// <summary>Hesaplanıyor mu.</summary>
        public bool IsLoading
        {
            get { return isLoading; }
            private set { SetProperty(ref isLoading, value); }
        }

        /// <summary>Proje özeti mi gösteriliyor (stok tablosu).</summary>
        public bool IsProject
        {
            get { return isProject; }
            private set { SetProperty(ref isProject, value); }
        }

        /// <summary>Stok özeti mi gösteriliyor (durum ve klasör tablosu).</summary>
        public bool IsStock
        {
            get { return isStock; }
            private set { SetProperty(ref isStock, value); }
        }

        /// <summary>Başlığın üstündeki satır (tür, numara türü, şablon, proje).</summary>
        public string Subtitle
        {
            get { return subtitle; }
            private set { SetProperty(ref subtitle, value); }
        }

        private string headerName = string.Empty;
        private string parentName = string.Empty;
        private string parentPath = string.Empty;
        private string contextText = string.Empty;

        /// <summary>Başlıkta numaranın yanındaki ad (stok adı, proje adı, kayıt tanımı); boş olabilir.</summary>
        public string HeaderName
        {
            get { return headerName; }
            private set { SetProperty(ref headerName, value); }
        }

        /// <summary>Üst kayıt (stokta proje, stoka ait envanterde stok); tıklanınca oraya gidilir. Boş olabilir.</summary>
        public string ParentName
        {
            get { return parentName; }
            private set
            {
                if (SetProperty(ref parentName, value))
                {
                    OnPropertyChanged(nameof(HasParent));
                }
            }
        }

        /// <summary>Üst kaydın klasörü.</summary>
        public string ParentPath
        {
            get { return parentPath; }
            private set { SetProperty(ref parentPath, value); }
        }

        /// <summary>Üst kayıt var mı.</summary>
        public bool HasParent
        {
            get { return parentName.Length > 0; }
        }

        /// <summary>Başlığın ikinci satırı: tür, şablon, alt tür ("Stok · Pasif anten", "Jig · Mekanik").</summary>
        public string ContextText
        {
            get { return contextText; }
            private set { SetProperty(ref contextText, value); }
        }

        /// <summary>Tablonun altındaki not.</summary>
        public string Note
        {
            get { return note; }
            private set { SetProperty(ref note, value); }
        }

        /// <summary>Stokun ana durumu.</summary>
        public StatusChip Status
        {
            get { return status; }
            private set { SetProperty(ref status, value); }
        }

        /// <summary>Durumun yanındaki bilgi ("03.11.2026'dan beri · numune gelmedi").</summary>
        public string StatusInfo
        {
            get { return statusInfo; }
            private set { SetProperty(ref statusInfo, value); }
        }

        /// <summary>Gösterilen stokun klasörü (durum değiştirmek için); stok değilse boş.</summary>
        public string StockPath
        {
            get { return node != null && node.Kind == FolderKind.Stock ? node.Path : string.Empty; }
        }

        /// <summary>Son gösterilen düğümü aynı ayarlarla yeniden hesaplar.</summary>
        public void Reload()
        {
            Load(node, templates, types);
        }

        /// <summary>Seçili düğümün özetini arka planda hesaplar ve gösterir.</summary>
        public async void Load(TreeNodeViewModel? selected, TemplateSet templateSet, TypeSet typeSet)
        {
            cancellation?.Cancel();
            node = selected;
            templates = templateSet;
            types = typeSet;
            Tiles.Clear();
            Rows.Clear();
            Steps.Clear();
            Distribution.Clear();
            Alerts.Clear();
            OpenTasks.Clear();
            NotifyOpenTasks();
            TestKpis.Clear();
            MatrixHeaders.Clear();
            MatrixRows.Clear();
            Timeline.Clear();
            DevelopmentRuns.Clear();
            QualificationRuns.Clear();
            TestPlans.Clear();
            QualificationPlans.Clear();
            FolderTree.Clear();
            InventoryGroups.Clear();
            Cover.Clear();
            ProjectNotes.Clear();
            NotifyTestHeaders();
            IsProject = false;
            IsStock = false;
            Note = string.Empty;
            StatusInfo = string.Empty;
            Subtitle = selected == null ? string.Empty : selected.KindText;
            HeaderName = string.Empty;
            ParentName = string.Empty;
            ParentPath = string.Empty;
            ContextText = Subtitle;
            if (selected?.Item != null)
            {
                // Envanter kaydı: tanım, tür · alt tür; stoka aitse üst kayıt o stok.
                var record = selected.Item.Record;
                HeaderName = record.Name;
                ContextText = string.Join(" · ", new[] { selected.Type?.Name ?? string.Empty, record.SubType }.Where(p => p.Length > 0));
                if (selected.Item.Owner.Length > 0)
                {
                    var folder = Path.GetDirectoryName(selected.Item.Folder);
                    while (!string.IsNullOrEmpty(folder) && !string.Equals(Path.GetFileName(folder), selected.Item.Owner, StringComparison.OrdinalIgnoreCase))
                    {
                        folder = Path.GetDirectoryName(folder);
                    }

                    if (!string.IsNullOrEmpty(folder))
                    {
                        ParentName = selected.Item.Owner;
                        ParentPath = folder!;
                    }
                }
            }

            if (selected == null || (selected.Kind != FolderKind.Project && selected.Kind != FolderKind.Stock))
            {
                IsLoading = false;
                return;
            }

            var source = new CancellationTokenSource();
            cancellation = source;
            IsLoading = true;
            var path = selected.Path;
            var kind = selected.Kind;
            FolderSummary summary;
            List<TestRun> runs = new List<TestRun>();
            FolderTreeNode? tree = null;
            StockCover? cover = null;
            ProjectInfo? projectInfo = null;
            List<EventNote> events = new List<EventNote>();
            List<OpenTask> tasks = new List<OpenTask>();
            List<TestPlan> plans = new List<TestPlan>();
            List<EventNote> projectJournal = new List<EventNote>();
            try
            {
                if (kind == FolderKind.Project)
                {
                    summary = await Task.Run(
                        () =>
                        {
                            projectInfo = JsonFile.TryRead<ProjectInfo>(TrackingFiles.ProjectFile(path));
                            tasks = OpenTask.ReadAll(path);
                            projectJournal = Journal.Read(NoteFile.Find(path, Path.GetFileName(path)));
                            return FolderSummary.ForProject(path, templates.ProjectFolders, source.Token);
                        },
                        source.Token);
                }
                else
                {
                    // Stok: klasör bir kez taranır (her klasör tek listeleme), bütün okuyucular bu taramadan çalışır ve
                    // aynı anda yapılır; md notları da bir kez okunur. Ağ paylaşımında gidiş-dönüş sayısı birkaç kat azalır.
                    var token = source.Token;
                    var currentTemplates = templates;
                    var snapshot = await Task.Run(() => FolderSnapshot.Take(path, token), token).ConfigureAwait(true);
                    using (FolderSnapshot.Activate(snapshot))
                    {
                        var runsTask = Task.Run(() => TestRunReader.ReadStock(path), token);
                        var treeTask = Task.Run(() => FolderTreeNode.Build(path, token), token);
                        var summaryTask = Task.Run(() => FolderSummary.ForStock(path, currentTemplates, token), token);
                        var coverTask = Task.Run(() => StockCover.Read(path, Path.GetFileName(path)), token);
                        var eventsTask = Task.Run(() => EventNote.ReadAll(path), token);
                        var tasksTask = Task.Run(() => OpenTask.ReadAll(path), token);
                        var plansTask = Task.Run(() => TestPlan.ReadStock(path), token);
                        var journalTask = Task.Run(() => Journal.Read(NoteFile.Find(path, Path.GetFileName(path))), token);
                        await Task.WhenAll(runsTask, treeTask, summaryTask, coverTask, eventsTask, tasksTask, plansTask, journalTask).ConfigureAwait(true);
                        plans = plansTask.Result;
                        tasks = tasksTask.Result;
                        summary = summaryTask.Result;
                        runs = runsTask.Result;
                        tree = treeTask.Result;
                        cover = coverTask.Result;
                        events = eventsTask.Result;
                        events.AddRange(journalTask.Result);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (source.IsCancellationRequested)
            {
                return;
            }

            IsLoading = false;
            if (kind == FolderKind.Project)
            {
                ShowProject(summary);
                HeaderName = projectInfo?.Name ?? string.Empty;
                // Kısa notlar ve projenin ana notundaki Günlük olayları birlikte, yeniden eskiye.
                var projectRows = (projectInfo?.Notes ?? new List<QuickNote>()).Select(n => new HistoryRow(n))
                    .Concat(projectJournal.Select(e => new HistoryRow(e)))
                    .OrderByDescending(r => r.SortDate);
                foreach (var row in projectRows)
                {
                    ProjectNotes.Add(row);
                }

                // Projenin bütün stoklarındaki açık işler (kaynak: stok numarası · not).
                foreach (var task in tasks)
                {
                    OpenTasks.Add(new OpenTaskRow(task, path));
                }

                NotifyOpenTasks();
            }
            else
            {
                ShowStock(summary, path);
                if (cover != null)
                {
                    Cover.Show(cover, path);
                }

                ShowTests(runs);
                foreach (var plan in plans)
                {
                    var planKind = types.FindFileType(Path.GetExtension(plan.Path))?.Name ?? Resources.FileType_Other;
                    (plan.IsQualification ? QualificationPlans : TestPlans).Add(new TestPlanRow(plan, runs, planKind));
                }
                AddToTimeline(summary.Stock, events, path);
                foreach (var task in tasks)
                {
                    OpenTasks.Add(new OpenTaskRow(task, string.Empty));
                }

                NotifyOpenTasks();
                ShowInventory(summary.Stock, path);
                if (tree != null)
                {
                    foreach (var child in tree.Children)
                    {
                        FolderTree.Add(new FolderTreeItem(child, 0));
                    }
                }
            }
        }

        private void ShowTests(List<TestRun> runs)
        {
            var overview = TestOverview.Build(runs);
            if (runs.Count > 0)
            {
                var last = overview.Timeline[0];
                TestKpis.Add(new TestKpi(Resources.Kpi_Runs, runs.Count.ToString(), StatusTone.Neutral));
                TestKpis.Add(new TestKpi(Resources.Kpi_Passed, runs.Count(r => r.Result == TestResult.Passed).ToString(), StatusTone.Done));
                TestKpis.Add(new TestKpi(Resources.Kpi_Failed, runs.Count(r => r.Result == TestResult.Failed).ToString(), StatusTone.Failed));
                TestKpis.Add(new TestKpi(Resources.Kpi_Last, (last.Date == DateTime.MinValue ? "—" : last.Date.ToString(Resources.Format_DayMonth)) + " · " + last.System, TestResultChip.For(last.Result).Tone));
            }

            foreach (var alert in overview.Alerts)
            {
                Alerts.Add(AlertItem.From(alert));
            }

            foreach (var column in overview.Columns)
            {
                MatrixHeaders.Add(TestResultChip.SystemLabel(column.Item1, column.Item2));
            }

            foreach (var serial in overview.Serials)
            {
                var cells = overview.Columns.Select(c =>
                {
                    TestRun? last;
                    return overview.Latest.TryGetValue(TestOverview.Key(c.Item1, c.Item2, serial), out last)
                        ? TestResultChip.For(last.Result, "T" + last.Round)
                        : new StatusChip("—", StatusTone.None);
                });
                MatrixRows.Add(new MatrixRow(serial, cells));
            }

            foreach (var run in overview.Timeline)
            {
                Timeline.Add(new TimelineItem(run));
                (run.IsQualification ? QualificationRuns : DevelopmentRuns).Add(new TestRunRow(run));
            }

            NotifyTestHeaders();
        }

        // Stokun kullandığı kayıtlar: türe göre bölümler; sıra dışı durumdakiler ve bulunamayan bağlar "Dikkat"e eklenir.
        private void ShowInventory(StockInfo? info, string path)
        {
            List<string> missing;
            var links = info?.Inventory ?? new List<string>();
            var used = InventoryService.UsedByStock(path, links, InventoryItems, out missing);

            // Her doldurmada baştan: üst üste binen yüklemelerde aynı tür iki kez görünmesin.
            InventoryGroups.Clear();
            foreach (var type in types.InventoryTypes.GroupBy(t => t.Key, StringComparer.OrdinalIgnoreCase).Select(g => g.First()))
            {
                var rows = used.Where(i => string.Equals(i.Record.Type, type.Key, StringComparison.OrdinalIgnoreCase))
                    .Select(i => new InventoryRow(i, type, i.Level != InventoryLevel.Stock))
                    .Concat(missing.Where(type.AcceptsNumber).Select(n => new InventoryRow(n, type)))
                    .ToList();
                var group = new StockInventoryGroup(type, rows);
                InventoryGroups.Add(group);
                foreach (var row in group.Rows)
                {
                    if (row.IsMissing)
                    {
                        Alerts.Add(new AlertItem(AppIcons.Help, string.Format(Resources.Alert_InvMissingTitle, type.Name), row.Number + " · " + Resources.Inv_LinkMissing, StatusTone.Waiting));
                    }
                    else if (type.SideStatuses.Any(s => string.Equals(s.Name, row.Status.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        var last = row.Item?.Record.LastEvent;
                        var detail = row.Number + " · " + row.Status.Name + (last != null && last.Note.Length > 0 ? " · " + last.Note : string.Empty);
                        Alerts.Add(new AlertItem(AppIcons.Repair, type.Name, detail, StatusTone.Waiting));
                    }
                }
            }

            OnPropertyChanged(nameof(HasAlerts));
        }

        // Günlük: test koşularına kısa notlar ve Obsidian olay notları tarih sırasıyla (yeniden eskiye) katılır.
        private void NotifyOpenTasks()
        {
            OnPropertyChanged(nameof(HasOpenTasks));
            OnPropertyChanged(nameof(OpenTasksHeader));
        }

        private void AddToTimeline(StockInfo? info, List<EventNote> events, string path)
        {
            var notes = info?.Notes ?? new List<QuickNote>();
            if (notes.Count == 0 && events.Count == 0)
            {
                return;
            }

            var all = Timeline.Concat(notes.Select(n => new TimelineItem(n, path)))
                .Concat(events.Select(e => new TimelineItem(e)))
                .OrderByDescending(i => i.SortDate)
                .ToList();
            Timeline.Clear();
            foreach (var item in all)
            {
                Timeline.Add(item);
            }
        }

        private void NotifyTestHeaders()
        {
            OnPropertyChanged(nameof(TestsHeader));
            OnPropertyChanged(nameof(QualificationHeader));
            OnPropertyChanged(nameof(HasRuns));
            OnPropertyChanged(nameof(HasAlerts));
        }

        private void ShowProject(FolderSummary summary)
        {
            var stockType = types.Stock;
            Tiles.Add(new SummaryTile(Resources.Tile_Stocks, summary.StockCount.ToString()));
            AddTotalTiles(summary.Total);
            Tiles.Add(new SummaryTile(Resources.Tile_LastChange, summary.Total.LastWrite.HasValue ? Formats.ShortDate(summary.Total.LastWrite.Value) : "—"));
            foreach (var row in summary.Rows)
            {
                Rows.Add(new DetailRowViewModel(row, stockType));
            }

            // Dağılım, tür kalıbındaki sırayla: sıralı durumlar, sıra dışılar, sonra durumu olmayanlar.
            var order = stockType.Statuses.Concat(stockType.SideStatuses).Select(s => s.Name).ToList();
            foreach (var group in Rows.GroupBy(r => r.Status.Name).OrderBy(g => order.IndexOf(g.Key) < 0 ? int.MaxValue : order.IndexOf(g.Key)))
            {
                var name = group.Key == "—" ? Resources.Status_None : group.Key;
                Distribution.Add(new StatusChip(group.Count() + " " + name, group.First().Status.Tone));
            }

            Note = string.Format(Resources.Detail_General, summary.General.FileCount);
            IsProject = true;
        }

        private void ShowStock(FolderSummary summary, string path)
        {
            var stockType = types.Stock;
            var info = summary.Stock;
            var number = info?.StockNo ?? Path.GetFileName(path);
            var format = stockType.MatchNumber(number);
            var project = Path.GetFileName(Path.GetDirectoryName(path) ?? string.Empty);
            var parts = new[] { format?.Label ?? Resources.Kind_Stock, summary.Template, project }.Where(p => !string.IsNullOrEmpty(p));
            Subtitle = string.Join(" · ", parts);
            HeaderName = info?.Name ?? string.Empty;
            ParentName = project;
            ParentPath = Path.GetDirectoryName(path) ?? string.Empty;
            ContextText = string.Join(" · ", new[] { format?.Label ?? Resources.Kind_Stock, summary.Template }.Where(p => !string.IsNullOrEmpty(p)));

            var current = info?.Status ?? string.Empty;
            Status = StatusChip.For(stockType, current);
            foreach (var step in StatusStep.Build(stockType, current))
            {
                Steps.Add(step);
            }

            var since = info?.StatusSinceDate;
            var infoParts = new[]
            {
                !since.HasValue ? string.Empty : since.Value.Date == DateTime.Today ? Resources.Status_SinceToday : string.Format(Resources.Status_Since, Formats.ShortDate(since.Value)),
                info?.StatusNote ?? string.Empty,
            };
            StatusInfo = string.Join(" · ", infoParts.Where(p => p.Length > 0));

            AddTotalTiles(summary.Total);
            Tiles.Add(new SummaryTile(Resources.Tile_LastFile, summary.Total.LastWrite.HasValue ? Path.GetFileName(summary.Total.LastFile) : "—"));
            foreach (var row in summary.Rows)
            {
                Rows.Add(new DetailRowViewModel(row, stockType));
            }

            Note = Resources.Detail_RowHint;
            IsStock = true;
            OnPropertyChanged(nameof(StockPath));
        }

        private void AddTotalTiles(FolderStats total)
        {
            Tiles.Add(new SummaryTile(Resources.Tile_Files, total.FileCount.ToString()));
            Tiles.Add(new SummaryTile(Resources.Tile_Size, total.FileCount == 0 ? "—" : Formats.Size(total.TotalBytes)));
        }
    }

    /// <summary>Boyut ve tarih biçimleri.</summary>
    public static class Formats
    {
        /// <summary>Bayt → "1,8 GB" gibi.</summary>
        public static string Size(long bytes)
        {
            const double Kb = 1024;
            if (bytes < Kb)
            {
                return string.Format(Resources.Size_Bytes, bytes);
            }

            if (bytes < Kb * Kb)
            {
                return string.Format(Resources.Size_KB, bytes / Kb);
            }

            if (bytes < Kb * Kb * Kb)
            {
                return string.Format(Resources.Size_MB, bytes / (Kb * Kb));
            }

            return string.Format(Resources.Size_GB, bytes / (Kb * Kb * Kb));
        }

        /// <summary>"08.11.2026 14:20".</summary>
        public static string Date(DateTime value)
        {
            return value.ToString(Resources.Format_DateTime);
        }

        /// <summary>Bugünse "bugün 14:20" (saat yoksa "bugün"), değilse "08.11.2026".</summary>
        public static string ShortDate(DateTime value)
        {
            if (value.Date == DateTime.Today)
            {
                return value.TimeOfDay == TimeSpan.Zero
                    ? Resources.Format_TodayOnly
                    : string.Format(Resources.Format_Today, value.ToString("HH:mm"));
            }

            return value.ToString(Resources.Format_Date);
        }
    }
}
