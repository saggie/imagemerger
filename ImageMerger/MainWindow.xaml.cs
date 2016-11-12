using System.Windows;

namespace ImageMerger
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var settings = ImageSettingsManager.ReadSettings("C:\\Path\\to\\settings.json");
        }
    }
}
