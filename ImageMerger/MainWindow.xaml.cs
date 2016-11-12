using System.Windows;

namespace ImageMerger
{
    public partial class MainWindow : Window
    {
        private enum ApplicationState { Waiting, Running }
        private ApplicationState currentState = ApplicationState.Waiting;
        private static ImagesMerger imageMerger = new ImagesMerger();
        

        public MainWindow()
        {
            InitializeComponent();

            onFileDrop(); // temp
        }

        public void onFileDrop()
        {
            var settingsFilePath = @"C:\path\to\settings.json";
            imageMerger.Init(settingsFilePath);
            currentState = ApplicationState.Running;
        }

        public void RefreshMergedImage()
        {
            imageMerger.Refresh();
        }
    }
}
