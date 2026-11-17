using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using MeDitor.ViewModels;

namespace MeDitor
{
    /// <summary>
    /// MeDitor giriş noktası: kayıtlı tema uygulanır; komut satırında verilen md dosyaları, yoksa önceki oturumun sekmeleri açılır.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>Kullanıcı ayarları (bütün pencereler paylaşır).</summary>
        public static AppSettings Settings { get; private set; } = new AppSettings();

        /// <summary>Beklenmeyen hataların yazıldığı dosya (<c>%LOCALAPPDATA%\MeDitor\hatalar.log</c>).</summary>
        public static string ErrorLogPath
        {
            get { return Path.Combine(Path.GetDirectoryName(AppSettings.FilePath)!, "hatalar.log"); }
        }

        /// <summary>Ayarları ve temayı yükler, hata yakalayıcıları kurar, ilk pencereyi açar.</summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                LogError(args.Exception);
                args.SetObserved();
            };

            Settings = AppSettings.Load();
            Themes.Apply(Themes.Find(Settings.Theme));
            var window = new MainWindow();
            MainWindow = window;
            var files = e.Args.Where(File.Exists).ToList();
            if (files.Count > 0)
            {
                foreach (var file in files)
                {
                    window.Open(file);
                }
            }
            else
            {
                window.RestoreSession();
            }

            window.Show();
        }
        /// <summary>Dosyayı ya da klasörü Windows'un varsayılanıyla açar.</summary>
        public static void OpenShell(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (System.ComponentModel.Win32Exception)
            {
            }
        }

        // Yakalanmamış hata: uygulama kapanmaz (açık metin kaybolmasın); hata dosyaya yazılır ve gösterilir.
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogError(e.Exception);
            e.Handled = true;
            MessageBox.Show(e.Exception.Message, "MeDitor", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>Hatayı zamanıyla birlikte hata dosyasına ekler; yazılamazsa sessizce geçer.</summary>
        public static void LogError(Exception exception)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ErrorLogPath)!);
                var line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " " + exception + Environment.NewLine + Environment.NewLine;
                File.AppendAllText(ErrorLogPath, line, new UTF8Encoding(true));
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