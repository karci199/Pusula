using System.Windows;

namespace Pusula.Wpf
{
    /// <summary>
    /// Uygulama giriş noktası. Varsayılan DesignKit teması <c>App.xaml</c> içinde yüklenir; kayıtlı tema açılışta uygulanır.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>Kayıtlı temayı uygular, sonra ana pencere açılır.</summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            Themes.Apply(Themes.Find(AppSettings.Load().Theme));
            base.OnStartup(e);
        }
    }
}
