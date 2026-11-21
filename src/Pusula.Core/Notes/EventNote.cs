using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Pusula.Core.Testing;

using Pusula.Core.Scanning;

namespace Pusula.Core.Notes
{
    /// <summary>
    /// Obsidian notundan okunan olay: YAML'ında <c>olay:</c> ve <c>tarih:</c> olan her <c>.md</c> bir olaydır
    /// (toplantı, karar, numune, revizyon …). Uygulama yalnızca okur.
    /// </summary>
    public sealed class EventNote
    {
        /// <summary>Olay çeşidini veren YAML alanı.</summary>
        public const string KindKey = "olay";

        /// <summary>Test koşusu çeşidi (koşular kendi okuyucusundan gelir; burada atlanır).</summary>
        public const string TestKind = "test";

        /// <summary>Notun tam yolu.</summary>
        public string Path { get; private set; } = string.Empty;

        /// <summary>Çeşit, küçük harfle (<c>toplanti</c>, <c>karar</c> …).</summary>
        public string Kind { get; private set; } = string.Empty;

        /// <summary>Tarih (saat yoksa gün başı).</summary>
        public DateTime Date { get; private set; }

        /// <summary>Kişi (<c>kisi:</c>); boş olabilir.</summary>
        public string Person { get; private set; } = string.Empty;

        /// <summary>Başlık: gövdedeki ilk <c># başlık</c>, yoksa dosya adı.</summary>
        public string Title { get; private set; } = string.Empty;

        /// <summary>Gövdenin başı (başlık hariç).</summary>
        public string Excerpt { get; private set; } = string.Empty;

        /// <summary>Önem: <c>Yüksek</c>, <c>Düşük</c>; normalse boş.</summary>
        public string Importance { get; private set; } = string.Empty;

        /// <summary>İlgili kayıt numaraları (<c>[[JIG-1000-0009]]</c> → <c>JIG-1000-0009</c>).</summary>
        public List<string> Related { get; private set; } = new List<string>();

        /// <summary>Etiketler (<c>#SATIMO</c> → <c>SATIMO</c>).</summary>
        public List<string> Tags { get; private set; } = new List<string>();

        /// <summary>Notun içindeki <c>## Günlük</c> bölümünden mi geldi (ayrı olay notu değil).</summary>
        public bool IsJournal { get; private set; }

        /// <summary>Günlük girdisinin notta yazıldığı başlık satırı (<c>### …</c>); düzenleme / silmede girdiyi bulmak için.</summary>
        public string SourceLine { get; private set; } = string.Empty;

        /// <summary>Günlük girdisinin tam açıklaması (satır sonlarıyla).</summary>
        public string Body { get; private set; } = string.Empty;

        /// <summary>Günlük girdisinin notta yazılan tür adı ("Karar").</summary>
        public string KindLabel { get; private set; } = string.Empty;

        /// <summary>Günlük girdisinin bağlı olduğu doğrulama maddesi (<c>madde: TG-1</c>); yoksa boş.</summary>
        public string Item { get; private set; } = string.Empty;

        /// <summary>Günlük girdisinin ölçüm sonucu (<c>Geçti</c>, <c>Kaldı</c>, <c>Bilgi</c>); yoksa boş.</summary>
        public string Result { get; private set; } = string.Empty;

        /// <summary>Günlük bölümündeki bir girdiden olay kurar (bkz. <see cref="Journal"/>).</summary>
        /// <summary>Günlük girdisinin ölçüm klasörü (<c>klasör:</c>, kayıt klasörüne göre); yoksa boş.</summary>
        public string Folder { get; private set; } = string.Empty;

        /// <summary>Revizyona giren stok; revizyon değilse boş.</summary>
        public string RevisionStock { get; private set; } = string.Empty;

        /// <summary>Önceki revizyon; ilk revizyonda boş.</summary>
        public string RevisionOld { get; private set; } = string.Empty;

        /// <summary>Yeni revizyon; revizyon değilse boş.</summary>
        public string RevisionNew { get; private set; } = string.Empty;

        /// <summary>Değişiklik numarası; yoksa boş.</summary>
        public string RevisionChangeNo { get; private set; } = string.Empty;

        /// <summary>Talep türü; talep değilse boş.</summary>
        public string RequestType { get; private set; } = string.Empty;

        /// <summary>Talep numarası; yoksa boş.</summary>
        public string RequestNo { get; private set; } = string.Empty;

        /// <summary>Talebin istendiği yer ya da kişiler (@ işaretsiz).</summary>
        public List<string> RequestFrom { get; private set; } = new List<string>();

        /// <summary>İstenen tarih; yoksa boş.</summary>
        public DateTime? RequestDue { get; private set; }

        /// <summary>Talep durumu (Açık, Teslim alındı, İptal); yoksa boş.</summary>
        public string RequestStatus { get; private set; } = string.Empty;

        /// <summary>Malzeme kalemleri.</summary>
        public List<RequestItem> RequestItems { get; private set; } = new List<RequestItem>();

        /// <summary>İstenenin anlatımı.</summary>
        public string RequestDetail { get; private set; } = string.Empty;

        /// <summary>Kararı verenler (@ işaretsiz).</summary>
        public List<string> DecidedBy { get; private set; } = new List<string>();

        /// <summary>Toplantı katılımcıları (@ işaretsiz).</summary>
        public List<string> Participants { get; private set; } = new List<string>();

