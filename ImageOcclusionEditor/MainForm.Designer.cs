namespace ImageOcclusionEditor
{
  partial class MainForm
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.webView = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.btnSaveExit = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.bottomPanel = new System.Windows.Forms.Panel();
            this.buttonFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)(this.webView)).BeginInit();
            this.bottomPanel.SuspendLayout();
            this.buttonFlowPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // webView
            // 
            this.webView.AllowExternalDrop = false;
            this.webView.CreationProperties = null;
            this.webView.DefaultBackgroundColor = System.Drawing.Color.White;
            this.webView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webView.Location = new System.Drawing.Point(0, 0);
            this.webView.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.webView.Name = "webView";
            this.webView.Size = new System.Drawing.Size(2128, 1177);
            this.webView.TabIndex = 0;
            this.webView.ZoomFactor = 1D;
            // 
            // btnCancel
            // 
            this.btnCancel.AutoSize = true;
            this.btnCancel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnCancel.Location = new System.Drawing.Point(6, 6);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.btnCancel.MinimumSize = new System.Drawing.Size(240, 61);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.btnCancel.Size = new System.Drawing.Size(240, 61);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.TabStop = false;
            this.btnCancel.Text = "&Cancel (ESC)";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            this.btnCancel.SizeChanged += new System.EventHandler(this.Button_SizeChanged);
            // 
            // btnSave
            // 
            this.btnSave.AutoSize = true;
            this.btnSave.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnSave.Location = new System.Drawing.Point(246, 6);
            this.btnSave.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.btnSave.MinimumSize = new System.Drawing.Size(240, 61);
            this.btnSave.Name = "btnSave";
            this.btnSave.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.btnSave.Size = new System.Drawing.Size(240, 61);
            this.btnSave.TabIndex = 2;
            this.btnSave.Text = "&Save (Ctrl+Shift+S)";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            this.btnSave.SizeChanged += new System.EventHandler(this.Button_SizeChanged);
            // 
            // btnSaveExit
            // 
            this.btnSaveExit.AutoSize = true;
            this.btnSaveExit.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnSaveExit.Location = new System.Drawing.Point(498, 6);
            this.btnSaveExit.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.btnSaveExit.MinimumSize = new System.Drawing.Size(240, 61);
            this.btnSaveExit.Name = "btnSaveExit";
            this.btnSaveExit.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.btnSaveExit.Size = new System.Drawing.Size(240, 61);
            this.btnSaveExit.TabIndex = 1;
            this.btnSaveExit.Text = "Save && E&xit (Ctrl+S)";
            this.btnSaveExit.UseVisualStyleBackColor = true;
            this.btnSaveExit.Click += new System.EventHandler(this.btnSaveExit_Click);
            this.btnSaveExit.SizeChanged += new System.EventHandler(this.Button_SizeChanged);
            // 
            // bottomPanel
            // 
            this.bottomPanel.AutoSize = true;
            this.bottomPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.bottomPanel.Controls.Add(this.buttonFlowPanel);
            this.bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.bottomPanel.MinimumSize = new System.Drawing.Size(0, 85);
            this.bottomPanel.Name = "bottomPanel";
            this.bottomPanel.Padding = new System.Windows.Forms.Padding(12, 6, 12, 12);
            this.bottomPanel.TabIndex = 4;
            // 
            // buttonFlowPanel
            // 
            this.buttonFlowPanel.AutoSize = true;
            this.buttonFlowPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonFlowPanel.Controls.Add(this.btnCancel);
            this.buttonFlowPanel.Controls.Add(this.btnSave);
            this.buttonFlowPanel.Controls.Add(this.btnSaveExit);
            this.buttonFlowPanel.Dock = System.Windows.Forms.DockStyle.Right;
            this.buttonFlowPanel.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.buttonFlowPanel.Location = new System.Drawing.Point(1366, 6);
            this.buttonFlowPanel.Name = "buttonFlowPanel";
            this.buttonFlowPanel.Size = new System.Drawing.Size(750, 73);
            this.buttonFlowPanel.TabIndex = 0;
            this.buttonFlowPanel.WrapContents = false;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2128, 1257);
            this.Controls.Add(this.webView);
            this.Controls.Add(this.bottomPanel);
            this.DoubleBuffered = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.Name = "MainForm";
            this.Text = "SuperMemo Image Occlusion Editor";
            this.Load += new System.EventHandler(this.MainForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.webView)).EndInit();
            this.bottomPanel.ResumeLayout(false);
            this.bottomPanel.PerformLayout();
            this.buttonFlowPanel.ResumeLayout(false);
            this.buttonFlowPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    #endregion

    private Microsoft.Web.WebView2.WinForms.WebView2 webView;
    private System.Windows.Forms.Button btnSaveExit;
    private System.Windows.Forms.Button btnSave;
    private System.Windows.Forms.Button btnCancel;
    private System.Windows.Forms.Panel bottomPanel;
    private System.Windows.Forms.FlowLayoutPanel buttonFlowPanel;
  }
}

