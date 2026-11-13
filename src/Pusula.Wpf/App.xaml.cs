using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Pusula.Wpf
{
    /// <summary>
    /// Uygulama giriş noktası. Varsayılan DesignKit teması <c>App.xaml</c> içinde yüklenir; kayıtlı tema açılışta uygulanır.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>Uygulama gerçekten başlatıldı mı (testlerde <c>false</c>); kök yoksa ilk açılışta Ayarlar açılır.</summary>
        internal static bool Started { get; private set; }

        /// <summary>Beklenmeyen hataların yazıldığı dosya (<c>%LOCALAPPDATA%\Pusula\hatalar.log</c>).</summary>
        public static string ErrorLogPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pusula", "hatalar.log"); }
        }

        /// <summary>Kayıtlı temayı uygular, beklenmeyen hata yakalayıcılarını kurar, sonra ana pencere açılır.</summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            Started = true;
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                LogError(args.Exception);
                args.SetObserved();
            };
            Themes.Apply(Themes.Find(AppSettings.Load().Theme));
            base.OnStartup(e);
        }

        // Arayüzde yakalanmamış hata: uygulama kapanmaz (açık md'deki yazı kaybolmasın); hata dosyaya yazılır ve gösterilir.
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogError(e.Exception);
            e.Handled = true;
            MessageBox.Show(
                string.Format(Pusula.Wpf.Properties.Resources.Error_Unexpected, e.Exception.Message, ErrorLogPath),
                Pusula.Wpf.Properties.Resources.Error_UnexpectedTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        /// <summary>Hatayı zamanıyla birlikte hata dosyasına ekler; yazılamazsa sessizce geçer.</summary>
        public static void LogError(Exception exception)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ErrorLogPath)!);
                var line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " " + exception + Environment.NewLine + Environment.NewLine;
                File.AppendAllText(ErrorLogPath, line, new System.Text.UTF8Encoding(true));
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
