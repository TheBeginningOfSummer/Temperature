namespace Temperature
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            TS菜单 = new ToolStrip();
            TSB打开串口 = new ToolStripButton();
            TSB关闭串口 = new ToolStripButton();
            TSC串口 = new ToolStripComboBox();
            toolStripLabel2 = new ToolStripLabel();
            TSC波特率 = new ToolStripComboBox();
            toolStripLabel1 = new ToolStripLabel();
            TSD设置 = new ToolStripDropDownButton();
            TSM设置温度点 = new ToolStripMenuItem();
            TSB加热 = new ToolStripButton();
            TSB休眠 = new ToolStripButton();
            BTN开始 = new Button();
            GB设备状态 = new GroupBox();
            BTN设置时间 = new Button();
            BTN设置风量 = new Button();
            BTN设置温度 = new Button();
            TB时间 = new TextBox();
            TB风量 = new TextBox();
            TB温度 = new TextBox();
            LB时间 = new Label();
            LB风量 = new Label();
            LB温度 = new Label();
            LB计时 = new Label();
            BTN停止 = new Button();
            AutoTest = new System.ComponentModel.BackgroundWorker();
            TB信息 = new TextBox();
            LB自动 = new Label();
            LB标题 = new Label();
            BTN手动模式 = new Button();
            BTN自动模式 = new Button();
            GB信息 = new GroupBox();
            BTN清除 = new Button();
            BTN下一温度 = new Button();
            TS菜单.SuspendLayout();
            GB设备状态.SuspendLayout();
            GB信息.SuspendLayout();
            SuspendLayout();
            // 
            // TS菜单
            // 
            TS菜单.AutoSize = false;
            TS菜单.BackColor = Color.Transparent;
            TS菜单.GripMargin = new Padding(1);
            TS菜单.GripStyle = ToolStripGripStyle.Hidden;
            TS菜单.Items.AddRange(new ToolStripItem[] { TSB打开串口, TSB关闭串口, TSC串口, toolStripLabel2, TSC波特率, toolStripLabel1, TSD设置, TSB加热, TSB休眠 });
            TS菜单.Location = new Point(0, 0);
            TS菜单.Margin = new Padding(1);
            TS菜单.Name = "TS菜单";
            TS菜单.Size = new Size(884, 32);
            TS菜单.TabIndex = 0;
            TS菜单.Text = "toolStrip1";
            // 
            // TSB打开串口
            // 
            TSB打开串口.DisplayStyle = ToolStripItemDisplayStyle.Text;
            TSB打开串口.Image = (Image)resources.GetObject("TSB打开串口.Image");
            TSB打开串口.ImageTransparentColor = Color.Magenta;
            TSB打开串口.Margin = new Padding(5, 1, 0, 2);
            TSB打开串口.Name = "TSB打开串口";
            TSB打开串口.Size = new Size(36, 29);
            TSB打开串口.Text = "打开";
            TSB打开串口.Click += TSB打开串口_Click;
            // 
            // TSB关闭串口
            // 
            TSB关闭串口.DisplayStyle = ToolStripItemDisplayStyle.Text;
            TSB关闭串口.Image = (Image)resources.GetObject("TSB关闭串口.Image");
            TSB关闭串口.ImageTransparentColor = Color.Magenta;
            TSB关闭串口.Name = "TSB关闭串口";
            TSB关闭串口.Size = new Size(36, 29);
            TSB关闭串口.Text = "关闭";
            TSB关闭串口.Click += TSB关闭串口_Click;
            // 
            // TSC串口
            // 
            TSC串口.Alignment = ToolStripItemAlignment.Right;
            TSC串口.Items.AddRange(new object[] { "COM1", "COM2", "COM3" });
            TSC串口.Name = "TSC串口";
            TSC串口.Size = new Size(75, 32);
            // 
            // toolStripLabel2
            // 
            toolStripLabel2.Alignment = ToolStripItemAlignment.Right;
            toolStripLabel2.Name = "toolStripLabel2";
            toolStripLabel2.Size = new Size(32, 29);
            toolStripLabel2.Text = "串口";
            // 
            // TSC波特率
            // 
            TSC波特率.Alignment = ToolStripItemAlignment.Right;
            TSC波特率.Items.AddRange(new object[] { "38400" });
            TSC波特率.Name = "TSC波特率";
            TSC波特率.Size = new Size(75, 32);
            // 
            // toolStripLabel1
            // 
            toolStripLabel1.Alignment = ToolStripItemAlignment.Right;
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new Size(44, 29);
            toolStripLabel1.Text = "波特率";
            // 
            // TSD设置
            // 
            TSD设置.DisplayStyle = ToolStripItemDisplayStyle.Text;
            TSD设置.DropDownItems.AddRange(new ToolStripItem[] { TSM设置温度点 });
            TSD设置.Image = (Image)resources.GetObject("TSD设置.Image");
            TSD设置.ImageTransparentColor = Color.Magenta;
            TSD设置.Name = "TSD设置";
            TSD设置.Size = new Size(45, 29);
            TSD设置.Text = "设置";
            // 
            // TSM设置温度点
            // 
            TSM设置温度点.Name = "TSM设置温度点";
            TSM设置温度点.Size = new Size(136, 22);
            TSM设置温度点.Text = "设置温度点";
            TSM设置温度点.Click += TSM设置温度点_Click;
            // 
            // TSB加热
            // 
            TSB加热.DisplayStyle = ToolStripItemDisplayStyle.Text;
            TSB加热.Image = (Image)resources.GetObject("TSB加热.Image");
            TSB加热.ImageTransparentColor = Color.Magenta;
            TSB加热.Name = "TSB加热";
            TSB加热.Size = new Size(36, 29);
            TSB加热.Text = "加热";
            TSB加热.Click += TSB加热_Click;
            // 
            // TSB休眠
            // 
            TSB休眠.DisplayStyle = ToolStripItemDisplayStyle.Text;
            TSB休眠.Image = (Image)resources.GetObject("TSB休眠.Image");
            TSB休眠.ImageTransparentColor = Color.Magenta;
            TSB休眠.Name = "TSB休眠";
            TSB休眠.Size = new Size(36, 29);
            TSB休眠.Text = "休眠";
            TSB休眠.Click += TSB休眠_Click;
            // 
            // BTN开始
            // 
            BTN开始.Anchor = AnchorStyles.Bottom;
            BTN开始.Location = new Point(400, 459);
            BTN开始.Margin = new Padding(20, 3, 20, 3);
            BTN开始.Name = "BTN开始";
            BTN开始.Size = new Size(100, 40);
            BTN开始.TabIndex = 1;
            BTN开始.Text = "开始";
            BTN开始.UseVisualStyleBackColor = true;
            BTN开始.Click += BTN开始_Click;
            // 
            // GB设备状态
            // 
            GB设备状态.Controls.Add(BTN设置时间);
            GB设备状态.Controls.Add(BTN设置风量);
            GB设备状态.Controls.Add(BTN设置温度);
            GB设备状态.Controls.Add(TB时间);
            GB设备状态.Controls.Add(TB风量);
            GB设备状态.Controls.Add(TB温度);
            GB设备状态.Controls.Add(LB时间);
            GB设备状态.Controls.Add(LB风量);
            GB设备状态.Controls.Add(LB温度);
            GB设备状态.Location = new Point(12, 95);
            GB设备状态.Name = "GB设备状态";
            GB设备状态.Size = new Size(289, 172);
            GB设备状态.TabIndex = 2;
            GB设备状态.TabStop = false;
            GB设备状态.Text = "设备状态";
            // 
            // BTN设置时间
            // 
            BTN设置时间.Location = new Point(186, 97);
            BTN设置时间.Name = "BTN设置时间";
            BTN设置时间.Size = new Size(69, 23);
            BTN设置时间.TabIndex = 8;
            BTN设置时间.Text = "设置";
            BTN设置时间.UseVisualStyleBackColor = true;
            BTN设置时间.Click += BTN设置时间_Click;
            // 
            // BTN设置风量
            // 
            BTN设置风量.Location = new Point(186, 61);
            BTN设置风量.Name = "BTN设置风量";
            BTN设置风量.Size = new Size(69, 23);
            BTN设置风量.TabIndex = 7;
            BTN设置风量.Text = "设置";
            BTN设置风量.UseVisualStyleBackColor = true;
            BTN设置风量.Click += BTN设置风量_Click;
            // 
            // BTN设置温度
            // 
            BTN设置温度.Location = new Point(186, 28);
            BTN设置温度.Name = "BTN设置温度";
            BTN设置温度.Size = new Size(69, 23);
            BTN设置温度.TabIndex = 6;
            BTN设置温度.Text = "设置";
            BTN设置温度.UseVisualStyleBackColor = true;
            BTN设置温度.Click += BTN设置温度_Click;
            // 
            // TB时间
            // 
            TB时间.Location = new Point(125, 97);
            TB时间.Name = "TB时间";
            TB时间.Size = new Size(55, 23);
            TB时间.TabIndex = 5;
            TB时间.Text = "60";
            // 
            // TB风量
            // 
            TB风量.Location = new Point(125, 61);
            TB风量.Name = "TB风量";
            TB风量.Size = new Size(55, 23);
            TB风量.TabIndex = 4;
            TB风量.Text = "60";
            // 
            // TB温度
            // 
            TB温度.Location = new Point(125, 28);
            TB温度.Name = "TB温度";
            TB温度.Size = new Size(55, 23);
            TB温度.TabIndex = 3;
            TB温度.Text = "200";
            // 
            // LB时间
            // 
            LB时间.AutoSize = true;
            LB时间.Location = new Point(18, 103);
            LB时间.Name = "LB时间";
            LB时间.Size = new Size(32, 17);
            LB时间.TabIndex = 2;
            LB时间.Text = "时间";
            // 
            // LB风量
            // 
            LB风量.AutoSize = true;
            LB风量.Location = new Point(18, 67);
            LB风量.Name = "LB风量";
            LB风量.Size = new Size(32, 17);
            LB风量.TabIndex = 1;
            LB风量.Text = "风量";
            // 
            // LB温度
            // 
            LB温度.AutoSize = true;
            LB温度.Location = new Point(18, 34);
            LB温度.Name = "LB温度";
            LB温度.Size = new Size(32, 17);
            LB温度.TabIndex = 0;
            LB温度.Text = "温度";
            // 
            // LB计时
            // 
            LB计时.AutoSize = true;
            LB计时.Font = new Font("Microsoft YaHei UI", 20F, FontStyle.Bold);
            LB计时.Location = new Point(12, 270);
            LB计时.Name = "LB计时";
            LB计时.Size = new Size(69, 36);
            LB计时.TabIndex = 4;
            LB计时.Text = "计时";
            // 
            // BTN停止
            // 
            BTN停止.Anchor = AnchorStyles.Bottom;
            BTN停止.Location = new Point(680, 459);
            BTN停止.Margin = new Padding(20, 3, 20, 3);
            BTN停止.Name = "BTN停止";
            BTN停止.Size = new Size(100, 40);
            BTN停止.TabIndex = 6;
            BTN停止.Text = "停止";
            BTN停止.UseVisualStyleBackColor = true;
            BTN停止.Click += BTN停止_Click;
            // 
            // TB信息
            // 
            TB信息.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            TB信息.Location = new Point(3, 19);
            TB信息.Multiline = true;
            TB信息.Name = "TB信息";
            TB信息.ReadOnly = true;
            TB信息.ScrollBars = ScrollBars.Both;
            TB信息.Size = new Size(234, 280);
            TB信息.TabIndex = 8;
            // 
            // LB自动
            // 
            LB自动.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            LB自动.AutoSize = true;
            LB自动.Location = new Point(12, 485);
            LB自动.Name = "LB自动";
            LB自动.Size = new Size(32, 17);
            LB自动.TabIndex = 9;
            LB自动.Text = "自动";
            // 
            // LB标题
            // 
            LB标题.Anchor = AnchorStyles.Top;
            LB标题.AutoSize = true;
            LB标题.Font = new Font("Times New Roman", 20F, FontStyle.Bold);
            LB标题.Location = new Point(328, 49);
            LB标题.Name = "LB标题";
            LB标题.Size = new Size(188, 31);
            LB标题.TabIndex = 10;
            LB标题.Text = "温度自动控制";
            // 
            // BTN手动模式
            // 
            BTN手动模式.Anchor = AnchorStyles.Bottom;
            BTN手动模式.Location = new Point(120, 459);
            BTN手动模式.Margin = new Padding(20, 3, 20, 3);
            BTN手动模式.Name = "BTN手动模式";
            BTN手动模式.Size = new Size(100, 40);
            BTN手动模式.TabIndex = 11;
            BTN手动模式.Text = "手动模式";
            BTN手动模式.UseVisualStyleBackColor = true;
            BTN手动模式.Click += BTN手动模式_Click;
            // 
            // BTN自动模式
            // 
            BTN自动模式.Anchor = AnchorStyles.Bottom;
            BTN自动模式.Location = new Point(260, 459);
            BTN自动模式.Margin = new Padding(20, 3, 20, 3);
            BTN自动模式.Name = "BTN自动模式";
            BTN自动模式.Size = new Size(100, 40);
            BTN自动模式.TabIndex = 12;
            BTN自动模式.Text = "自动模式";
            BTN自动模式.UseVisualStyleBackColor = true;
            BTN自动模式.Click += BTN自动模式_Click;
            // 
            // GB信息
            // 
            GB信息.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            GB信息.Controls.Add(BTN清除);
            GB信息.Controls.Add(TB信息);
            GB信息.Location = new Point(632, 95);
            GB信息.Name = "GB信息";
            GB信息.Size = new Size(240, 341);
            GB信息.TabIndex = 13;
            GB信息.TabStop = false;
            GB信息.Text = "信息";
            // 
            // BTN清除
            // 
            BTN清除.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            BTN清除.Location = new Point(6, 305);
            BTN清除.Name = "BTN清除";
            BTN清除.Size = new Size(75, 30);
            BTN清除.TabIndex = 9;
            BTN清除.Text = "清除";
            BTN清除.UseVisualStyleBackColor = true;
            BTN清除.Click += BTN清除_Click;
            // 
            // BTN下一温度
            // 
            BTN下一温度.Anchor = AnchorStyles.Bottom;
            BTN下一温度.Location = new Point(540, 459);
            BTN下一温度.Margin = new Padding(20, 3, 20, 3);
            BTN下一温度.Name = "BTN下一温度";
            BTN下一温度.Size = new Size(100, 40);
            BTN下一温度.TabIndex = 14;
            BTN下一温度.Text = "暂停/继续";
            BTN下一温度.UseVisualStyleBackColor = true;
            BTN下一温度.Click += BTN暂停_下一温度_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(884, 511);
            Controls.Add(BTN下一温度);
            Controls.Add(GB信息);
            Controls.Add(BTN自动模式);
            Controls.Add(BTN手动模式);
            Controls.Add(LB标题);
            Controls.Add(LB自动);
            Controls.Add(LB计时);
            Controls.Add(BTN停止);
            Controls.Add(GB设备状态);
            Controls.Add(BTN开始);
            Controls.Add(TS菜单);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "温度自动控制";
            TS菜单.ResumeLayout(false);
            TS菜单.PerformLayout();
            GB设备状态.ResumeLayout(false);
            GB设备状态.PerformLayout();
            GB信息.ResumeLayout(false);
            GB信息.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ToolStrip TS菜单;
        private ToolStripButton TSB打开串口;
        private ToolStripButton TSB关闭串口;
        private ToolStripLabel toolStripLabel2;
        private ToolStripComboBox TSC波特率;
        private ToolStripLabel toolStripLabel1;
        private ToolStripComboBox TSC串口;
        private Button BTN开始;
        private GroupBox GB设备状态;
        private Label LB时间;
        private Label LB风量;
        private Label LB温度;
        private Label LB计时;
        private Button BTN设置时间;
        private Button BTN设置风量;
        private Button BTN设置温度;
        private TextBox TB时间;
        private TextBox TB风量;
        private TextBox TB温度;
        private ToolStripDropDownButton TSD设置;
        private ToolStripMenuItem TSM设置温度点;
        private Button BTN停止;
        private ToolStripButton TSB加热;
        private ToolStripButton TSB休眠;
        private System.ComponentModel.BackgroundWorker AutoTest;
        private TextBox TB信息;
        private Label LB自动;
        private Label LB标题;
        private Button BTN手动模式;
        private Button BTN自动模式;
        private GroupBox GB信息;
        private Button BTN下一温度;
        private Button BTN清除;
    }
}
