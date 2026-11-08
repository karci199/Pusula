using System.Windows;

namespace Pusula.Wpf
{
    /// <summary>
    /// Uygulama giriş noktası. Varsayılan DesignKit teması <c>App.xaml</c> içinde yüklenir; kayıtlı tema açılışta uygulanır.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>Uygulama gerçekten başlatıldı mı (testlerde <c>false</c>); kök yoksa ilk açılışta Ayarlar açılır.</summary>
        internal static bool Started { get; private set; }

        /// <summary>Kayıtlı temayı uygular, sonra ana pencere açılır.</summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            Started = true;
            Themes.Apply(Themes.Find(AppSettings.Load().Theme));
            base.OnStartup(e);
        }
    }
}
