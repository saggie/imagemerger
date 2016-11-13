using System;
using System.Drawing.Imaging;
using System.IO;
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
            imagesMerger.Init(dropFiles[0]);

            EnterRunningState();

            ShowFileNameAtWindowTitle(imagesMerger.GetFileName());
            ResizeWindow(imagesMerger.margedImage.Width, imagesMerger.margedImage.Height);

            UpdateImage();
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

        private void image_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                UpdateImage();
            }
        }

        private void UpdateImage()
        {
            imagesMerger.Refresh();

            using (var stream = new MemoryStream())
            {
                imagesMerger.margedImage.Save(stream, ImageFormat.Bmp);
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
                        imagesMerger.Save();
                        lastSavedTime = DateTime.Now.Ticks;
                    }
                }
            }
        }

        private bool hasElapsed3SecondsSinceLastSave()
        {
            return (DateTime.Now.Ticks - lastSavedTime) > 30000000;
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
