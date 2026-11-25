using Pusula.Core.Templates;
using Pusula.Wpf.Mvvm;

namespace Pusula.Wpf.ViewModels.TemplateEditor
{
    /// <summary>Şablonlar penceresinde doküman tablosunun bir satırı (kod, ad, stok öneki).</summary>
    public sealed class DocumentEditRow : ViewModelBase
    {
        private string code = string.Empty;
        private string name = string.Empty;
        private string stockPrefix = string.Empty;
        private string group = string.Empty;

        /// <summary>Boş satır (tablonun yeni satırı).</summary>
        public DocumentEditRow()
        {
        }

        /// <summary>Tanımdan satır.</summary>
        public DocumentEditRow(DocumentDefinition definition)
        {
            code = definition.Code;
            name = definition.Name;
            stockPrefix = definition.StockPrefix;
            group = definition.Group;
        }

        /// <summary>Doküman kodu (261, 032 …); boş bırakılabilir (sonra girilecek).</summary>
        public string Code
        {
            get { return code; }
            set { SetProperty(ref code, value ?? string.Empty); }
        }

        /// <summary>Ad.</summary>
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value ?? string.Empty); }
        }

        /// <summary>Stok no öneki (AB, JY); boşsa bütün stoklar.</summary>
        public string StockPrefix
        {
            get { return stockPrefix; }
            set { SetProperty(ref stockPrefix, value ?? string.Empty); }
        }

        /// <summary>Birlikte aktarılan dokümanların grubu (aynı ad yazılanlar bir arada gösterilir).</summary>
        public string Group
        {
            get { return group; }
            set { SetProperty(ref group, value ?? string.Empty); }
        }

        /// <summary>Satır bütünüyle boş mu (kaydedilmez).</summary>
        public bool IsBlank
        {
            get { return code.Trim().Length == 0 && name.Trim().Length == 0 && stockPrefix.Trim().Length == 0 && group.Trim().Length == 0; }
        }

        /// <summary>Tanıma çevirir.</summary>
        public DocumentDefinition ToDefinition()
        {
            return new DocumentDefinition { Code = code.Trim(), Name = name.Trim(), StockPrefix = stockPrefix.Trim(), Group = group.Trim() };
        }
    }
}
