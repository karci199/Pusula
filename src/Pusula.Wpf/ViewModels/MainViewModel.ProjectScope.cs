using System;
using System.Globalization;
using System.IO;
using Pusula.Core.Tracking;
using Pusula.Wpf.Properties;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>
    /// Proje sayfası = ana sayfanın o projeye daraltılmış hali: aynı sekmeler (Günlük, Yapılacaklar, Kayıtlar),
    /// kapsam yalnız seçili proje. Kayıtlar'da Projeler görünümü ve proje süzgeci yoktur; üstte proje başlığı durur.
    /// </summary>
    public sealed partial class MainViewModel
    {
        private string? scopeProject;

        /// <summary>Ana sayfa sekmeleri bir projeye daraltılmış mı (proje seçili).</summary>
        public bool IsProjectScope
        {
            get { return scopeProject != null; }
        }

        /// <summary>Sekmeli sayfa görünür mü: hiçbir şey seçili değilken (ana sayfa) ya da proje seçiliyken.</summary>
        public bool IsHomePageVisible
        {
            get { return selectedNode == null || selectedNode.Kind == FolderKind.Project; }
        }

        /// <summary>Kayıt sayfası (stok, envanter …) görünür mü.</summary>
        public bool IsDetailVisible
        {
            get { return selectedNode != null && selectedNode.Kind != FolderKind.Project; }
        }

        /// <summary>Proje başlığı: kod (klasör adının ilk "_"e kadarki kısmı).</summary>
        public string ScopeProjectCode
        {
            get
            {
                var folder = scopeProject == null ? string.Empty : Path.GetFileName(scopeProject);
                var cut = folder.IndexOf('_');
                return cut > 0 ? folder.Substring(0, cut) : folder;
            }
        }

        /// <summary>Proje başlığı: ad.</summary>
        public string ScopeProjectName
        {
            get
            {
                ProjectRow? row;
                return scopeProject != null && projectRows.TryGetValue(scopeProject, out row) ? row.Name : string.Empty;
            }
        }

        /// <summary>Proje başlığı: renk.</summary>
        public string ScopeProjectColor
        {
            get { return selectedNode != null && selectedNode.Kind == FolderKind.Project ? selectedNode.ProjectColor : string.Empty; }
        }

        /// <summary>Proje başlığının altındaki sayılar: "4 stok · 4 MB · son değişiklik 11.11.2026".</summary>
        public string ScopeProjectStats
        {
            get
            {
                ProjectRow? row;
                if (scopeProject == null || !projectRows.TryGetValue(scopeProject, out row))
                {
                    return string.Empty;
                }

                var text = string.Format(CultureInfo.CurrentCulture, Resources.Scope_Stocks, row.Stocks);
                if (row.Bytes.HasValue)
                {
                    text += " · " + row.SizeText;
                }

                if (row.LastChange.HasValue)
                {
                    text += " · " + string.Format(CultureInfo.CurrentCulture, Resources.Project_LastChange, row.LastChangeText);
                }

                return text;
            }
        }

        // Seçim değişince: proje seçildiyse sekmeler o projeye daraltılıp yeniden yüklenir; ana sayfaya dönünce kapsam kalkar.
        private void SyncProjectScope()
        {
            var project = selectedNode != null && selectedNode.Kind == FolderKind.Project ? selectedNode.Path : null;
            var changed = !string.Equals(project, scopeProject, StringComparison.OrdinalIgnoreCase);
            scopeProject = project;
            OnPropertyChanged(nameof(IsHomePageVisible));
            OnPropertyChanged(nameof(IsDetailVisible));
            if (!changed)
            {
                return;
            }

            OnPropertyChanged(nameof(IsProjectScope));
            OnPropertyChanged(nameof(ScopeProjectCode));
            OnPropertyChanged(nameof(ScopeProjectColor));
            NotifyScopeHeader();
            if (project != null && recordView == "projects")
            {
                IsViewStocks = true;
            }

            if (project != null || selectedNode == null)
            {
                LoadHome();
            }
        }

        private void NotifyScopeHeader()
        {
            OnPropertyChanged(nameof(ScopeProjectName));
            OnPropertyChanged(nameof(ScopeProjectStats));
        }

        // Kapsamdaki proje: yalnız onun altındaki yollar (işler, olaylar) kapsamdadır; kapsam yoksa hepsi.
        private bool InScope(string path)
        {
            return scopeProject == null || path.StartsWith(scopeProject.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase);
        }
    }
}
