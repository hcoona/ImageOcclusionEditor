using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Hjg.Pngcs;
using Hjg.Pngcs.Chunks;
using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;
using Svg;

namespace ImageOcclusionEditorWinUI3
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private const string SvgEditorPath = "svg-edit/svg-editor.html";

        public string OriginalSvg { get; }
        public string OcclusionFilePath { get; }
        public int OcclusionWidth { get; }
        public int OcclusionHeight { get; }

        private string BackgroundFilePath { get; set; }
        private bool isWebViewReady = false;

        public MainWindow(string backgroundFilePath, string occlusionFilePath)
        {
            InitializeComponent();

            BackgroundFilePath = backgroundFilePath;
            OcclusionFilePath = occlusionFilePath;

            OriginalSvg = ReadSvgFromChunk();

            GetImageSize(BackgroundFilePath, out int width, out int height);

            OcclusionWidth = width;
            OcclusionHeight = height;

            // Load the WebView2
            LoadWebView();
        }

        private async void LoadWebView()
        {
            try
            {
                string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ImageOcclusionEditor\\WebView2UserData");

                CoreWebView2Environment env = await CoreWebView2Environment.CreateWithOptionsAsync(
                    browserExecutableFolder: null,
                    userDataFolder: userDataFolder,
                    new CoreWebView2EnvironmentOptions
                    {
                        AdditionalBrowserArguments = "--disable-features=msSmartScreenProtection",
                    });

                var controllerOptions = env.CreateCoreWebView2ControllerOptions();
                controllerOptions.AllowHostInputProcessing = true;

                // For WinUI3, we can use the simpler approach
                await webView.EnsureCoreWebView2Async(env);

                webView.CoreWebView2.Settings.AreHostObjectsAllowed = true;
                webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                webView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;

                webView.CoreWebView2.NavigationCompleted += WebView_NavigationCompleted;
                webView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;

                GetImageSize(BackgroundFilePath, out int width, out int height);
                var path = $"{GetSvgEditorUri()}?{GenerateUrlParams(BackgroundFilePath, width, height)}";
                webView.CoreWebView2.Navigate(path);
            }
            catch (Exception ex)
            {
                ShowError($"Error initializing WebView2: {ex.Message}");
            }
        }

        private async void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                isWebViewReady = true;
                if (!string.IsNullOrWhiteSpace(OriginalSvg))
                {
                    await SetSvgInBrowserAsync(OriginalSvg);
                }

                // Inject keyboard shortcut handler
                await InjectKeyboardShortcutHandler();
            }
        }

        private async void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string message = e.TryGetWebMessageAsString();
                await HandleKeyboardShortcutFromWebView2(message);
            }
            catch (Exception ex)
            {
                ShowError($"Error handling web message: {ex.Message}");
            }
        }

        private async Task InjectKeyboardShortcutHandler()
        {
            if (!isWebViewReady) return;

            string script = @"
                (function() {
                    // Remove any existing event listener to avoid duplicates
                    if (window.imageOcclusionKeyHandler) {
                        document.removeEventListener('keydown', window.imageOcclusionKeyHandler, true);
                        document.removeEventListener('keyup', window.imageOcclusionKeyHandler, true);
                    }
                    
                    // Track if our handler is active
                    window.imageOcclusionHandlerActive = true;
                    
                    // Define the keyboard handler
                    window.imageOcclusionKeyHandler = function(e) {
                        // Only handle if our handler is active
                        if (!window.imageOcclusionHandlerActive) return;
                        
                        // Check for our specific shortcuts
                        if (e.key === 'Escape') {
                            e.preventDefault();
                            e.stopPropagation();
                            e.stopImmediatePropagation();
                            if (window.chrome && window.chrome.webview) {
                                window.chrome.webview.postMessage('cancel');
                            }
                            return false;
                        }
                        else if (e.ctrlKey && e.shiftKey && (e.key === 'S' || e.key === 's')) {
                            e.preventDefault();
                            e.stopPropagation();
                            e.stopImmediatePropagation();
                            if (window.chrome && window.chrome.webview) {
                                window.chrome.webview.postMessage('save');
                            }
                            return false;
                        }
                        else if (e.ctrlKey && !e.shiftKey && (e.key === 'S' || e.key === 's')) {
                            e.preventDefault();
                            e.stopPropagation();
                            e.stopImmediatePropagation();
                            if (window.chrome && window.chrome.webview) {
                                window.chrome.webview.postMessage('saveExit');
                            }
                            return false;
                        }
                    };
                    
                    // Add the event listeners with capture phase to catch events early
                    document.addEventListener('keydown', window.imageOcclusionKeyHandler, true);
                    // Also add to window to ensure we catch everything
                    window.addEventListener('keydown', window.imageOcclusionKeyHandler, true);
                    
                    // Add a backup using keyup as well for better reliability
                    window.addEventListener('keyup', function(e) {
                        if (!window.imageOcclusionHandlerActive) return;
                        
                        // Handle on keyup as backup for some cases
                        if (e.key === 'Escape' || 
                            (e.ctrlKey && (e.key === 'S' || e.key === 's'))) {
                            e.preventDefault();
                            e.stopPropagation();
                        }
                    }, true);
                    
                    console.log('Image Occlusion keyboard shortcuts injected successfully');
                    
                    // Re-inject periodically to ensure persistence
                    setTimeout(function() {
                        if (window.imageOcclusionHandlerActive && 
                            window.chrome && window.chrome.webview) {
                            console.log('Keyboard shortcut handler still active');
                        }
                    }, 5000);
                })();
            ";

            try
            {
                await webView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                ShowError($"Error injecting keyboard handler: {ex.Message}");
            }
        }

        private async Task HandleKeyboardShortcutFromWebView2(string shortcut)
        {
            switch (shortcut?.ToLower())
            {
                case "cancel":
                    this.Close();
                    break;
                case "save":
                    try
                    {
                        await SaveOcclusionAsync();
                    }
                    catch (Exception ex)
                    {
                        ShowError($"save failed: {ex.Message}");
                    }
                    break;
                case "saveexit":
                    try
                    {
                        await SaveOcclusionAndExitAsync();
                    }
                    catch (Exception ex)
                    {
                        ShowError($"save and exit failed: {ex.Message}");
                    }
                    break;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await SaveOcclusionAsync();
            }
            catch (Exception ex)
            {
                ShowError($"save failed: {ex.Message}");
            }
        }

        private async void BtnSaveExit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await SaveOcclusionAndExitAsync();
            }
            catch (Exception ex)
            {
                ShowError($"save and exit failed: {ex.Message}");
            }
        }

        private Uri GetSvgEditorUri()
        {
            string appFolder = AppContext.BaseDirectory;
            return new Uri($"file:///{Path.Combine(appFolder, SvgEditorPath)}");
        }

        private string GenerateUrlParams(string backgroundFilePath, int width, int height)
        {
            var urlParams = new StringBuilder();

            AppendUrlParam(urlParams, "bkgd_url", backgroundFilePath);
            AppendUrlParam(urlParams, "dimensions", $"{width},{height}");
            AppendUrlParam(urlParams, "initFill[color]", Settings.FillColor);
            AppendUrlParam(urlParams, "initFill[opacity]", "1");
            AppendUrlParam(urlParams, "initStroke[color]", Settings.StrokeColor);
            AppendUrlParam(urlParams, "initStroke[width]", Settings.StrokeWidth);
            AppendUrlParam(urlParams, "initStroke[opacity]", "1");

            return urlParams.ToString().TrimStart('&');
        }

        private void AppendUrlParam(StringBuilder sb, string key, string value)
        {
            if (sb.Length > 0)
                sb.Append('&');
            sb.Append(Uri.EscapeDataString(key));
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(value));
        }

        private void GetImageSize(string filePath, out int width, out int height)
        {
            using Image img = Image.FromFile(filePath);
            width = img.Width;
            height = img.Height;
        }

        private async Task SetSvgInBrowserAsync(string svg)
        {
            if (!isWebViewReady) return;

            svg = svg.Replace("\r", "").Replace("\n", "").Replace("'", "\\'");
            string script = $"svgCanvas.setSvgString('{svg}')";

            try
            {
                await webView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                ShowError($"Error setting SVG in browser: {ex.Message}");
            }
        }

        private async Task<string> GetSvgFromBrowserAsync()
        {
            if (!isWebViewReady) return string.Empty;

            try
            {
                string result = await webView.CoreWebView2.ExecuteScriptAsync("svgCanvas.svgCanvasToString()");
                return JsonSerializer.Deserialize(result, JsonContext.Default.String) ?? string.Empty;
            }
            catch (Exception ex)
            {
                ShowError($"Error getting SVG from browser: {ex.Message}");
                return string.Empty;
            }
        }

        private Bitmap ConvertSvgToImage(string svg, int width, int height)
        {
            var svgDoc = SvgDocument.FromSvg<SvgDocument>(svg);
            return svgDoc.Draw(width, height);
        }

        private void CreateChunk(PngWriter pngw, string svg)
        {
            PngChunkSVGI chunk = new PngChunkSVGI(pngw.ImgInfo);
            chunk.SetSVG(svg);
            chunk.Priority = true;

            pngw.GetChunksList().Queue(chunk);
        }

        private void WriteSvgToChunk(string tmpOcclusionFilePath, string svg)
        {
            using MemoryStream memoryStream = new MemoryStream();

            using (var fileStream = File.OpenRead(tmpOcclusionFilePath))
            {
                fileStream.CopyTo(memoryStream);
            }

            memoryStream.Seek(0, SeekOrigin.Begin);

            PngReader pngr = new PngReader(memoryStream);
            PngWriter pngw = FileHelper.CreatePngWriter(tmpOcclusionFilePath, pngr.ImgInfo, true);

            pngw.CopyChunksFirst(pngr, ChunkCopyBehaviour.COPY_ALL_SAFE);

            CreateChunk(pngw, svg);

            for (int row = 0; row < pngr.ImgInfo.Rows; row++)
            {
                ImageLine l1 = pngr.ReadRow(row);
                pngw.WriteRow(l1, row);
            }

            pngw.CopyChunksLast(pngr, ChunkCopyBehaviour.COPY_ALL);

            pngr.End();
            pngw.End();
        }

        private string ReadSvgFromChunk()
        {
            try
            {
                var pngr = FileHelper.CreatePngReader(OcclusionFilePath);
                PngChunkSVGI? chunk = (PngChunkSVGI?)pngr.GetChunksList().GetById1(PngChunkSVGI.ID);
                pngr.End();
                return chunk?.GetSVG() ?? string.Empty;
            }
            catch
            {
                NativeHelper.MessageBox(
                    IntPtr.Zero,
                    "Failed to read SVG data from PNG chunk. Please ensure the occlusion file is valid.",
                    "Error - Image Occlusion Editor",
                    NativeHelper.MB_OK | NativeHelper.MB_ICONERROR);
                return string.Empty;
            }
        }

        private async Task SaveOcclusionAsync()
        {
            string tmpOcclusionFilePath = Path.GetTempFileName();
            string svg = await GetSvgFromBrowserAsync();

            if (string.IsNullOrEmpty(svg))
            {
                ShowError("Failed to get SVG data from browser.");
                return;
            }

            using (Bitmap img = ConvertSvgToImage(svg, OcclusionWidth, OcclusionHeight))
            {
                img.Save(tmpOcclusionFilePath, ImageFormat.Png);
            }

            WriteSvgToChunk(tmpOcclusionFilePath, svg);

            if (File.Exists(OcclusionFilePath))
                File.Delete(OcclusionFilePath);

            File.Move(tmpOcclusionFilePath, OcclusionFilePath);
        }

        private async Task SaveOcclusionAndExitAsync()
        {
            await SaveOcclusionAsync();
            this.Close();
        }

        private async void ShowError(string message)
        {
            // In WinUI3, we can use ContentDialog for better user experience
            try
            {
                var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog()
                {
                    Title = "Error",
                    Content = message,
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
            catch
            {
                // Fallback to debug output if dialog fails
                System.Diagnostics.Debug.WriteLine($"Error: {message}");
            }
        }
    }
}
