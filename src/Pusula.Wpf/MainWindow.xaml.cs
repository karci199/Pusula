using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DesignKit.Controls;
using Pusula.Core.Testing;
using Pusula.Core.Types;
using Pusula.Wpf.ViewModels;
using Pusula.Wpf.ViewModels.TemplateEditor;

namespace Pusula.Wpf
{
    /// <summary>
    /// Ana pencere: solda klasör ağacı, sağda seçilen düğümün paneli.
    /// </summary>
    public partial class MainWindow : ChromeWindow
    {
        private readonly MainViewModel viewModel = new MainViewModel();

        /// <summary>
        /// Ana pencereyi oluşturur.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.NoteEditorOpener = OpenNoteWindow;
            MentionAssist.Provider = viewModel.NoteSuggestions;
            MentionAssist.TagProvider = viewModel.TagSuggestions;
            viewModel.ActivateMain = () =>
            {
                if (WindowState == WindowState.Minimized)
                {
                    WindowState = WindowState.Normal;
                }

                Activate();
            };

            Closed += (s, e) => viewModel.SaveRecentNow();

            // İlk açılışta kök klasör seçilmemişse Ayarlar açılır (kök seçimi artık orada).
            Loaded += (s, e) => HookTitleBarLogo();
            ContentRendered += (s, e) =>
            {
                if (App.Started && !viewModel.HasRoot)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => Settings_Click(this, new RoutedEventArgs())));
                }
            };

            // ContextMenu görsel ağacın dışında durur; DataContext'i elle verilir.
            foreach (var key in new[] { "AddMenu", "NodeMenu", "TreeMenu" })
            {
                ((ContextMenu)FindResource(key)).DataContext = viewModel;
            }

            SetUpLayout();
            SetUpRecordTables();

            // Tür düğümünde (hepsi aynı tür) envanter listesinin Tür sütunu gizlenir.
            viewModel.Inventory.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(InventoryDetailViewModel.TypeColumnWidth))
                {
                    InventoryTypeColumn.Width = viewModel.Inventory.TypeColumnWidth;
                }
            };
        }

        private void FolderTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // İki ağaç var (projeler, envanter); birinde seçim kalkınca (boş) seçim silinmez.
            if (e.NewValue is TreeNodeViewModel node)
            {
                viewModel.SelectedNode = node;
            }
        }

        // Envanter projelerin hemen altında durur; projeler çoksa üst ağaç bölmenin üçte ikisinde kayar.
        private void TreePane_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FolderTree.MaxHeight = Math.Max(80, TreePane.ActualHeight * 2 / 3);
        }

        // Sağ tık, menü açılmadan önce tıklanan düğümü seçer.
        private void FolderTree_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;
            while (source != null && !(source is TreeViewItem))
            {
                source = VisualTreeHelper.GetParent(source);
            }

            if (source is TreeViewItem item)
            {
                item.IsSelected = true;
                item.Focus();
            }
        }

        // Diyalog açılınca ilk alana odaklanır.
        private void FocusWhenVisible(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is TextBox box && box.IsVisible)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => Keyboard.Focus(box)));
            }
        }

        // ContentDialog birincil düğmede kendiliğinden kapanır; hata varsa hata metniyle yeniden açılır.
        private void ProjectDialog_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            if (!viewModel.ConfirmNewProject())
            {
                Dispatcher.BeginInvoke(new Action(() => viewModel.IsProjectDialogOpen = true));
            }
        }

        // Doğrulama maddesi: hata varsa ya da "yenisini aç" işaretliyse pencere hemen yeniden açılır (metin kutusu odakta).
        private void VerificationDialog_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            if (!viewModel.ConfirmNewVerification())
            {
                Dispatcher.BeginInvoke(new Action(() => viewModel.IsVerificationDialogOpen = true));
            }
        }

        private void VerPlan_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            if (!viewModel.ConfirmVerPlan())
            {
                Dispatcher.BeginInvoke(new Action(() => viewModel.IsVerPlanOpen = true));
            }
        }

        private void VerReport_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            if (!viewModel.ConfirmVerReport())
            {
                Dispatcher.BeginInvoke(new Action(() => viewModel.IsVerReportOpen = true));
            }
        }

        // Satırdaki Test raporu kartı: Rapor ekle / Düzenle.
        private void VerReportOpen_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is VerificationRow row)
            {
                viewModel.OpenVerReport(row);
            }
        }

        // Açık satırdaki Maddeyi düzenle.
        private void VerEditRow_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is VerificationRow row)
            {
                viewModel.EditVerification(row);
            }
        }

        // Satırdaki Test planı kartı: Test planı ekle / Düzenle.
        private void VerPlanOpen_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is VerificationRow row)
            {
                viewModel.OpenVerPlan(row);
            }
        }

        private void VerificationDelete_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            viewModel.ConfirmDeleteVerification();
        }

        // Plan sekmesi: "+ Plan maddesi".
        private void VerPlanNew_Click(object sender, RoutedEventArgs e)
        {
            viewModel.NewPlanItem();
        }

        // Rapor sekmesi: "+ Rapor maddesi".
        private void VerReportNew_Click(object sender, RoutedEventArgs e)
        {
            viewModel.NewReportItem();
        }

        // Plan satırı ya da kartındaki kalem: maddeyi düzenle.
        private void VerPlanEdit_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is PlanItem plan)
            {
                viewModel.EditPlanItem(plan);
            }
        }

        // Rapor satırı ya da kartındaki kalem: maddeyi düzenle.
        private void VerReportEdit_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is ReportItem report)
            {
                viewModel.EditReportItem(report);
            }
        }

        // Plan satırına çift tık: düzenle.
        private void VerPlanRow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && (sender as FrameworkElement)?.Tag is PlanItem plan)
            {
                viewModel.EditPlanItem(plan);
                e.Handled = true;
            }
        }

        // Rapor satırına çift tık: düzenle.
        private void VerReportRow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && (sender as FrameworkElement)?.Tag is ReportItem report)
            {
                viewModel.EditReportItem(report);
                e.Handled = true;
            }
        }

        // Rapor kartındaki sonucun yeri: Aç.
        private void VerReportPathOpen_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is ReportItem report)
            {
                viewModel.OpenReportPath(report);
            }
        }

        // Plan maddesi penceresindeki Sil: onay sorulur.
        private void VerPlanDelete_Click(object sender, RoutedEventArgs e)
        {
            viewModel.RequestDeletePlanItem();
        }

        // Rapor maddesi penceresindeki Sil: onay sorulur.
        private void VerReportDelete_Click(object sender, RoutedEventArgs e)
        {
            viewModel.RequestDeleteReportItem();
        }

        private void VerLinkDelete_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            viewModel.ConfirmDeleteVerLink();
        }

        // Kapsama uyarısının bir parçası: o maddeleri listeleyen süzgeç.
        private void CoveragePart_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is CoveragePart part)
            {
                viewModel.ApplyCoveragePart(part);
            }
        }

        // Bağ süzgecini (ve aramayı) temizle.
        private void VerLinkClear_Click(object sender, RoutedEventArgs e)
        {
            viewModel.ClearLinkFilter();
        }

        // Doğrulama satırı: tek tık açar / kapar (tam gereksinim, plan / rapor özeti), çift tık düzenleme penceresi.
        private void VerificationRow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!((sender as FrameworkElement)?.DataContext is VerificationRow row))
            {
                return;
            }

            e.Handled = true;
            viewModel.ToggleVerification(row);
            if (e.ClickCount == 2)
            {
                // İlk tık açmıştı; çift tıkta satır eski hâline döner, düzenleme açılır.
                viewModel.EditVerification(row);
            }
        }

        // Doğrulama tablosu: başlıktaki ayırıcı sürüklenince sütun genişliği değişir. TG no ayırıcısı sağ kenarda (sağa çekince
        // genişler); diğerleri sütunun sol kenarında (sola çekince genişler, gereksinim sütunu daralır).
        private void VerificationColumn_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is string key)
            {
                viewModel.VerificationColumns.Resize(key, VerificationColumns.IsLeading(key) ? e.HorizontalChange : -e.HorizontalChange);
            }
        }

        private void VerificationColumn_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            viewModel.SaveVerificationColumns();
        }

        // Ayırıcıya çift tık: bütün sütunlar varsayılan genişliğe döner.
        private void VerificationColumn_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            viewModel.VerificationColumns.Reset();
            viewModel.SaveVerificationColumns();
        }
        // Zaman çizelgesi: "+ Kayıt" penceresi (yapıldı ya da yapılacak).
        // "+ İş": gösterilen kaydın notuna yapılacak iş ekleme penceresi.
        private void TaskDialogOpen_Click(object sender, RoutedEventArgs e)
        {
            viewModel.OpenTaskDialogHere();
        }

        // Ana sayfa "+ İş": önce stok seçilir.
        private void HomeTaskDialogOpen_Click(object sender, RoutedEventArgs e)
        {
            viewModel.OpenTaskDialogHome();
        }

        private void TaskDialog_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            if (!viewModel.ConfirmTaskDialog())
            {
                Dispatcher.BeginInvoke(new Action(() => viewModel.IsTaskDialogOpen = true));
            }
        }
        private void TimelineAdd_Click(object sender, RoutedEventArgs e)
        {
            viewModel.OpenJournalHere();
        }

        // Karta çift tık: günlük olayı düzenleme penceresinde açılır; diğer kayıtlar (koşu, not) kendi notunda.
        private void TimelineCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!((sender as FrameworkElement)?.DataContext is StockTimelineCard card))
            {
                return;
            }

            // Tek tık: açıklama tam açılır / kapanır; çift tık: kayıt düzenlenir ya da açılır.
            if (e.ClickCount == 1)
            {
                if (card.HasDescription)
                {
                    card.IsExpanded = !card.IsExpanded;
                }

                return;
            }

            if (e.ClickCount != 2)
            {
                return;
            }

            e.Handled = true;
            if (card.SourcePath.Length > 0)
            {
                // Ana sayfa Günlük: kaydın stoku açılır.
                viewModel.OpenRecordPath(card.SourcePath);
            }
            else if (card.Item?.Journal != null)
            {
                viewModel.OpenJournalEdit(card.Item.Journal);
            }
            else if (card.Item != null)
            {
                viewModel.OpenNoteOrFolder(card.Item.NotePath, card.Item.Folder);
            }
            else if (card.Task != null)
            {
                viewModel.OpenNoteOrFolder(card.Task.NotePath, System.IO.Path.GetDirectoryName(card.Task.NotePath) ?? string.Empty);
            }
        }

        // Durum çubuğunda son iş: yazdığı dosya açılır.
        private void LastAction_Click(object sender, RoutedEventArgs e)
        {
            viewModel.OpenLastAction();
        }

        // Kartın "Notta aç" düğmesi: kaydın (ya da işin) yazıldığı not, ayarlardaki not uygulamasında açılır.
        private void TimelineOpenNote_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is StockTimelineCard card)
            {
                var path = card.Item?.NotePath ?? card.Task?.NotePath ?? string.Empty;
                viewModel.OpenInPusula(path);
            }
        }

        // Doğrulama satırının MD düğmesi: test gereksinimleri dosyası Pusula'nın not düzenleyicisinde açılır.
        private void VerificationOpenNote_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            viewModel.OpenVerificationFileCommand.Execute(null);
        }

        // Kart: ölçüm klasörünü aç; sonradan dosya / klasör ekle (menü ya da sürükle-bırak).
        private void TimelineOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (((FrameworkElement)sender).DataContext is StockTimelineCard card)
            {
                viewModel.OpenPath(card.FolderPath);
            }
        }

        // Toplantı (Outlook): mail dosyası seç, sürükle-bırak, kaldır; kartta Outlook'ta aç.
        private void JournalMailPick_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Title = Properties.Resources.Journal_MailPickTitle, Filter = "Outlook (*.msg)|*.msg" };
            if (dialog.ShowDialog(this) == true)
            {
                viewModel.SetJournalMail(dialog.FileName);
            }
        }

        private void JournalMail_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void JournalMail_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] paths && paths.Length > 0)
            {
                viewModel.SetJournalMail(paths[0]);
            }

            e.Handled = true;
        }

        private void JournalMailClear_Click(object sender, RoutedEventArgs e)
        {
            viewModel.ClearJournalMail();
        }

        private void TimelineOpenMail_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (((FrameworkElement)sender).DataContext is StockTimelineCard card && card.Item?.Journal != null)
            {
                viewModel.OpenMail(card.Item.Journal);
            }
        }

        // Talep kartında "Teslim alındı": talep kapanır, çizelgeye teslim kaydı düşer.
        private void TimelineDeliver_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (((FrameworkElement)sender).DataContext is StockTimelineCard card && card.Item?.Journal != null)
            {
                viewModel.MarkRequestDelivered(card.Item.Journal);
            }
        }

        // Açık sorunun kartından "✓ Çözüm ekle": kayıt penceresi Çözüm ve bu sorun seçili açılır.
        private void TimelineAddSolution_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (((FrameworkElement)sender).DataContext is StockTimelineCard card && card.Item?.Journal != null)
            {
                // Ana sayfada kayıt sorunun stokuna yazılır.
                viewModel.OpenSolutionFor(card.Item.Journal, card.SourcePath);
            }
        }

        // Malzeme talebi: kalem satırı ekle / çıkar.
        private void RequestItemAdd_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AddRequestItemRow();
        }

        private void RequestItemRemove_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is RequestItemRow row)
            {
                viewModel.RemoveRequestItemRow(row);
            }
        }

        private void AttemptFolder_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (((FrameworkElement)sender).DataContext is VerificationAttempt attempt)
            {
                viewModel.OpenPath(attempt.FolderPath);
            }
        }

        private void TimelineCard_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var element = (FrameworkElement)sender;
            if (!(element.DataContext is StockTimelineCard card) || card.Item?.Journal == null || element.ContextMenu == null)
            {
                e.Handled = true;
                return;
            }

            var note = card.Item.Journal;
            var menu = element.ContextMenu;
            menu.Items.Clear();
            var edit = new MenuItem { Header = Properties.Resources.Timeline_Edit };
            edit.Click += (s, a) => viewModel.OpenJournalEdit(note);
            var folder = new MenuItem { Header = card.HasFolder ? Properties.Resources.Timeline_OpenFolder : Properties.Resources.Timeline_CreateFolder };
            folder.Click += (s, a) => viewModel.CreateEntryFolder(note);
            menu.Items.Add(edit);
            menu.Items.Add(folder);
        }

        // Doğrulama satırında "+ Deneme": kayıt penceresi Ölçüm ve bu madde seçili açılır.
        private void VerificationAttempt_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (((FrameworkElement)sender).DataContext is VerificationRow row)
            {
                viewModel.OpenMeasurementFor(row);
            }
        }
        // Yapılacak kartın kutusu: iş kapatılmadan önce sorulur (Yapılacaklar sekmesiyle aynı).
        private void TimelineTask_Click(object sender, RoutedEventArgs e)
        {
            var box = (CheckBox)sender;
            box.IsChecked = false;
            if (box.DataContext is StockTimelineCard card && card.Task != null)
            {
                viewModel.RequestTaskDone(card.Task, true);
            }
        }
        // Durum etiketine basmak satırı açıp kapamaz (menüsü açılır).
        private void VerificationStatus_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        // Satırın sağ tık menüsü: Düzenle · Sil.
        private void VerificationRow_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var element = (FrameworkElement)sender;
            if (element.DataContext is VerificationRow row && element.ContextMenu != null)
            {
                FillVerificationRowMenu(element.ContextMenu, row);
            }
        }

        // Satır sonundaki ⋯ düğmesi: aynı menü düğmenin altında açılır.
        private void VerificationRowMenu_Click(object sender, RoutedEventArgs e)
        {
            var button = (FrameworkElement)sender;
            if (button.DataContext is VerificationRow row)
            {
                var menu = new ContextMenu { PlacementTarget = button, Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom };
                FillVerificationRowMenu(menu, row);
                menu.IsOpen = true;
            }
        }

        private void FillVerificationRowMenu(ContextMenu menu, VerificationRow row)
        {
            menu.Items.Clear();
            var open = new MenuItem { Header = Properties.Resources.Verify_OpenInFile, Icon = new AppIcon { Glyph = AppIcons.Note } };
            open.Click += (s, a) => viewModel.OpenVerificationFileCommand.Execute(null);
            menu.Items.Add(open);
            var edit = new MenuItem { Header = Properties.Resources.Verify_Edit, Icon = new AppIcon { Glyph = AppIcons.Edit } };
            edit.Click += (s, a) => viewModel.EditVerification(row);
            var delete = new MenuItem { Header = Properties.Resources.Verify_Delete, Icon = new AppIcon { Glyph = AppIcons.Delete } };
            delete.Click += (s, a) => viewModel.RequestDeleteVerification(row);
            var plan = new MenuItem { Header = Properties.Resources.Verify_PlanMenu, Icon = new AppIcon { Glyph = AppIcons.Edit } };
            plan.Click += (s, a) => viewModel.OpenVerPlan(row);
            menu.Items.Add(edit);
            menu.Items.Add(plan);
            var report = new MenuItem { Header = Properties.Resources.Verify_ReportMenu, Icon = new AppIcon { Glyph = AppIcons.Check } };
            report.Click += (s, a) => viewModel.OpenVerReport(row);
            menu.Items.Add(report);
            menu.Items.Add(delete);
        }

        // Durum etiketine basınca durum menüsü: seçilen durum yalnız o satırın durum hücresine yazılır.
        private void VerificationStatus_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var pill = (FrameworkElement)sender;
            if (!(pill.DataContext is VerificationRow row))
            {
                return;
            }

            e.Handled = true;
            var menu = new ContextMenu { PlacementTarget = pill, Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom };
            foreach (var status in viewModel.VerificationStatuses)
            {
                var value = status;
                var item = new MenuItem
                {
                    Header = value,
                    IsCheckable = false,
                    IsChecked = string.Equals(value, row.StatusText, StringComparison.CurrentCultureIgnoreCase),
                };
                item.Click += (s, a) => viewModel.SetVerificationStatus(row, value);
                menu.Items.Add(item);
            }

            menu.IsOpen = true;
        }

        // Durum menüsü her açılışta tür kalıbından kurulur: sıralı durumlar, ayraç, sıra dışı durumlar.
        private void StatusMenu_Opened(object sender, RoutedEventArgs e)
        {
            var menu = (ContextMenu)sender;
            menu.Items.Clear();
            var type = viewModel.Types.Stock;
            var current = viewModel.Detail.Status.Name;

            foreach (var status in type.Statuses)
            {
                menu.Items.Add(CreateStatusItem(status, current));
            }

            if (type.SideStatuses.Count > 0)
            {
                menu.Items.Add(new Separator());
                foreach (var status in type.SideStatuses)
                {
                    menu.Items.Add(CreateStatusItem(status, current));
                }
            }
        }

        private MenuItem CreateStatusItem(Core.Types.StatusDefinition status, string current)
        {
            var item = new MenuItem
            {
                Header = status.NeedsNote ? status.Name + "…" : status.Name,
            };
            if (string.Equals(status.Name, current, StringComparison.OrdinalIgnoreCase))
            {
                item.Icon = new AppIcon { Glyph = AppIcons.Check };
            }

            item.Click += (s, args) =>
            {
                viewModel.NoteMenuChoice();
                if (status.NeedsNote)
                {
                    viewModel.AskStatusNote(status.Name);
                }
                else
                {
                    viewModel.SetStockStatus(status.Name, string.Empty);
                }
            };
            return item;
        }

        private void MarkDialog_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            if (!viewModel.ConfirmMark())
            {
                Dispatcher.BeginInvoke(new Action(() => viewModel.IsMarkDialogOpen = true));
            }
        }

        private void StatusNote_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            viewModel.ConfirmStatusNote();
        }

        // Testler / Kalifikasyon: sütun başlığına tıklayınca o sütuna göre sıralar (ikinci tık ters çevirir).
        private void RunsHeader_Click(object sender, RoutedEventArgs e)
        {
            var header = e.OriginalSource as GridViewColumnHeader;
            var property = header?.Tag as string;
            var list = sender as ListView;
            if (property == null || list?.ItemsSource == null)
            {
                return;
            }

            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(list.ItemsSource);
            var direction = System.ComponentModel.ListSortDirection.Ascending;
            if (view.SortDescriptions.Count > 0 && view.SortDescriptions[0].PropertyName == property
                && view.SortDescriptions[0].Direction == System.ComponentModel.ListSortDirection.Ascending)
            {
                direction = System.ComponentModel.ListSortDirection.Descending;
            }

            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new System.ComponentModel.SortDescription(property, direction));
        }

        // Koşuya çift tıklayınca notu (test.md / _kosu.md) varsayılan programla açar; not yoksa klasörü açar.
        private void RunsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || viewModel.IsDialogBusy)
            {
                return;
            }

            var source = e.OriginalSource as DependencyObject;
            while (source != null && !(source is ListViewItem))
            {
                source = VisualTreeHelper.GetParent(source);
            }

            if ((source as ListViewItem)?.DataContext is TestRunRow run)
            {
                viewModel.OpenNoteOrFolder(run.NotePath, run.Folder);
            }
        }

        // Zaman çizelgesi kartına tıklayınca ayrıntı açılır / kapanır (karttaki düğmeler hariç).
        private void TimelineCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;
            while (source != null && source != sender)
            {
                if (source is ButtonBase)
                {
                    return;
                }

                source = VisualTreeHelper.GetParent(source);
            }

            if ((sender as FrameworkElement)?.DataContext is TimelineItem item && item.HasDetail)
            {
                item.IsExpanded = !item.IsExpanded;
            }
        }

        private void TimelineNote_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is TimelineItem item)
            {
                viewModel.OpenNoteOrFolder(item.NotePath, item.Folder);
            }
        }

        private void TimelineFolder_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is TimelineItem item)
            {
                viewModel.OpenPath(item.Folder);
            }
        }

        // Testler / Kalifikasyon sağ tık menüsü: seçili koşu üzerinde.
        private static TestRunRow? SelectedRun(object sender)
        {
            var menu = (sender as MenuItem)?.Parent as ContextMenu;
            return (menu?.PlacementTarget as ListView)?.SelectedItem as TestRunRow;
        }

        private readonly Dictionary<string, NoteWindow> noteWindows = new Dictionary<string, NoteWindow>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Notu Pusula'nın md penceresinde açar; aynı not zaten açıksa o pencere öne gelir. Kapanınca, not kaydedildiyse
        /// ana pencere yenilenir (not özeti, bahisler).
        /// </summary>
        public bool OpenNoteWindow(string path)
        {
            NoteWindow existing;
            if (noteWindows.TryGetValue(path, out existing))
            {
                if (existing.WindowState == WindowState.Minimized)
                {
                    existing.WindowState = WindowState.Normal;
                }

                existing.Activate();
                return true;
            }

            var editor = viewModel.CreateNoteEditor(path);
            var window = new NoteWindow(editor, viewModel.NavigateToKey, viewModel.OpenNoteExternally, viewModel.OpenPath);
            editor.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(NoteEditorViewModel.SaveCount) && editor.SaveCount > 0)
                {
                    viewModel.NoteSaved(path);
                }
            };
            window.Closed += (s, e) =>
            {
                noteWindows.Remove(path);
                if (editor.SaveCount > 0)
                {
                    viewModel.Refresh();
                }
            };
            noteWindows[path] = window;
            window.Show();
            return true;
        }

        /// <summary>Açık not pencereleri (testler için).</summary>
        public IEnumerable<NoteWindow> NoteWindows
        {
            get { return noteWindows.Values; }
        }


        private void Mention_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is MentionRow mention)
            {
                viewModel.OpenNoteOrFolder(mention.Path, mention.Folder);
            }
        }

        private void RunMenuAnalysis_Click(object sender, RoutedEventArgs e)
        {
            var run = SelectedRun(sender);
            if (run != null)
            {
                viewModel.OpenAnalysis(run);
            }
        }

        private void AnalysisDialog_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            if (!viewModel.ConfirmAnalysis())
            {
                Dispatcher.BeginInvoke(new Action(() => viewModel.IsAnalysisOpen = true));
            }
        }

        private void RunMenuNote_Click(object sender, RoutedEventArgs e)
        {
            var run = SelectedRun(sender);
            if (run != null)
            {
                viewModel.OpenNoteOrFolder(run.NotePath, run.Folder);
            }
        }

        private void RunMenuFolder_Click(object sender, RoutedEventArgs e)
        {
            var run = SelectedRun(sender);
            if (run != null)
            {
                viewModel.OpenPath(run.Folder);
            }
        }

        private void RunMenuRaw_Click(object sender, RoutedEventArgs e)
        {
            var run = SelectedRun(sender);
            if (run != null)
            {
                viewModel.OpenSubFolderOrFolder(run.Folder, "Ham");
            }
        }

        private void RunMenuPhotos_Click(object sender, RoutedEventArgs e)
        {
            var run = SelectedRun(sender);
            if (run != null)
            {
                viewModel.OpenSubFolderOrFolder(run.Folder, "Foto");
            }
        }

        // Stok Genel kapağı: not, fotoğraf klasörü, tek fotoğraf.
        private void StockNote_Click(object sender, RoutedEventArgs e)
        {
            var cover = viewModel.Detail.Cover;
            viewModel.OpenNoteOrFolder(cover.NoteFile, cover.StockFolder);
        }

        // Başlık çubuğundaki Pusula simgesi (ChromeWindow şablonundaki ilk Image): tıklayınca ana sayfa.
        // Başlık çubuğu sürükleme alanı olduğu için simge tıklamaya açılır (IsHitTestVisibleInChrome).
        private void HookTitleBarLogo()
        {
            var pending = new Stack<DependencyObject>();
            pending.Push(this);
            while (pending.Count > 0)
            {
                var node = pending.Pop();
                if (node is Image logo && ReferenceEquals(logo.TemplatedParent, this))
                {
                    logo.Cursor = Cursors.Hand;
                    System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(logo, true);
                    logo.MouseLeftButtonUp += (s, e) => viewModel.GoHome();
                    return;
                }

                for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
                {
                    pending.Push(VisualTreeHelper.GetChild(node, i));
                }
            }
        }

        // Sol üstte "Ana sayfa": seçimi kaldırıp proje kartlarını gösterir.
        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel.GoHome();
        }

        // Stok sayfası: TEÜ görünümündeki büyük fotoğrafa tıklayınca tam boy açılır.
        private void StockCover_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var cover = viewModel.Detail.Cover;
            if (cover.SelectedPhoto.Length > 0)
            {
                viewModel.OpenNoteOrFolder(cover.SelectedPhoto, cover.HasPhotosFolder ? cover.PhotosFolder : cover.StockFolder);
            }
        }

        // Stok sayfası · Test yardımcı araçları: satıra tıklayınca kayıt açılır.
        private void StockToolRow_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is ToolRow row)
            {
                viewModel.OpenRecordPath(row.Path);
            }
        }

        // Kayıtlar: satıra tıklayınca kayıt (proje, stok, araç) ağaçta seçilir.
        private void RecordRow_Click(object sender, MouseButtonEventArgs e)
        {
            var context = (sender as FrameworkElement)?.DataContext;
            var path = (context as ProjectRow)?.Path ?? (context as StockRow)?.Path ?? (context as ToolRow)?.Path;
            if (path != null)
            {
                viewModel.OpenRecordPath(path);
            }
        }

        // Kayıtlar tablolarında seçim tutulmaz: satıra tıklamak kaydı açar. Fareye basıp dışarı çekince
        // kalan seçim, üzerine gelinen satırla aynı renkte görünüp iki satır seçili gibi duruyordu.
        private void RecordList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView list && list.SelectedIndex >= 0)
            {
                list.SelectedIndex = -1;
            }
        }

        // Kayıtlar: sütun başlığına tıklayınca o sütuna göre sıralanır; ikinci tıklama yönü çevirir.
        private void RecordHeader_Click(object sender, RoutedEventArgs e)
        {
            var header = e.OriginalSource as GridViewColumnHeader;
            var key = header?.Tag as string;
            if (string.IsNullOrEmpty(key) || !(sender is ListView list))
            {
                return;
            }

            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(list.ItemsSource);
            var current = view.SortDescriptions.FirstOrDefault();
            var direction = current.PropertyName == key && current.Direction == System.ComponentModel.ListSortDirection.Ascending
                ? System.ComponentModel.ListSortDirection.Descending
                : System.ComponentModel.ListSortDirection.Ascending;
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new System.ComponentModel.SortDescription(key, direction));
        }

        private void ScopeProjectsAll_Click(object sender, RoutedEventArgs e)
        {
            viewModel.SelectAllProjects(true);
        }

        private void ScopeProjectsClear_Click(object sender, RoutedEventArgs e)
        {
            viewModel.SelectAllProjects(false);
        }

        private void ScopeStocksAll_Click(object sender, RoutedEventArgs e)
        {
            viewModel.SelectAllStocks(true);
        }

        private void ScopeStocksClear_Click(object sender, RoutedEventArgs e)
        {
            viewModel.SelectAllStocks(false);
        }

        // Ana sayfada proje kartı: projeyi ağaçta seçer.
        private void ProjectCard_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is ProjectCard card)
            {
                viewModel.OpenProjectCard(card);
            }
        }

        // Başlıktaki "Birim bilgisi": stokun ana md'si varsa açılır, yoksa oluşturulup açılır.
        private void HeaderUnitInfo_Click(object sender, RoutedEventArgs e)
        {
            var cover = viewModel.Detail.Cover;
            if (cover.HasNote)
            {
                viewModel.OpenNoteOrFolder(cover.NoteFile, cover.StockFolder);
            }
            else
            {
                viewModel.CreateStockNote();
            }
        }

        private void StockCreateNote_Click(object sender, RoutedEventArgs e)
        {
            viewModel.CreateStockNote();
        }

        // TEÜ görünümünde 📁: 01_Ortak\Fotograflar açılır (yoksa boş olarak oluşturulur).
        private void StockPhotos_Click(object sender, RoutedEventArgs e)
        {
            viewModel.OpenStockPhotosFolder();
        }

        // Şeritteki küçük resim: büyükte gösterilir; "+N" klasörü açar.
        private void PhotoThumb_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is PhotoThumb thumb)
            {
                e.Handled = true;
                if (thumb.IsMore)
                {
                    viewModel.OpenStockPhotosFolder();
                }
                else
                {
                    viewModel.Detail.Cover.Select(thumb);
                }
            }
        }

        private void StockDocuments_Click(object sender, RoutedEventArgs e)
        {
            var cover = viewModel.Detail.Cover;
            viewModel.OpenPath(cover.HasDocumentsFolder ? cover.DocumentsFolder : cover.StockFolder);
        }

        private void EditTemplates_Click(object sender, RoutedEventArgs e)
        {
            EditTemplates(this);
        }

        private void EditTypes_Click(object sender, RoutedEventArgs e)
        {
            EditTypes(this);
        }
        // Ayarlar düğmesi: kök klasör, tema, tanımlar (şablon, tür, dosya türü).
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow(this, viewModel) { Owner = this }.ShowDialog();
        }

        /// <summary>Dosya türleri penceresini açar.</summary>
        internal void EditFileTypes(Window owner)
        {
            if (!viewModel.CanEditTemplates())
            {
                return;
            }

            var editor = new FileTypesWindow(new ViewModels.TypeEditor.FileTypesViewModel(viewModel.RootPath, viewModel.Types))
            {
                Owner = owner,
            };
            if (editor.ShowDialog() == true)
            {
                viewModel.ReloadTypes();
            }
        }

        /// <summary>Türler penceresini açar.</summary>
        internal void EditTypes(Window owner)
        {
            if (!viewModel.CanEditTemplates())
            {
                return;
            }

            var editor = new TypeEditorWindow(new ViewModels.TypeEditor.TypeEditorViewModel(viewModel.RootPath, viewModel.Types))
            {
                Owner = owner,
            };
            if (editor.ShowDialog() == true)
            {
                viewModel.ReloadTypes();
            }
        }

        // Stok sayfası, Jig ve kablo: "Ekle" düğmesi genel / proje kayıtlarını menüde listeler, seçilen stoka bağlanır.
        private void StockInvAdd_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (!(button?.Tag is TypeDefinition type))
            {
                return;
            }

            var menu = new ContextMenu { PlacementTarget = button, Placement = PlacementMode.Bottom };
            var candidates = viewModel.LinkCandidates(type);
            foreach (var candidate in candidates)
            {
                var number = candidate.Record.Number;
                var item = new MenuItem
                {
                    Header = number + " · " + candidate.Record.Name + "  (" + InventoryDetailViewModel.LevelText(candidate) + ")",
                };
                item.Click += (s, args) => viewModel.SetStockLink(number, true);
                menu.Items.Add(item);
            }

            if (candidates.Count == 0)
            {
                menu.Items.Add(new MenuItem { Header = Properties.Resources.StockInv_NoCandidates, IsEnabled = false });
            }

            menu.IsOpen = true;
        }

        private void StockInvNew_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is TypeDefinition type)
            {
                viewModel.OpenNewInventory(type);
            }
        }

        private static InventoryRow? MenuRow(object sender)
        {
            var menu = (sender as MenuItem)?.Parent as ContextMenu;
            return (menu?.PlacementTarget as ListView)?.SelectedItem as InventoryRow;
        }

        private void StockInvOpen_Click(object sender, RoutedEventArgs e)
        {
            var row = MenuRow(sender);
            if (row != null && row.Folder.Length > 0)
            {
                viewModel.ShowInventory(row.Folder);
            }
        }

        private void StockInvFolder_Click(object sender, RoutedEventArgs e)
        {
            var row = MenuRow(sender);
            if (row != null && row.Folder.Length > 0)
            {
                viewModel.OpenPath(row.Folder);
            }
        }

        private void StockInvNote_Click(object sender, RoutedEventArgs e)
        {
            var row = MenuRow(sender);
            if (row != null && row.Folder.Length > 0)
            {
                viewModel.OpenQuickNote(QuickNoteTarget.Record, row.Folder, row.Number);
            }
        }

        private void StockInvUnlink_Click(object sender, RoutedEventArgs e)
        {
            var row = MenuRow(sender);
            if (row != null && row.IsLinked)
            {
                viewModel.SetStockLink(row.Number, false);
            }
        }

        // Kayıt sayfası: klasör kutucuğu klasörü, fotoğraf resmi açar.
        private void RecordFolder_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is string path)
            {
                viewModel.OpenPath(path);
            }
        }

        private void RecordPhoto_Click(object sender, RoutedEventArgs e)
        {
            var shown = viewModel.Inventory.Item;
            if ((sender as FrameworkElement)?.Tag is string path && shown != null)
            {
                viewModel.OpenNoteOrFolder(path, shown.Folder);
            }
        }

        private void RecordNote_Click(object sender, RoutedEventArgs e)
        {
            var shown = viewModel.Inventory.Item;
            if (shown != null)
            {
                viewModel.OpenNoteOrFolder(viewModel.Inventory.NoteFile, shown.Folder);
            }
        }

        private async void RecordCreateNote_Click(object sender, RoutedEventArgs e)
        {
            await viewModel.CreateInventoryNoteAsync().ConfigureAwait(true);
        }

        // Klasör ağacında çift tıklayınca klasörü açar.
        private void FolderTreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && !viewModel.IsDialogBusy && (sender as TreeView)?.SelectedItem is FolderTreeItem item)
            {
                viewModel.OpenPath(item.Path);
                e.Handled = true;
            }
        }

        // "+" menüsü: envanter türleri (Yeni kablo, Yeni jig …) her açılışta tür kalıbından eklenir.
        private void AddMenu_Opened(object sender, RoutedEventArgs e)
        {
            var menu = (ContextMenu)sender;
            foreach (var old in menu.Items.OfType<FrameworkElement>().Where(i => Equals(i.Tag, "inventory")).ToList())
            {
                menu.Items.Remove(old);
            }

            var index = 2;
            menu.Items.Insert(index++, new MenuItem { Header = Properties.Resources.Tree_Inventory, Tag = "inventory", IsEnabled = false, FontSize = 12, Margin = new Thickness(0, 6, 0, 0) });
            // Bütün test yardımcı araçları tek pencereden eklenir; tür pencerede seçilir.
            var tool = new MenuItem
            {
                Header = Properties.Resources.Add_Tool,
                Tag = "inventory",
                Icon = new AppIcon { Glyph = AppIcons.Inventory },
                IsEnabled = viewModel.InventoryTypes.Any(),
            };
            tool.Click += (s, args) => viewModel.OpenAddTool();
            menu.Items.Insert(index, tool);
        }

        // Ekleme penceresi: "+ Yeni alt tür" kutuyu açar / kapatır; açılınca kutuya odaklanır.
        private void NewSubTypeToggle_Click(object sender, RoutedEventArgs e)
        {
            viewModel.IsAddingSubType = !viewModel.IsAddingSubType;
            if (viewModel.IsAddingSubType)
            {
                Dispatcher.BeginInvoke(new Action(() => NewSubTypeBox.Focus()), System.Windows.Threading.DispatcherPriority.Input);
            }
        }

        private void NewSubTypeAdd_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AddSubType();
        }

        // Enter ekler, Esc kapatır (Esc pencerenin kendisini kapatmasın).
        private void NewSubTypeBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                viewModel.AddSubType();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                viewModel.IsAddingSubType = false;
                e.Handled = true;
            }
        }

        // Doğrulamalar, durum süzgeci: Temizle bütün işaretleri kaldırır (hepsi görünür).
        private void VerStatusClear_Click(object sender, RoutedEventArgs e)
        {
            viewModel.ClearVerificationStatusFilter();
        }

        // Kullanım yeri arama kutusu: ↓ ↑ listede gezer, Enter seçer (seçili yoksa ilkini), Esc listeyi kapatır.
        private void UsePlaceBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var count = UsePlaceList.Items.Count;
            if (e.Key == Key.Down || e.Key == Key.Up)
            {
                if (count > 0)
                {
                    viewModel.IsUsePlaceOpen = true;
                    var index = UsePlaceList.SelectedIndex + (e.Key == Key.Down ? 1 : -1);
                    UsePlaceList.SelectedIndex = Math.Max(0, Math.Min(count - 1, index));
                    UsePlaceList.ScrollIntoView(UsePlaceList.SelectedItem);
                }

                e.Handled = true;
            }
            else if (e.Key == Key.Enter && viewModel.IsUsePlaceOpen && count > 0)
            {
                viewModel.ChooseUsePlace((UsePlaceList.SelectedItem ?? UsePlaceList.Items[0]) as ViewModels.LevelOption);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && viewModel.IsUsePlaceOpen)
            {
                viewModel.IsUsePlaceOpen = false;
                e.Handled = true;
            }
        }

        private void UsePlaceBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // Liste tıklanırken kutu odağı kaybeder; seçim tıklamada yapılır, burada yalnız dışarı çıkılınca yazı geri alınır.
            if (!UsePlaceList.IsMouseOver)
            {
                viewModel.RestoreUsePlaceText();
            }
        }

        private void UsePlaceList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var item = ItemsControl.ContainerFromElement(UsePlaceList, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (item != null)
            {
                viewModel.ChooseUsePlace(item.DataContext as ViewModels.LevelOption);
                UsePlaceBox.Focus();
                UsePlaceBox.CaretIndex = UsePlaceBox.Text.Length;
                e.Handled = true;
            }
        }
        // Kayıt sayfası, Kullanan stoklar: "+ Stoka bağla" stok kutusunu açar ve listeyi indirir.
        private void UsingStockLinkToggle_Click(object sender, RoutedEventArgs e)
        {
            viewModel.IsLinkingStock = !viewModel.IsLinkingStock;
            if (viewModel.IsLinkingStock)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    StockLinkBox.Focus();
                    StockLinkBox.IsDropDownOpen = true;
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
        }

        // Satır: o stokun sayfasını açar.
        private void UsingStockOpen_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is string path && path.Length > 0)
            {
                viewModel.SelectNodeByPath(path);
            }
        }

        // ✕: ilk tık onay ister, ikinci tık bağı kaldırır.
        private void UsingStockUnlink_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is ViewModels.UsingStockRow row)
            {
                viewModel.UnlinkUsingStock(row);
            }
        }
        // Stok sayfası, Test yardımcı araçları: "Ekle" ortak pencereyi açar; kullanım yeri bu stok gelir.
        private void StockToolAdd_Click(object sender, RoutedEventArgs e)
        {
            viewModel.OpenAddTool();
        }

        // Başlıktaki üst kayıt (proje / stok) bağlantısı: oraya gider.
        private void HeaderParent_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.Detail.ParentPath.Length > 0)
            {
                viewModel.SelectNodeByPath(viewModel.Detail.ParentPath);
            }
        }

        // Başlıktaki ⋯: yolu kopyala, düzenle, takipten çıkar, sil.
        private void HeaderMore_Click(object sender, RoutedEventArgs e)
        {
            var node = viewModel.SelectedNode;
            if (node == null)
            {
                return;
            }

            var menu = new ContextMenu { PlacementTarget = (UIElement)sender, Placement = PlacementMode.Bottom };
            var copy = new MenuItem { Header = Properties.Resources.Header_CopyPath, Icon = new AppIcon { Glyph = AppIcons.Copy } };
            copy.Click += (s, args) =>
            {
                try
                {
                    Clipboard.SetText(node.Path);
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    // Pano başka bir uygulamada kilitliyse sessizce geçilir.
                }
            };
            menu.Items.Add(copy);
            if (viewModel.CanEditSelected)
            {
                menu.Items.Add(new Separator());
                var edit = new MenuItem { Header = Properties.Resources.Menu_Edit, Icon = new AppIcon { Glyph = AppIcons.Edit } };
                edit.Click += NodeEdit_Click;
                var untrack = new MenuItem { Header = Properties.Resources.Menu_Untrack, Icon = new AppIcon { Glyph = AppIcons.Remove } };
                untrack.Click += NodeUntrack_Click;
                var delete = new MenuItem { Header = Properties.Resources.Menu_Delete, Icon = new AppIcon { Glyph = AppIcons.Delete } };
                delete.Click += NodeDelete_Click;
                menu.Items.Add(edit);
                menu.Items.Add(untrack);
                menu.Items.Add(delete);
            }

            menu.IsOpen = true;
        }

        // Seri numaraları tablosunda hücre: o turun notunu (yoksa klasörünü) açar.
        private void SerialCell_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is MatrixCell cell && cell.HasRun)
            {
                viewModel.OpenNoteOrFolder(cell.NotePath, cell.Folder);
            }
        }

        // "+" düğmesi: menüyü düğmenin altında açar (ok yok, sade ikon).
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var menu = (ContextMenu)FindResource("AddMenu");
            menu.PlacementTarget = (UIElement)sender;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private static Brush? BrushOf(string color)
        {
            try
            {
                return color.Length == 0 ? null : (Brush?)new BrushConverter().ConvertFromString(color);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        // Envanter kaydının durum menüsü: kayıt türünün durumları; seçince tarih / kişi / konum / not sorulur.
        private void InventoryStatusMenu_Opened(object sender, RoutedEventArgs e)
        {
            var menu = (ContextMenu)sender;
            menu.Items.Clear();
            var type = viewModel.Inventory.Type;
            if (type == null)
            {
                return;
            }

            var current = viewModel.Inventory.Status.Name;
            foreach (var status in type.Statuses)
            {
                menu.Items.Add(CreateInventoryStatusItem(status.Name, current));
            }

            if (type.SideStatuses.Count > 0)
            {
                menu.Items.Add(new Separator());
                foreach (var status in type.SideStatuses)
                {
                    menu.Items.Add(CreateInventoryStatusItem(status.Name, current));
                }
            }
        }

        private MenuItem CreateInventoryStatusItem(string status, string current)
        {
            var item = new MenuItem { Header = status + "…" };
            if (string.Equals(status, current, StringComparison.OrdinalIgnoreCase))
            {
                item.Icon = new AppIcon { Glyph = AppIcons.Check };
            }

            item.Click += (s, args) =>
            {
                viewModel.NoteMenuChoice();
                viewModel.AskInventoryEvent(status);
            };
            return item;
        }

        private void InventoryDialog_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            if (!viewModel.ConfirmNewInventory())
            {
                Dispatcher.BeginInvoke(new Action(() => viewModel.IsInventoryDialogOpen = true));
            }
        }

        private void EventDialog_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            viewModel.ConfirmInventoryEvent();
        }

        // Envanter listesinde satıra çift tıklayınca ağaçta o kayıt seçilir.
        private void InventoryList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || viewModel.IsDialogBusy)
            {
                return;
            }

            var source = e.OriginalSource as DependencyObject;
            while (source != null && !(source is ListViewItem))
            {
                source = VisualTreeHelper.GetParent(source);
            }

            if ((source as ListViewItem)?.DataContext is InventoryRow row && row.Folder.Length > 0)
            {
                viewModel.ShowInventory(row.Folder);
            }
        }

        // Özet tablosunda satıra çift tıklayınca o klasör açılır.
        private void DetailList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Yalnızca bir satırın üzerine sol tuşla yapılan çift tıklama; diyalog ya da menü az önce kapandıysa yok say.
            if (e.ChangedButton != MouseButton.Left || viewModel.IsDialogBusy)
            {
                return;
            }

            var source = e.OriginalSource as DependencyObject;
            while (source != null && !(source is ListViewItem))
            {
                source = VisualTreeHelper.GetParent(source);
            }

            if ((source as ListViewItem)?.DataContext is DetailRowViewModel row)
            {
                OpenDetailRow(row);
            }
        }

        // Proje tablosu: çift tık / "Aç" stokun sayfasına gider; ağaçta yoksa klasörü açar.
        private void OpenDetailRow(DetailRowViewModel row)
        {
            viewModel.SelectNodeByPath(row.Path);
            if (!string.Equals(viewModel.SelectedNode?.Path, row.Path, StringComparison.OrdinalIgnoreCase))
            {
                viewModel.OpenPath(row.Path);
            }
        }

        private static DetailRowViewModel? DetailMenuRow(object sender)
        {
            var menu = (sender as MenuItem)?.Parent as ContextMenu;
            return (menu?.PlacementTarget as ListView)?.SelectedItem as DetailRowViewModel;
        }

        private void DetailRowOpen_Click(object sender, RoutedEventArgs e)
        {
            var row = DetailMenuRow(sender);
            if (row != null)
            {
                OpenDetailRow(row);
            }
        }

        private void DetailRowFolder_Click(object sender, RoutedEventArgs e)
        {
            var row = DetailMenuRow(sender);
            if (row != null)
            {
                viewModel.OpenPath(row.Path);
            }
        }

        private void DetailRowNote_Click(object sender, RoutedEventArgs e)
        {
            var row = DetailMenuRow(sender);
            if (row != null)
            {
                viewModel.OpenQuickNote(QuickNoteTarget.Stock, row.Path, row.Name);
            }
        }

        // Geçmiş / proje notlarında Obsidian olay notunu açar.
        private void HistoryNote_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is string path && path.Length > 0)
            {
                viewModel.OpenNoteOrFolder(path, System.IO.Path.GetDirectoryName(path) ?? string.Empty);
            }
        }

        // Satır başındaki klasör düğmesi.
        private void RowFolder_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is string path && path.Length > 0)
            {
                viewModel.OpenPath(path);
            }
        }

        // Kısa not kutusu: Enter ya da "Not ekle" yazar; kutunun Tag'i nereye yazılacağını söyler (Project, Stock, Record).
        private void QuickNoteBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox box)
            {
                SubmitQuickNote(box);
                e.Handled = true;
            }
        }

        private void StockFilterClear_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Detail.ClearStockFilter();
        }

        private void RunFilterClear_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Detail.ClearRunFilter();
        }

        private void QuickNoteAdd_Click(object sender, RoutedEventArgs e)
        {
            var grid = (sender as FrameworkElement)?.Parent as Panel;
            var box = grid?.Children.OfType<TextBox>().FirstOrDefault();
            if (box != null)
            {
                SubmitQuickNote(box);
            }
        }

        private void SubmitQuickNote(TextBox box)
        {
            QuickNoteTarget target;
            if (Enum.TryParse(box.Tag as string, out target) && viewModel.AddQuickNoteHere(target, box.Text))
            {
                box.Text = string.Empty;
            }
        }

        private void QuickNote_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            viewModel.ConfirmQuickNote();
        }

        // Düzenle / takipten çıkar: ağaç, envanter satırı, proje tablosu satırı aynı diyaloğu açar.
        private void NodeEdit_Click(object sender, RoutedEventArgs e)
        {
            viewModel.OpenEditSelected();
        }

        private void NodeUntrack_Click(object sender, RoutedEventArgs e)
        {
            viewModel.OpenUntrackSelected();
        }

        private void StockInvEdit_Click(object sender, RoutedEventArgs e)
        {
            var row = MenuRow(sender);
            if (row != null && row.Folder.Length > 0)
            {
                viewModel.OpenEdit(row.Folder);
            }
        }

        private void StockInvUntrack_Click(object sender, RoutedEventArgs e)
        {
            var row = MenuRow(sender);
            if (row != null && row.Folder.Length > 0)
            {
                viewModel.OpenUntrack(row.Folder);
            }
        }

        private void DetailRowEdit_Click(object sender, RoutedEventArgs e)
        {
            var row = DetailMenuRow(sender);
            if (row != null)
            {
                viewModel.OpenEdit(row.Path);
            }
        }

        private void DetailRowUntrack_Click(object sender, RoutedEventArgs e)
        {
            var row = DetailMenuRow(sender);
            if (row != null)
            {
                viewModel.OpenUntrack(row.Path);
            }
        }

        private void Edit_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            viewModel.ConfirmEdit();
        }

        private void Untrack_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            viewModel.ConfirmUntrack();
        }

        // Test planı kartı: dosyayı (KIWI planı KIWI ile) ya da klasörünü açar.
        private void PlanOpen_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is string path && path.Length > 0)
            {
                viewModel.OpenNoteOrFolder(path, System.IO.Path.GetDirectoryName(path) ?? string.Empty);
            }
        }

        private void PlanFolder_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is string path && path.Length > 0)
            {
                viewModel.OpenPath(path);
            }
        }

        // Açık iş: notu açar (Ayarlar'daki programla).
        // Yapılacaklar: Ekle düğmesi / Enter işi ekler; kutu işi kapatır ya da yeniden açar.
        private void TaskAdd_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AddTask();
        }

        private void TaskBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox box && !MentionAssist.IsOpen(box))
            {
                box.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
                viewModel.AddTask();
                e.Handled = true;
            }
        }

        private void TaskDone_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox box && box.DataContext is OpenTaskRow row)
            {
                // Hemen değiştirilmez: kutu eski haline döner, önce sorulur.
                var done = box.IsChecked == true;
                box.IsChecked = row.IsDone;
                viewModel.RequestTaskDone(row, done);
            }
        }

        private void TaskConfirm_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            viewModel.ConfirmTaskDone();
        }

        // Başlangıç sayfası: son açılan düğüme gider.
        private void HomeRecent_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is TreeNodeViewModel node)
            {
                viewModel.SelectNodeByPath(node.Path);
            }
        }

        // Başlangıç sayfası: işin geldiği stoka / kayda gider.
        private void HomeTask_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is OpenTaskRow row)
            {
                viewModel.OpenHomeTask(row);
            }
        }

        private void OpenTaskNote_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is string path && path.Length > 0)
            {
                viewModel.OpenNoteOrFolder(path, System.IO.Path.GetDirectoryName(path) ?? string.Empty);
            }
        }

        // + Olay: gösterilen stokun ya da kaydın ana notuna.
        private void JournalAdd_Click(object sender, RoutedEventArgs e)
        {
            viewModel.OpenJournalHere();
        }

        // Günlük olayında sağ tık: düzenle / sil.
        private static Pusula.Core.Notes.EventNote? JournalOf(object sender)
        {
            var context = (sender as FrameworkElement)?.DataContext;
            return (context as TimelineItem)?.Journal ?? (context as HistoryRow)?.Journal;
        }

        private void JournalEdit_Click(object sender, RoutedEventArgs e)
        {
            var note = JournalOf(sender);
            if (note != null)
            {
                viewModel.OpenJournalEdit(note);
            }
        }

        private void JournalDelete_Click(object sender, RoutedEventArgs e)
        {
            var note = JournalOf(sender);
            if (note != null)
            {
                viewModel.OpenJournalDelete(note);
            }
        }

        private void JournalDelete_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            viewModel.ConfirmJournalDelete();
        }

        private void Journal_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            viewModel.ConfirmJournal();
        }

        private void NodeDelete_Click(object sender, RoutedEventArgs e)
        {
            viewModel.OpenDeleteSelected();
        }

        private void StockInvDelete_Click(object sender, RoutedEventArgs e)
        {
            var row = MenuRow(sender);
            if (row != null && row.Folder.Length > 0)
            {
                viewModel.OpenDelete(row.Folder);
            }
        }

        private void DetailRowDelete_Click(object sender, RoutedEventArgs e)
        {
            var row = DetailMenuRow(sender);
            if (row != null)
            {
                viewModel.OpenDelete(row.Path);
            }
        }

        private void Delete_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            viewModel.ConfirmDelete();
        }

        /// <summary>Şablonlar penceresini açar.</summary>
        internal void EditTemplates(Window owner)
        {
            if (!viewModel.CanEditTemplates())
            {
                return;
            }

            var editor = new TemplateEditorWindow(new TemplateEditorViewModel(viewModel.RootPath, viewModel.Templates))
            {
                Owner = owner,
            };
            if (editor.ShowDialog() == true)
            {
                viewModel.ReloadTemplates();
            }
        }

        private void ModuleDialog_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            viewModel.ConfirmAddModules();
        }

        private void StockDialog_PrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            if (!viewModel.ConfirmNewStock())
            {
                Dispatcher.BeginInvoke(new Action(() => viewModel.IsStockDialogOpen = true));
            }
        }
    }
}
