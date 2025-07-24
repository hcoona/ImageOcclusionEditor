using System.Threading.Tasks;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ImageOcclusionEditorWinUI3
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Wire up button events
            btnCancel.Click += BtnCancel_Click;
            btnSave.Click += BtnSave_Click;
            btnSaveExit.Click += BtnSaveExit_Click;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Close the window
            this.Close();
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement save functionality
            await SaveOcclusionAsync();
        }

        private async void BtnSaveExit_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement save and exit functionality
            await SaveOcclusionAndExitAsync();
        }

        private Task SaveOcclusionAsync()
        {
            // TODO: Implement the save logic similar to WinForms version
            // This would involve getting SVG from WebView2 and processing it
            return Task.CompletedTask;
        }

        private async Task SaveOcclusionAndExitAsync()
        {
            await SaveOcclusionAsync();
            this.Close();
        }
    }
}
