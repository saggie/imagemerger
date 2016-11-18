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
        }

        private void OnFileDrop(object sender, DragEventArgs e)
        {
            var dropFiles = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (dropFiles == null) { return; }
            imagesMerger.Initialize(dropFiles[0]);

            EnterRunningState();

            ShowFileNameAtWindowTitle(imagesMerger.GetFileName());
            ResizeWindow(imagesMerger.mergedImage.Width, imagesMerger.mergedImage.Height);

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
                    if (imagesMerger.IsImageFileUpdated())
                    {
                        image.Dispatcher.BeginInvoke(new Action(() => UpdateImage()));
                        imagesMerger.UpdateLastUpdateMap();
                    }
                    Thread.Sleep(500);
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
    }
}
