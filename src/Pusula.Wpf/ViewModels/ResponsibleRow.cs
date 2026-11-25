using Pusula.Wpf.Mvvm;

namespace Pusula.Wpf.ViewModels
{
    /// <summary>Düzenle penceresinde bir sorumlu satırı: rol ve kişi (25.11.2026).</summary>
    public sealed class ResponsibleRow : ViewModelBase
    {
        private string role;
        private string person;

        /// <summary>Satırı kurar.</summary>
        public ResponsibleRow(string role, string person)
        {
            this.role = role ?? string.Empty;
            this.person = person ?? string.Empty;
        }

        /// <summary>Rol ("Tasarım sorumlusu"); boş olabilir.</summary>
        public string Role
        {
            get { return role; }
            set { SetProperty(ref role, value ?? string.Empty); }
        }

        /// <summary>Kişi (# ile Kişiler listesinden önerilir).</summary>
        public string Person
        {
            get { return person; }
            set { SetProperty(ref person, value ?? string.Empty); }
        }
    }
}
