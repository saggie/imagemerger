using System;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageMerger
{
    public partial class MainWindow : Window
    {
        private static ImagesMerger imagesMerger = new ImagesMerger();

        public MainWindow()
        {
            InitializeComponent();
            GetLastLoadedFilePath();
        }

        private void OnFileDrop(object sender, DragEventArgs e)
        {
            var dropFiles = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (dropFiles == null) { return; }
            Initialize(dropFiles[0]);
        }

        private void Initialize(string settingFilePath)
        {
            imagesMerger.Initialize(settingFilePath);

            EnterRunningState();

            ShowFileNameAtWindowTitle(imagesMerger.GetOutputFileName());
            ResizeWindow(imagesMerger.mergedImage.Width, imagesMerger.mergedImage.Height);

            SaveLastLoadedFile(settingFilePath);
            RunUpdateChecker();
        }

        public void EnterRunningState()
        {
            Background = new SolidColorBrush(Colors.Black);

            textBlock1.Visibility = Visibility.Hidden;
            textBlock2.Visibility = Visibility.Hidden;
            image.Visibility = Visibility.Visible;
        }

        private void ShowFileNameAtWindowTitle(string fileName)
        {
            Title = fileName + " - " + this.Title;
        }

        private void ResizeWindow(int width, int height)
        {
            stackPanel.Width = width;
            stackPanel.Height = height;
        }

        public void RunUpdateChecker()
        {
            Task infiniteTask = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (imagesMerger.IsFileUpdated())
                    {
                        image.Dispatcher.BeginInvoke(new Action(() => UpdateImage()));
                        imagesMerger.UpdateLastUpdateMap();

                    }
                    Thread.Sleep(1000);
                }
            });
        }

        private void UpdateImage()
        {
            imagesMerger.Refresh();

            using (var stream = new MemoryStream())
            {
                imagesMerger.mergedImage.Save(stream, ImageFormat.Bmp);
                stream.Seek(0, SeekOrigin.Begin);
                image.Source = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            }
        }

        private bool s_key_pressed;
        private bool ctrl_key_pressed;
        private static object saveLock = new object();
        private static long lastSavedTime = DateTime.Now.Ticks;

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.S:         s_key_pressed    = true; break;
                case System.Windows.Input.Key.LeftCtrl:  ctrl_key_pressed = true; break;
                case System.Windows.Input.Key.RightCtrl: ctrl_key_pressed = true; break;
            }

            if (s_key_pressed && ctrl_key_pressed)
            {
                lock (saveLock)
                {
                    if (hasElapsed3SecondsSinceLastSave())
                    {
                        imagesMerger.SaveMergedImage();
                        lastSavedTime = DateTime.Now.Ticks;
                    }
                }
            }
        }

        private bool hasElapsed3SecondsSinceLastSave()
        {
            return (DateTime.Now.Ticks - lastSavedTime) > 3 * 1000 * 10000;
        }

        private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.S:         s_key_pressed    = false; break;
                case System.Windows.Input.Key.LeftCtrl:  ctrl_key_pressed = false; break;
                case System.Windows.Input.Key.RightCtrl: ctrl_key_pressed = false; break;
            }
        }

        private const string iniFileName = "settings.ini";
        private static string iniFilePath;
        private static string appName;
        private string lastLoadedFilePath;

        private void SaveLastLoadedFile(string settingFilePath)
        {
            IniFileManager.WriteToFile(iniFilePath, appName, "LastLoadedFilePath", settingFilePath);
        }

        private void GetLastLoadedFilePath()
        {
            GetIniFilePath();
            var settingFilePath = IniFileManager.ReadFromFile(iniFilePath, appName, "LastLoadedFilePath");
            if (File.Exists(settingFilePath))
            {
                this.lastLoadedFilePath = settingFilePath;
                textBlock3.Visibility = Visibility.Visible;
            }
        }

        private void GetIniFilePath()
        {
            var exePath = Environment.GetCommandLineArgs()[0];
            var exeFullPath = Path.GetFullPath(exePath);
            appName = Path.GetFileNameWithoutExtension(exePath);

            iniFilePath = Path.Combine(Path.GetDirectoryName(exeFullPath), iniFileName);
        }

        private void textBlock3_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Initialize(lastLoadedFilePath);
        }

        private void ShowErrorMessage(string errorMessage)
        {
            textBlock4.Text = string.Format("Error: {0}", errorMessage);
        }
    }
}
