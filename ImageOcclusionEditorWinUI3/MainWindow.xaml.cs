/*
 * ImageOcclusionEditorWinUI3 - A WinUI 3 application for creating image occlusion cards
 * Copyright (C) 2025 Shuai Zhang
 *
 * This file contains code derived from ImageOcclusionEditor by SuperMemo Community,
 * which is licensed under the MIT License.
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Hjg.Pngcs;
using Hjg.Pngcs.Chunks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using SkiaSharp;
using Svg.Skia;

namespace ImageOcclusionEditorWinUI3
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private const string SvgEditorPath = "svg-edit/index.html";

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
        }

        private async void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ImageOcclusionEditor",
                    "WebView2UserData");

                CoreWebView2Environment env = await CoreWebView2Environment.CreateWithOptionsAsync(
                    browserExecutableFolder: null,
                    userDataFolder: userDataFolder,
                    new CoreWebView2EnvironmentOptions
                    {
                        AdditionalBrowserArguments =
                            "--disable-features=msSmartScreenProtection " +
                            "--allow-file-access-from-files",
                    });

                webView.NavigationCompleted += WebView_NavigationCompleted;
                webView.WebMessageReceived += WebView_WebMessageReceived;

                await webView.EnsureCoreWebView2Async(env);

                webView.CoreWebView2.Settings.AreHostObjectsAllowed = true;
                webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                webView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;

                GetImageSize(BackgroundFilePath, out int width, out int height);
                var targetUri = $"{GetSvgEditorUri()}?{GenerateUrlParams(BackgroundFilePath, width, height)}";

                webView.CoreWebView2.Navigate(targetUri);
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

                await SetBackgroundInBrowser(BackgroundFilePath);

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
            AppendUrlParam(urlParams, "storagePrompt", "false");

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
            using var codec = SKCodec.Create(filePath);
            width = codec.Info.Width;
            height = codec.Info.Height;
        }

        private async Task SetBackgroundInBrowser(string backgroundFilePath)
        {
            if (!isWebViewReady) return;

            string script = $"svgEditor.setBackground(\"\", \"{new Uri(backgroundFilePath).AbsoluteUri}\")";
            try
            {
                await webView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                ShowError($"Error setting background in browser: {ex.Message}");
            }
        }

        private async Task SetSvgInBrowserAsync(string svg)
        {
            if (!isWebViewReady) return;

            svg = svg.Replace("\r", "").Replace("\n", "").Replace("'", "\\'");
            string script = $"svgEditor.loadSvgString('{svg}')";

            try
            {
                string result = await webView.CoreWebView2.ExecuteScriptAsync(script);
                if (result == "false")
                {
                    ShowError($"Failed to set SVG in browser!");
                }
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
                string result = await webView.CoreWebView2.ExecuteScriptAsync("svgEditor.svgCanvas.svgCanvasToString()");
                return JsonSerializer.Deserialize(result, JsonContext.Default.String) ?? string.Empty;
            }
            catch (Exception ex)
            {
                ShowError($"Error getting SVG from browser: {ex.Message}");
                return string.Empty;
            }
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

            using var sksvg = SKSvg.CreateFromSvg(svg);
            sksvg.Save(
                path: tmpOcclusionFilePath,
                background: SKColors.Transparent,
                format: SKEncodedImageFormat.Png
            );

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
                var dialog = new ContentDialog()
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
