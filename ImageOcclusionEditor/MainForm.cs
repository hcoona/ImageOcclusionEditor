using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hjg.Pngcs;
using Hjg.Pngcs.Chunks;
using Microsoft.Web.WebView2.Core;
using Svg;

namespace ImageOcclusionEditor
{
    public partial class MainForm : Form
    {
        const string SvgEditorPath = "svg-edit/svg-editor.html";

        public string OriginalSvg { get; }
        public string OcclusionFilePath { get; }
        public int OcclusionWidth { get; }
        public int OcclusionHeight { get; }

        private string BackgroundFilePath { get; set; }
        private bool isWebViewReady = false;

        // Prevent recursive updates
        private bool isUpdatingButtonSizes = false;

        public MainForm(string backgroundFilePath, string occlusionFilePath)
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            InitializeComponent();

            OcclusionFilePath = occlusionFilePath;
            OriginalSvg = ReadSvgFromChunk();

            this.BackgroundFilePath = backgroundFilePath;

            int width, height;
            GetImageSize(BackgroundFilePath, out width, out height);

            OcclusionWidth = width;
            OcclusionHeight = height;
        }

        /// <summary>
        /// Synchronizes all button sizes when any button size changes,
        /// ensuring all three buttons maintain the same width and height
        /// </summary>
        private void Button_SizeChanged(object sender, EventArgs e)
        {
            if (isUpdatingButtonSizes) return;

            isUpdatingButtonSizes = true;
            try
            {
                Size maxSize = GetMaxButtonSize();

                // Only use MinimumSize to ensure consistency, let FlowLayoutPanel handle layout naturally
                btnCancel.MinimumSize = maxSize;
                btnSave.MinimumSize = maxSize;
                btnSaveExit.MinimumSize = maxSize;

                buttonFlowPanel.PerformLayout();
            }
            finally
            {
                isUpdatingButtonSizes = false;
            }
        }

        /// <summary>
        /// Gets the maximum size among all buttons
        /// </summary>
        private Size GetMaxButtonSize()
        {
            int maxWidth = Math.Max(Math.Max(btnCancel.Width, btnSave.Width), btnSaveExit.Width);
            int maxHeight = Math.Max(Math.Max(btnCancel.Height, btnSave.Height), btnSaveExit.Height);

            // Ensure not smaller than minimum size
            maxWidth = Math.Max(maxWidth, 240);
            maxHeight = Math.Max(maxHeight, 61);

            return new Size(maxWidth, maxHeight);
        }

