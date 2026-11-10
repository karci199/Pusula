using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pusula.Core.Markdown;
using Pusula.Core.Tracking;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Pusula'nın md not penceresiyle bağlantı: açma, öneriler (@ / [[), [[bağlantı]] ile gezinme.</summary>
    public sealed partial class MainViewModel
    {
        /// <summary>Notu Pusula'nın md penceresinde açar (pencere tarafı verir); açtıysa <c>true</c>.</summary>
        public Func<string, bool>? NoteEditorOpener { get; set; }

        /// <summary>Gezinme sonrası ana pencere öne gelsin (pencere tarafı verir).</summary>
        public Action? ActivateMain { get; set; }

        /// <summary>Not açma ayarı Pusula'nın kendi penceresi mi.</summary>
        public bool UsesNoteEditor
        {
            get { return string.Equals(NoteApp.Key, NoteOpener.Pusula, StringComparison.OrdinalIgnoreCase); }
        }

        /// <summary>Not penceresinin görünüm modelini kurar (ayarlar paylaşılır, değişince kaydedilir).</summary>
        public NoteEditorViewModel CreateNoteEditor(string path)
        {
            var editorSettings = settings.NoteEditor ?? (settings.NoteEditor = new NoteEditorSettings());
            return new NoteEditorViewModel(path, rootPath, editorSettings, settings.Save, NoteSuggestions);
        }

        /// <summary>Not penceresinden "dışarıda aç": ayar Pusula ise Obsidian (yoksa Windows varsayılanı), değilse ayardaki program.</summary>
        public void OpenNoteExternally(string path)
        {
            var app = UsesNoteEditor ? NoteOpener.Obsidian : NoteApp.Key;
            try
            {
                var missing = NoteOpener.Open(path, app);
                if (missing.Length > 0)
                {
                    Status = string.Format(Resources.NoteApp_Missing, missing);
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                OpenPath(Path.GetDirectoryName(path) ?? string.Empty);
            }
        }

        /// <summary>Notu Pusula penceresinde açar (açıcı yoksa <c>false</c>).</summary>
        public bool OpenInNoteEditor(string path)
        {
            return NoteEditorOpener != null && File.Exists(path) && NoteEditorOpener(path);
        }

        /// <summary>
        /// @ / [[ önerileri: projeler ve stoklar (ağaçtan), jig / kablo / yazılım kayıtları ve kişiler (kayıtların olay ve
        /// notlarındaki kişiler ile bu bilgisayarın kullanıcısı).
        /// </summary>
        public IEnumerable<NoteSuggestion> NoteSuggestions()
        {
            var list = new List<NoteSuggestion>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pending = new Stack<TreeNodeViewModel>(Nodes);
            while (pending.Count > 0)
            {
                var node = pending.Pop();
                foreach (var child in node.Children)
                {
                    pending.Push(child);
                }

                if (node.Kind != FolderKind.Project && node.Kind != FolderKind.Stock)
                {
                    continue;
                }

                var key = Path.GetFileName(node.Path);
                if (seen.Add(key))
                {
                    var glyph = node.Kind == FolderKind.Project ? AppIcons.Project : AppIcons.Stock;
                    list.Add(new NoteSuggestion(key, node.Title == key ? string.Empty : node.Title, node.KindText, glyph, "[[" + key + "]]"));
                }
            }

            foreach (var item in inventoryItems)
            {
                if (item.Record.Number.Length == 0 || !seen.Add(item.Record.Number))
                {
                    continue;
                }

                var type = types.Find(item.Record.Type);
                list.Add(new NoteSuggestion(item.Record.Number, item.Record.Name, type?.Name ?? item.Record.Type, type != null ? TreeNodeViewModel.GlyphOf(type.Icon) : AppIcons.Inventory, "[[" + item.Record.Number + "]]"));
            }

            // Kişiler: önce Türler penceresindeki kişi listesi (rol · ekip ile), sonra kullanıcı ve kayıtlarda geçen adlar.
            var people = types.People.Select(p => p.Name).Concat(new[] { CurrentUser })
                .Concat(inventoryItems.SelectMany(i => i.Record.Events.Select(e => e.Person).Concat(i.Record.Notes.Select(n => n.Person))))
                .Select(p => (p ?? string.Empty).Trim())
                .Where(p => p.Length > 0)
                .Distinct(StringComparer.CurrentCultureIgnoreCase);
            foreach (var person in people)
            {
                if (seen.Add("@" + person))
                {
                    list.Add(new NoteSuggestion(person, types.FindPerson(person)?.Summary ?? string.Empty, Resources.Note_KindPerson, "", "@" + person));
                }
            }

            return list;
        }

        /// <summary>
        /// <c>[[hedef]]</c> tıklandı: hedef bir proje, stok ya da envanter kaydıysa ağaçta seçilir ve ana pencere öne gelir;
        /// değilse kökte o adda bir md notu varsa not penceresinde açılır. Hiçbiri yoksa <c>false</c> (yeni not oluşturulmaz).
        /// </summary>
        public bool NavigateToKey(string target)
        {
            var key = (target ?? string.Empty).Trim().TrimStart('@');
            if (key.Length == 0 || !HasRoot)
            {
                return false;
            }

            var record = inventoryItems.FirstOrDefault(i => string.Equals(i.Record.Number, key, StringComparison.OrdinalIgnoreCase)
                || string.Equals(Path.GetFileName(i.Folder), key, StringComparison.OrdinalIgnoreCase));
            if (record != null)
            {
                ShowInventory(record.Folder);
                ActivateMain?.Invoke();
                return true;
            }

            var node = FindNode(key);
            if (node != null)
            {
                SelectNodeByPath(node.Path);
                ActivateMain?.Invoke();
                return true;
            }

            string note;
            var index = MarkdownNotes.BuildNameIndex(rootPath);
            if (index.TryGetValue(key.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ? key : key + ".md", out note))
            {
                return OpenInNoteEditor(note);
            }

            return false;
        }

        private TreeNodeViewModel? FindNode(string key)
        {
            var pending = new Stack<TreeNodeViewModel>(Nodes);
            while (pending.Count > 0)
            {
                var node = pending.Pop();
                if (node.Kind == FolderKind.Project || node.Kind == FolderKind.Stock)
                {
                    var name = Path.GetFileName(node.Path);
                    if (string.Equals(name, key, StringComparison.OrdinalIgnoreCase) || MentionKeys(node).Contains(key, StringComparer.OrdinalIgnoreCase))
                    {
                        return node;
                    }
                }

                foreach (var child in node.Children)
                {
                    pending.Push(child);
                }
            }

            return null;
        }
    }
}