        /// <summary>Toplantı ortamı (<c>ortam:</c>); yoksa boş.</summary>
        public string Setting { get; private set; } = string.Empty;

        /// <summary>Toplantı kararları (<c>#### Kararlar</c> altındaki metin); yoksa boş.</summary>
        public string Decisions { get; private set; } = string.Empty;

        internal static EventNote FromJournal(string path, string kind, DateTime date, string title, string body, string person, string importance, List<string> related, List<string> tags, string sourceLine, string fullBody, string kindLabel, string item, string result, string folder, List<string> participants, string setting, string decisions, List<string> decidedBy, RequestInfo request, string[] revision)
        {
            return new EventNote
            {
                RevisionStock = revision[0],
                RevisionOld = revision[1],
                RevisionNew = revision[2],
                RevisionChangeNo = revision[3],
                RequestType = request.Type,
                RequestNo = request.No,
                RequestFrom = request.From,
                RequestDue = request.Due,
                RequestStatus = request.Status,
                RequestItems = request.Items,
                RequestDetail = request.Detail,
                DecidedBy = decidedBy,
                Participants = participants,
                Setting = setting,
                Decisions = decisions,
                Folder = folder,
                Item = item,
                Result = result,
                SourceLine = sourceLine,
                Body = fullBody,
                KindLabel = kindLabel,
                Path = path,
                Kind = kind,
                Date = date,
                Title = title,
                Excerpt = body,
                Person = person,
                Importance = importance,
                Related = related,
                Tags = tags,
                IsJournal = true,
            };
        }

        /// <summary>
        /// Klasördeki (alt klasörler dahil, <c>_</c> ile başlayanlar hariç) olay notlarını okur, yeniden eskiye sıralar.
        /// <c>olay: test</c> notları atlanır (test koşuları ayrıca okunur).
        /// </summary>
        public static List<EventNote> ReadAll(string folder)
        {
            var result = new List<EventNote>();
            foreach (var file in MarkdownFiles(folder))
            {
                var note = TryRead(file);
                if (note != null && note.Kind != TestKind)
                {
                    result.Add(note);
                }
            }

            return result.OrderByDescending(n => n.Date).ToList();
        }

        /// <summary>Tek notu okur; olay değilse (YAML'da <c>olay</c> ya da geçerli <c>tarih</c> yoksa) <c>null</c>.</summary>
        public static EventNote? TryRead(string path)
        {
            // Önce yalnızca YAML başlığına bakılır; olay değilse dosyanın geri kalanı okunmaz (KIWI raporları vb. büyük olabilir).
            if (!HeaderHasKind(path))
            {
                return null;
            }

            string text;
            try
            {
                text = Fs.ReadAllText(path);
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }

            string body;
            var fields = FrontMatter.Parse(text, out body);
            string kind;
            string dateText;
            DateTime date;
            if (!fields.TryGetValue(KindKey, out kind) || kind.Trim().Length == 0
                || !fields.TryGetValue("tarih", out dateText)
                || !DateTime.TryParseExact(dateText.Trim(), new[] { "yyyy-MM-dd", "yyyy-MM-dd HH:mm", "dd.MM.yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return null;
            }

            string person;
            var title = string.Empty;
            foreach (var raw in body.Replace("\r\n", "\n").Split('\n'))
            {
                var line = raw.Trim();
                if (line.StartsWith("# ", StringComparison.Ordinal))
                {
                    title = line.Substring(2).Trim();
                    break;
                }
            }

            var excerpt = NoteFile.Excerpt(path);
            return new EventNote
            {
                Path = path,
                Kind = kind.Trim().ToLowerInvariant(),
                Date = date,
                Person = fields.TryGetValue("kisi", out person) ? person.Trim() : string.Empty,
                Title = title.Length > 0 ? title : System.IO.Path.GetFileNameWithoutExtension(path).Replace('_', ' '),
                Excerpt = excerpt,
            };
        }

        private static bool HeaderHasKind(string path)
        {
            try
            {
                // Tarama etkinse metin bellekten gelir (açık işler de aynı notu okur); değilse yalnız baş satırlar okunur.
                using (var reader = FolderSnapshot.Active != null && FolderSnapshot.Active.Covers(path)
                    ? (TextReader)new StringReader(Fs.ReadAllText(path))
                    : new StreamReader(path, Encoding.UTF8))
                {
                    var first = reader.ReadLine();
                    if (first == null || first.Trim().TrimStart('\uFEFF') != "---")
                    {
                        return false;
                    }

                    for (var i = 0; i < 60; i++)
                    {
                        var line = reader.ReadLine();
                        if (line == null || line.Trim() == "---")
                        {
                            return false;
                        }

                        if (line.TrimStart().StartsWith(KindKey + ":", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            return false;
        }

        private static IEnumerable<string> MarkdownFiles(string folder)
        {
            if (!Fs.DirectoryExists(folder))
            {
                yield break;
            }

            string[] files;
            string[] folders;
            try
            {
                files = Fs.GetFiles(folder, "*.md");
                folders = Fs.GetDirectories(folder);
            }
            catch (IOException)
            {
                yield break;
            }
            catch (UnauthorizedAccessException)
            {
                yield break;
            }

            foreach (var file in files)
            {
                yield return file;
            }

            foreach (var sub in folders)
            {
                if (System.IO.Path.GetFileName(sub).StartsWith("_", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (var file in MarkdownFiles(sub))
                {
                    yield return file;
                }
            }
        }
    }
}
