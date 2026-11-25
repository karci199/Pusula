using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Pusula.Core.Testing;
using Pusula.Core.Tracking;
using Pusula.Core.Types;
using Pusula.Wpf.Mvvm;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Doğrulamalar'ın üstündeki doküman şeridinde bir doküman: kod, ad, durum yazısı ve rengi.</summary>
    public sealed class StockDocumentRow
    {
        /// <summary>Tek doküman; tıklanınca dosya (yoksa doküman klasörü) Gezgin'de gösterilir.</summary>
        public StockDocumentRow(StockDocument document, string folder)
            : this(new[] { document }, document.Definition.Name, folder)
        {
        }

        /// <summary>
        /// Birlikte aktarılan ve durumu aynı dokümanlar tek çip ("032 · 132 · 133  Yazılım dokümanları  AA · aktarıldı");
        /// tek dokümanda <paramref name="name"/> dokümanın adıdır.
        /// </summary>
        public StockDocumentRow(IList<StockDocument> documents, string name, string folder)
        {
            var document = documents[0];
            Document = document;
            Code = string.Join(" · ", documents.Select(d => d.Definition.Code));
            Name = name;
            StateText = StateTextOf(document);
            Tone = ToneOf(document.State);
            Path = document.Path;
            ToolTip = document.Path.Length > 0
                ? string.Join(Environment.NewLine, documents.Select(d => d.Path))
                : string.Format(CultureInfo.CurrentCulture, Resources.Doc_NotHereHint, folder, string.Join(", ", documents.Select(d => d.Definition.Code)));
            var target = document.Path.Length > 0 ? document.Path : folder;
            Open = new RelayCommand(() => Reveal(target));
        }

        /// <summary>Okunan durum.</summary>
        public StockDocument Document { get; }

        /// <summary>Kod ("261").</summary>
        public string Code { get; }

        /// <summary>Ad.</summary>
        public string Name { get; }

        /// <summary>Durum yazısı ("P1 · taslak aktarıldı", "AA · hazırlanıyor", "AA · aktarıldı", "AA aktarıldı · BA değişiklik bekliyor", "Burada yok").</summary>
        public string StateText { get; }

        /// <summary>Renk (<see cref="StatusTone"/>): aktarıldı yeşil, prototip aktarıldı mavi, taslak sarı, değişiklik bekliyor kırmızı, yok gri.</summary>
        public string Tone { get; }

        /// <summary>Dosya / klasör yolu; yoksa boş.</summary>
        public string Path { get; }

        /// <summary>İpucu: dosyanın yolu ya da aranan klasör ve ad düzeni.</summary>
        public string ToolTip { get; }

        /// <summary>Dosyayı (yoksa doküman klasörünü) Gezgin'de gösterir.</summary>
        public ICommand Open { get; }

        /// <summary>Durum yazısı.</summary>
        public static string StateTextOf(StockDocument document)
        {
            switch (document.State)
            {
                case DocumentState.PrototypeDraft:
                    return string.Format(CultureInfo.CurrentCulture, Resources.Doc_PrototypeDraft, document.Revision);
                case DocumentState.PrototypeReleased:
                    return string.Format(CultureInfo.CurrentCulture, Resources.Doc_PrototypeReleased, document.Revision);
                case DocumentState.Draft:
                    return string.Format(CultureInfo.CurrentCulture, Resources.Doc_Draft, document.Revision);
                case DocumentState.Released:
                    return string.Format(CultureInfo.CurrentCulture, Resources.Doc_Released, document.Revision);
                case DocumentState.ChangePending:
                    return string.Format(CultureInfo.CurrentCulture, Resources.Doc_ChangePending, document.ReleasedRevision, document.Revision);
                default:
                    return Resources.Doc_NotHere;
            }
        }

        /// <summary>Durumun rengi.</summary>
        public static string ToneOf(DocumentState state)
        {
            switch (state)
            {
                case DocumentState.Released:
                    return StatusTone.Done;
                case DocumentState.PrototypeReleased:
                    return StatusTone.Active;
                case DocumentState.Draft:
                case DocumentState.PrototypeDraft:
                    return StatusTone.Waiting;
                case DocumentState.ChangePending:
                    return StatusTone.Failed;
                default:
                    return string.Empty;
            }
        }

        private static void Reveal(string path)
        {
            if (File.Exists(path) || Directory.Exists(path) && !string.IsNullOrEmpty(System.IO.Path.GetExtension(path)))
            {
                System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + path + "\"");
            }
            else if (Directory.Exists(path))
            {
                System.Diagnostics.Process.Start("explorer.exe", "\"" + path + "\"");
            }
            else
            {
                var parent = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
                {
                    System.Diagnostics.Process.Start("explorer.exe", "\"" + parent + "\"");
                }
            }
        }
    }

    /// <summary>
    /// Stok dokümanları (25.11.2026): Doğrulamalar'ın üstünde şerit; 261'in durumu 261 ile doğrulanan maddelerin sarısını belirler.
    /// Yalnız okunur: dosyaya ya da zaman çizelgesine bir şey yazılmaz (kullanıcı kararı).
    /// </summary>
    public sealed partial class MainViewModel
    {
        private StockDocument? document261;

        /// <summary>Bu stokta aranan dokümanlar (Şablonlar → Dokümanlar listesinden, stok önekine göre).</summary>
        public ObservableCollection<StockDocumentRow> StockDocumentRows { get; } = new ObservableCollection<StockDocumentRow>();

        /// <summary>Şerit görünür mü (stokta aranan en az bir doküman var).</summary>
        public bool HasStockDocuments
        {
            get { return StockDocumentRows.Count > 0; }
        }

        /// <summary>Doküman klasörü (<c>01_Ortak\Dokumanlar</c>).</summary>
        public string StockDocumentsFolder { get; private set; } = string.Empty;

        // Doğrulamalar okunurken (LoadVerifications) çağrılır; stok değilse şerit boşalır.
        private void LoadStockDocuments()
        {
            StockDocumentRows.Clear();
            document261 = null;
            var stock = selectedNode != null && selectedNode.Kind == FolderKind.Stock ? selectedNode : null;
            StockDocumentsFolder = stock == null ? string.Empty : StockDocuments.FolderOf(stock.Path);
            if (stock != null)
            {
                var number = Path.GetFileName(stock.Path);
                var documents = StockDocuments.Read(stock.Path, number, templates.Documents);
                document261 = documents.FirstOrDefault(d => string.Equals(d.Definition.Code.Trim(), VerificationRow.DocumentCode261, StringComparison.OrdinalIgnoreCase));
                var shownGroups = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
                foreach (var document in documents)
                {
                    // Birlikte aktarılan grup (yazılımın 032 / 132 / 133): hepsinin durumu ve revizyonu aynıysa tek çip, değilse ayrı ayrı.
                    var group = (document.Definition.Group ?? string.Empty).Trim();
                    if (group.Length == 0)
                    {
                        StockDocumentRows.Add(new StockDocumentRow(document, StockDocumentsFolder));
                        continue;
                    }

                    if (!shownGroups.Add(group))
                    {
                        continue;
                    }

                    var members = documents.Where(d => string.Equals((d.Definition.Group ?? string.Empty).Trim(), group, StringComparison.CurrentCultureIgnoreCase)).ToList();
                    if (members.All(m => m.State == members[0].State && m.Revision == members[0].Revision && m.ReleasedRevision == members[0].ReleasedRevision))
                    {
                        StockDocumentRows.Add(new StockDocumentRow(members, group, StockDocumentsFolder));
                    }
                    else
                    {
                        foreach (var member in members)
                        {
                            StockDocumentRows.Add(new StockDocumentRow(member, StockDocumentsFolder));
                        }
                    }
                }
            }

            OnPropertyChanged(nameof(StockDocumentsFolder));
            OnPropertyChanged(nameof(HasStockDocuments));
        }

        // 261 ile doğrulanan madde: plan / rapor istemez (bağ uyarısına, bağ süzgecine, özet sayılarına girmez).
        private static bool IsDocumentVerified(VerificationItem item)
        {
            return VerificationRow.IsDocumentMethod(item.Method);
        }

        // 261'in resmî (harfli) bir revizyonu aktarıldı mı; değilse 261 ile doğrulanan madde sarı kalır.
        private bool Is261Official
        {
            get { return document261 != null && document261.IsOfficial; }
        }

        // Maddenin Plan sütunundaki 261 çipi: durum yazısı; resmî aktarıldıysa yeşil, değilse sarı.
        private LinkChip DocumentChipOf()
        {
            // Kısa: "261 · AA" (resmî aktarıldı), "261 · P1 taslak" (taslak aktarıldı), "261 · AA hazırlanıyor", "261 · yok"; ayrıntı şeritte.
            string text;
            if (document261 == null)
            {
                text = Resources.Doc_261NotDefined;
            }
            else if (document261.IsOfficial)
            {
                text = string.Format(CultureInfo.CurrentCulture, Resources.Doc_ChipReleased, document261.ReleasedRevision);
            }
            else if (document261.State == DocumentState.PrototypeReleased)
            {
                text = string.Format(CultureInfo.CurrentCulture, Resources.Doc_ChipDraft, document261.Revision);
            }
            else if (document261.Revision.Length > 0)
            {
                text = string.Format(CultureInfo.CurrentCulture, Resources.Doc_ChipPreparing, document261.Revision);
            }
            else
            {
                text = Resources.Doc_ChipMissing;
            }
            return new LinkChip(text, Is261Official ? 1 : 4);
        }
    }
}
