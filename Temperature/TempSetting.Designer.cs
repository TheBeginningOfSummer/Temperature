namespace Temperature
{
    partial class TempSetting
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TempSetting));
            PN温度点 = new Panel();
            BTN保存 = new Button();
            TS菜单 = new ToolStrip();
            toolStripLabel1 = new ToolStripLabel();
            TST温度点数 = new ToolStripTextBox();
            TSB温度点个数设置 = new ToolStripButton();
            TS菜单.SuspendLayout();
            SuspendLayout();
            // 
            // PN温度点
            // 
            PN温度点.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            PN温度点.AutoScroll = true;
            PN温度点.Location = new Point(12, 28);
            PN温度点.Name = "PN温度点";
            PN温度点.Size = new Size(776, 392);
            PN温度点.TabIndex = 0;
            // 
            // BTN保存
            // 
            BTN保存.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            BTN保存.Location = new Point(713, 426);
            BTN保存.Name = "BTN保存";
            BTN保存.Size = new Size(75, 23);
            BTN保存.TabIndex = 1;
            BTN保存.Text = "保存";
            BTN保存.UseVisualStyleBackColor = true;
            BTN保存.Click += BTN保存_Click;
            // 
            // TS菜单
            // 
            TS菜单.GripStyle = ToolStripGripStyle.Hidden;
            TS菜单.Items.AddRange(new ToolStripItem[] { toolStripLabel1, TST温度点数, TSB温度点个数设置 });
            TS菜单.Location = new Point(0, 0);
            TS菜单.Name = "TS菜单";
            TS菜单.Size = new Size(800, 25);
            TS菜单.TabIndex = 32;
            TS菜单.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            toolStripLabel1.Margin = new Padding(5, 1, 0, 2);
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new Size(68, 22);
            toolStripLabel1.Text = "温度点数：";
            // 
            // TST温度点数
            // 
            TST温度点数.Name = "TST温度点数";
            TST温度点数.Size = new Size(80, 25);
            TST温度点数.Text = "1";
            // 
            // TSB温度点个数设置
            // 
            TSB温度点个数设置.DisplayStyle = ToolStripItemDisplayStyle.Text;
            TSB温度点个数设置.Image = (Image)resources.GetObject("TSB温度点个数设置.Image");
            TSB温度点个数设置.ImageTransparentColor = Color.Magenta;
            TSB温度点个数设置.Name = "TSB温度点个数设置";
            TSB温度点个数设置.Size = new Size(36, 22);
            TSB温度点个数设置.Text = "设置";
            TSB温度点个数设置.Click += TSB温度点个数设置_Click;
            // 
            // TempSetting
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 461);
            Controls.Add(TS菜单);
            Controls.Add(BTN保存);
            Controls.Add(PN温度点);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "TempSetting";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "温度点设置";
            FormClosing += TempSetting_FormClosing;
            TS菜单.ResumeLayout(false);
            TS菜单.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel PN温度点;
        private Button BTN保存;
        private ToolStrip TS菜单;
        private ToolStripButton TSB温度点个数设置;
        private ToolStripLabel toolStripLabel1;
        private ToolStripTextBox TST温度点数;
    }
}