        private async void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                isWebViewReady = true;
                if (!String.IsNullOrWhiteSpace(OriginalSvg))
                {
                    await SetSvgInBrowserAsync(OriginalSvg);
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            if (Properties.Settings.Default.Maximized)
            {
                WindowState = FormWindowState.Maximized;
                Location = Properties.Settings.Default.Location;
                Size = Properties.Settings.Default.Size;
            }
            else if (Properties.Settings.Default.Minimized)
            {
                WindowState = FormWindowState.Minimized;
                Location = Properties.Settings.Default.Location;
                Size = Properties.Settings.Default.Size;
            }
            else
            {
                Location = Properties.Settings.Default.Location;
                Size = Properties.Settings.Default.Size;
            }

            base.OnLoad(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (WindowState == FormWindowState.Maximized)
            {
                Properties.Settings.Default.Location = RestoreBounds.Location;
                Properties.Settings.Default.Size = RestoreBounds.Size;
                Properties.Settings.Default.Maximized = true;
                Properties.Settings.Default.Minimized = false;
            }
            else if (WindowState == FormWindowState.Normal)
            {
                Properties.Settings.Default.Location = Location;
                Properties.Settings.Default.Size = Size;
                Properties.Settings.Default.Maximized = false;
                Properties.Settings.Default.Minimized = false;
            }
            else
            {
                Properties.Settings.Default.Location = RestoreBounds.Location;
                Properties.Settings.Default.Size = RestoreBounds.Size;
                Properties.Settings.Default.Maximized = false;
                Properties.Settings.Default.Minimized = true;
            }

            Properties.Settings.Default.Save();

            base.OnClosing(e);
        }

        private void RunTaskSafely(Task task, string operationName = "operation")
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted && !IsDisposed)
                {
                    try
                    {
                        this.Invoke(new Action(() =>
                        {
                            MessageBox.Show($"{operationName} failed: {t.Exception?.GetBaseException().Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }));
                    }
                    catch (ObjectDisposedException)
                    {
                        // Form disposed, ignore
                    }
                    catch (InvalidOperationException)
                    {
                        // Control handle destroyed, ignore
                    }
                }
            }, TaskScheduler.Default);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Escape))
            {
                Close();
                return true;
            }
            else if (keyData == (Keys.Control | Keys.Shift | Keys.S))
            {
                RunTaskSafely(SaveOcclusionAsync(), "save");
                return true;
            }
            else if (keyData == (Keys.Control | Keys.S))
            {
                RunTaskSafely(SaveOcclusionAndExitAsync(), "save and exit");
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private Uri GetSvgEditorUri()
        {
            string appFolder = Application.StartupPath;
            return new Uri(String.Format("file:///{0}", Path.Combine(appFolder, SvgEditorPath)));
        }

        private string GenerateUrlParams(string backgroundFilePath, int width, int height)
        {
            var urlParams = new StringBuilder();

            AppendUrlParam(urlParams, "bkgd_url", backgroundFilePath);
            AppendUrlParam(urlParams, "dimensions", String.Format("{0},{1}", width, height));
            AppendUrlParam(urlParams, "initFill[color]", Properties.Settings.Default.FillColor);
            AppendUrlParam(urlParams, "initFill[opacity]", "1");
            AppendUrlParam(urlParams, "initStroke[color]", Properties.Settings.Default.StrokeColor);
            AppendUrlParam(urlParams, "initStroke[width]", Properties.Settings.Default.StrokeWidth);
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
            using (Image img = Image.FromFile(filePath))
            {
                width = img.Width;
                height = img.Height;
            }
        }

        private async Task SetSvgInBrowserAsync(string svg)
        {
            if (!isWebViewReady) return;

            svg = svg.Replace("\r", "").Replace("\n", "").Replace("'", "\\'");
            string script = String.Format("svgCanvas.setSvgString('{0}')", svg);

            try
            {
                await webView.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting SVG in browser: {ex.Message}");
            }
        }

        private async Task<string> GetSvgFromBrowserAsync()
        {
            if (!isWebViewReady) return string.Empty;

            try
            {
                string result = await webView.ExecuteScriptAsync("svgCanvas.svgCanvasToString()");
                return JsonSerializer.Deserialize<string>(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting SVG from browser: {ex.Message}");
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

        private Stream ToMemoryStream(string filePath)
        {
            MemoryStream memStream = new MemoryStream();

            using (Stream inStream = File.OpenRead(filePath))
            {
                byte[] buffer = new byte[8192];

                while (inStream.Read(buffer, 0, buffer.Length) > 0)
                    memStream.Write(buffer, 0, buffer.Length);
            }

            memStream.Seek(0, SeekOrigin.Begin);

            return memStream;
        }

        private void WriteSvgToChunk(string tmpOcclusionFilePath, string svg)
        {
            using (Stream inStream = ToMemoryStream(tmpOcclusionFilePath))
            {
                PngReader pngr = new PngReader(inStream);
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
        }

        private string ReadSvgFromChunk()
        {
            var pngr = FileHelper.CreatePngReader(OcclusionFilePath);

            PngChunkSVGI chunk = (PngChunkSVGI)pngr.GetChunksList().GetById1(PngChunkSVGI.ID);

            pngr.End();

            return chunk?.GetSVG();
        }

        private async Task SaveOcclusionAsync()
        {
            string tmpOcclusionFilePath = Path.GetTempFileName();
            string svg = await GetSvgFromBrowserAsync();

            if (string.IsNullOrEmpty(svg))
            {
                MessageBox.Show("Failed to get SVG data from browser.");
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
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            await SaveOcclusionAsync();
        }

        private async void btnSaveExit_Click(object sender, EventArgs e)
        {
            await SaveOcclusionAndExitAsync();
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ImageOcclusionEditor\\WebView2UserData");
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

                var controllerOptions = env.CreateCoreWebView2ControllerOptions();
                // Allows WinForms keyboard shortcuts (ProcessCmdKey) to work when WebView2 has focus.
                controllerOptions.AllowHostInputProcessing = true;

                await webView.EnsureCoreWebView2Async(env, controllerOptions);

                webView.CoreWebView2.Settings.AreHostObjectsAllowed = true;
                webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                webView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;

                webView.NavigationCompleted += WebView_NavigationCompleted;

                int width, height;
                GetImageSize(BackgroundFilePath, out width, out height);

                var path = String.Format("{0}?{1}", GetSvgEditorUri(), GenerateUrlParams(BackgroundFilePath, width, height));
                webView.CoreWebView2.Navigate(path.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing WebView2: {ex.Message}");
            }
        }
    }
}
