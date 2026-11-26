using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Pusula.Core.Office;

namespace Pusula.Core.Testing
{
    /// <summary>Excel'den okunan bir gereksinim satırı.</summary>
    public sealed class ImportedRequirement
    {
        /// <summary>TG no.</summary>
        public string TgNo { get; set; } = string.Empty;

        /// <summary>Gereksinim metni (madde açıklaması).</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>Tür: Temel ya da Türetilmiş.</summary>
        public string Kind { get; set; } = string.Empty;

        /// <summary>Not.</summary>
        public string Note { get; set; } = string.Empty;

        /// <summary>Üstündeki başlık: "3.1 Elektriksel özellikler" (yoksa boş).</summary>
        public string Section { get; set; } = string.Empty;
    }

    /// <summary>İçe aktarma sonucu: okunan maddeler, atlanan başlık satırları, bulunamayan sütunlar.</summary>
    public sealed class RequirementImportResult
    {
        /// <summary>Okunan gereksinimler (sırasıyla).</summary>
        public List<ImportedRequirement> Items { get; } = new List<ImportedRequirement>();

        /// <summary>Başlık sayılıp madde yapılmayan satırlar ("3.1 Elektriksel özellikler").</summary>
        public List<string> Headings { get; } = new List<string>();

        /// <summary>Hata (başlık satırı bulunamadı, dosya okunamadı …); yoksa boş.</summary>
        public string Error { get; set; } = string.Empty;
    }

    /// <summary>
    /// DOORS'tan alınan Excel'den gereksinim içe aktarma (deneme, 22.11.2026). İlk sayfada başlık satırı aranır
    /// (TG no / ID; Gereksinim / Açıklama / Object Text; Tür; Not). Tür'ü boş ve metni numarayla başlayan satır
    /// ("3.1 Elektriksel özellikler") başlıktır: madde olmaz, altındaki maddelerin bölümü olur.
    /// </summary>
    public static class RequirementImport
    {
        private static readonly Regex HeadingPattern = new Regex(@"^\d+(\.\d+)*\.?\s+\S", RegexOptions.CultureInvariant);

        /// <summary>Excel dosyasını okur.</summary>
        public static RequirementImportResult ReadExcel(string path)
        {
            try
            {
                return Parse(XlsxReader.ReadFirstSheet(path));
            }
            catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException || ex is System.IO.InvalidDataException || ex is System.Xml.XmlException)
            {
                return new RequirementImportResult { Error = ex.Message };
            }
        }

        /// <summary>Satırlardan gereksinimleri çıkarır.</summary>
        public static RequirementImportResult Parse(IList<List<string>> rows)
        {
            var result = new RequirementImportResult();
            var headerAt = -1;
            int colTg = -1, colText = -1, colKind = -1, colNote = -1;
            for (var r = 0; r < rows.Count && headerAt < 0; r++)
            {
                for (var c = 0; c < rows[r].Count; c++)
                {
                    switch (Norm(rows[r][c]))
                    {
                        case "tgno":
                        case "tg":
                        case "id":
                        case "objectidentifier":
                        case "maddeno":
                            colTg = colTg < 0 ? c : colTg;
                            break;
                        case "gereksinim":
                        case "açıklama":
                        case "aciklama":
                        case "maddeaçıklaması":
                        case "objecttext":
                        case "metin":
                            colText = colText < 0 ? c : colText;
                            break;
                        case "tür":
                        case "tur":
                        case "objecttype":
                            colKind = colKind < 0 ? c : colKind;
                            break;
                        case "not":
                        case "açıklamanotu":
                        case "notes":
                            colNote = colNote < 0 ? c : colNote;
                            break;
                    }
                }

                if (colTg >= 0 && colText >= 0)
                {
                    headerAt = r;
                }
                else
                {
                    colTg = colText = colKind = colNote = -1;
                }
            }

            if (headerAt < 0)
            {
                result.Error = "header";
                return result;
            }

            var section = string.Empty;
            for (var r = headerAt + 1; r < rows.Count; r++)
            {
                var tg = Cell(rows[r], colTg);
                var text = Cell(rows[r], colText);
                var kind = Cell(rows[r], colKind);
                if (tg.Length == 0 && text.Length == 0)
                {
                    continue;
                }

                if (kind.Length == 0 && HeadingPattern.IsMatch(text))
                {
                    section = text;
                    result.Headings.Add(text);
                    continue;
                }

                result.Items.Add(new ImportedRequirement { TgNo = tg, Text = text, Kind = kind, Note = Cell(rows[r], colNote), Section = section });
            }

            return result;
        }

        /// <summary>
        /// Okunanları doğrulama dosyasına yazar: TG no'su olmayan madde eklenir; var olanın yalnız gereksinim metni, türü,
        /// notu ve bölümü (grup) güncellenir (durum, bağlar, elle yazılan başlık korunur); yeni maddenin başlığı boş kalır, sonra elle yazılır. Hiçbir madde silinmez. (eklenen, güncellenen, aynı).
        /// </summary>
        public static Tuple<int, int, int> Apply(string stockFolder, IList<ImportedRequirement> items, string defaultGroup, string newFileTemplate, out string error)
        {
            error = string.Empty;
            int added = 0, updated = 0, same = 0;
            var existing = VerificationList.Read(stockFolder);
            foreach (var imp in items)
            {
                var old = imp.TgNo.Length == 0 ? null : existing.FirstOrDefault(i => string.Equals(i.TgNo.Trim(), imp.TgNo, StringComparison.OrdinalIgnoreCase));
                if (old == null)
                {
                    var item = new VerificationItem
                    {
                        TgNo = imp.TgNo,
                        Requirement = imp.Text,
                        Text = string.Empty,
                        Kind = imp.Kind,
                        Description = imp.Note,
                        Group = KnownGroup(imp.Section, defaultGroup),
                        Status = string.Empty,
                    };
                    error = VerificationList.Add(stockFolder, item, newFileTemplate);
                    if (error.Length > 0)
                    {
                        return Tuple.Create(added, updated, same);
                    }

                    added++;
                    continue;
                }

                var group = KnownGroup(imp.Section, old.Group);
                if (old.Requirement == imp.Text && old.Kind == imp.Kind && old.Description == imp.Note && old.Group == group)
                {
                    same++;
                    continue;
                }

                old.Requirement = imp.Text;
                old.Kind = imp.Kind;
                old.Description = imp.Note;
                old.Group = group;
                error = VerificationList.Update(stockFolder, old);
                if (error.Length > 0)
                {
                    return Tuple.Create(added, updated, same);
                }

                updated++;
            }

            return Tuple.Create(added, updated, same);
        }

        // Excel başlığı bir grup adıysa (İşlevsel, ÇK …) o; değilse verilen grup (26.11.2026: başlıklar grup olmaz).
        private static string KnownGroup(string section, string fallback)
        {
            var known = VerificationList.Groups.FirstOrDefault(g => string.Equals(g, (section ?? string.Empty).Trim(), StringComparison.CurrentCultureIgnoreCase));
            return known ?? fallback;
        }

        private static string Cell(List<string> row, int col)
        {
            return col >= 0 && col < row.Count ? (row[col] ?? string.Empty).Trim() : string.Empty;
        }

        private static string Norm(string text)
        {
            return new string((text ?? string.Empty).ToLowerInvariant().Where(c => !char.IsWhiteSpace(c) && c != '.' && c != '_' && c != '-').ToArray());
        }
    }
}
