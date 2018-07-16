using ImageMerger.Exceptions;
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
        private static ImageMergerCore imagesMerger = new ImageMergerCore();

        public MainWindow()
        {
            InitializeComponent();
            GetLastLoadedFilePath();
            HandleFileDropToExe();
        }

        private void HandleFileDropToExe()
        {
            if (Environment.GetCommandLineArgs().Length > 1)
            {
                var droppedFilePath = Environment.GetCommandLineArgs()[1];
                if (File.Exists(droppedFilePath))
                {
                    Initialize(droppedFilePath);
                }
            }
        }

        private void HandleAutoSaveAndExit()
        {
            if (imagesMerger.GetAutoSaveAndExitOption() == true)
            {
                SaveOutputImage();
                Application.Current.Shutdown();
            }
        }

        private void OnFileDrop(object sender, DragEventArgs e)
        {
            var dropFiles = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (dropFiles == null) { return; }
            Initialize(dropFiles[0]);
        }

        private void Initialize(string settingFilePath)
        {
            try
            {
                imagesMerger.Initialize(settingFilePath);
            }
            catch (InvalidSettingsFileException)
            {
                ShowErrorMessage("Settings file is not valid.");
                return;
            }
            catch (ImageFileNotFoundException)
            {
                ShowErrorMessage("Image files cannot be found.");
                return;
            }

            EnterRunningState();
            ShowMergedImage();
            ShowFileNameAtWindowTitle(imagesMerger.GetOutputFileName());
            ResizeWindow();

            SaveLastLoadedFile(settingFilePath);
            RunUpdateChecker();

            HandleAutoSaveAndExit();

            ShowInfoMessage("Load completed.");
        }

        public void EnterRunningState()
        {
            Background = new SolidColorBrush(Colors.Black);

            textBlock1.Visibility = Visibility.Collapsed;
            textBlock2.Visibility = Visibility.Collapsed;
            textBlock3.Visibility = Visibility.Collapsed;
            marginForStatusBar.Visibility = Visibility.Collapsed;

            image.Visibility = Visibility.Visible;
            statusBar.Visibility = Visibility.Visible;
        }

        private void ShowFileNameAtWindowTitle(string fileName)
        {
            Title = fileName + " - " + this.Title;
        }

        private void ResizeWindow()
        {
            stackPanel.Width = image.Width;
            stackPanel.Height = image.Height + statusBar.Height;
        }

        public void RunUpdateChecker()
        {
            Task infiniteTask = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (imagesMerger.IsFileUpdated())
                    {
                        image.Dispatcher.BeginInvoke(new Action(() => UpdateMergedImage()));
                    }
                    Thread.Sleep(500);
                }
            });
        }

        private void UpdateMergedImage()
        {
            imagesMerger.Refresh();
            ShowMergedImage();

            ShowInfoMessage("Updated.");
        }

        private void ShowMergedImage()
        {
            using (var stream = new MemoryStream())
            {
                imagesMerger.mergedImage.Save(stream, ImageFormat.Bmp);
                stream.Seek(0, SeekOrigin.Begin);
                image.Source = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            }
        }

        #region Keys

        private bool s_key_pressed;
        private bool r_key_pressed;
        private bool ctrl_key_pressed;

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.R:         r_key_pressed    = true; break;
                case System.Windows.Input.Key.S:         s_key_pressed    = true; break;
                case System.Windows.Input.Key.LeftCtrl:  ctrl_key_pressed = true; break;
                case System.Windows.Input.Key.RightCtrl: ctrl_key_pressed = true; break;
            }

            // Manually update
            if (r_key_pressed && ctrl_key_pressed)
            {
                image.Dispatcher.BeginInvoke(new Action(() => UpdateMergedImage()));
            }

            if (s_key_pressed && ctrl_key_pressed)
            {
                SaveOutputImage();
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
                case System.Windows.Input.Key.R:         r_key_pressed    = false; break;
                case System.Windows.Input.Key.S:         s_key_pressed    = false; break;
                case System.Windows.Input.Key.LeftCtrl:  ctrl_key_pressed = false; break;
                case System.Windows.Input.Key.RightCtrl: ctrl_key_pressed = false; break;
            }
        }

        #endregion

        private static object saveLock = new object();
        private bool saveFirstTime = true;
        private long lastSavedTime = DateTime.Now.Ticks;

        private void SaveOutputImage()
        {
            lock (saveLock)
            {
                if (saveFirstTime || hasElapsed3SecondsSinceLastSave())
                {
                    imagesMerger.SaveMergedImage();
                    lastSavedTime = DateTime.Now.Ticks;
                    saveFirstTime = false;
                    ShowInfoMessage("Save completed.");
                }
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

        private void ShowInfoMessage(string infoMessage)
        {
            statusBar.Visibility = Visibility.Visible;
            statusBarText.Text = string.Format("Info: {0} [{1}]", infoMessage, DateTime.Now);
        }

        private void ShowErrorMessage(string errorMessage)
        {
            statusBar.Visibility = Visibility.Visible;
            statusBarText.Text = string.Format("Error: {0} [{1}]", errorMessage, DateTime.Now);
        }
    }
}
