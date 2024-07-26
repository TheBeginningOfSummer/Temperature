using CSharpKit.FileManagement;
using Services;

namespace Temperature
{
    public partial class TempSetting : Form
    {
        public readonly Dictionary<int, Control[]> ControlList = [];
        readonly DataManager cfg = DataManager.Instance;

        public TempSetting()
        {
            InitializeComponent();
            TST温度点数.Text = cfg.Config.Load("温度点数", "10");
            InitializeControl();
        }

        #region 控件初始化
        public void InitializeControl(int row = 12, int xInitial = 15, int yInitial = 40, int horInterval = 360, int verInterval = 30)
        {
            ControlList.Clear();
            int x = xInitial;
            int y = yInitial;
            bool isHeader = true;
            int horOffset = 60;
            _ = int.TryParse(cfg.Config.Load("[温度设置]horOffset", "60"), out horOffset);
            horInterval = 360;
            _ = int.TryParse(cfg.Config.Load("[温度设置]horInterval", "360"), out horInterval);

            for (int i = 0; i < cfg.TemperatureList.Count; i++)
            {
                if (isHeader)
                {
                    AddLabel(new Point(x + horOffset, y - 30), i, $"温度℃");
                    AddLabel(new Point(x + horOffset * 2, y - 30), i, $"时间S");
                    AddLabel(new Point(x + horOffset * 3, y - 30), i, $"风量");
                    AddLabel(new Point(x + horOffset * 4, y - 30), i, $"备注");
                    isHeader = false;
                }
                AddLabel(new Point(x, y + 3), i, $"温度点{i}");
                AddTextBoxes(new Point(x + horOffset, y), i, 50, horOffset, cfg.TemperatureList[i].Temperature.ToString(), cfg.TemperatureList[i].IntervalTime.ToString(), cfg.TemperatureList[i].Air.ToString());
                AddTextBox(new Point(x + horOffset * 4, y), i);
                y += verInterval;
                if ((i + 1) % row == 0 && i != 0)//换列
                {
                    isHeader = true;
                    x += horInterval;
                    y = yInitial;
                }
            }
        }

        public void AddLabel(Point point, int index, string message)
        {
            Label label = new()
            {
                Name = $"LB{index}",
                Location = point,
                Text = message,
                AutoSize = true
            };
            PN温度点.Controls.Add(label);
        }

        public TextBox AddTextBox(Point point, int index, int width = 50, string name = "备注", string value = "")
        {
            TextBox textBox = new()
            {
                Name = $"TB{name}{index}",
                Location = new Point(point.X, point.Y),
                Text = value,
                Size = new Size(width, 24)
            };
            PN温度点.Controls.Add(textBox);
            return textBox;
        }

        public void AddTextBoxes(Point point, int index, int width = 50, int xOffset = 60, params string[] values)
        {
            TextBox[] textBoxes = new TextBox[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                TextBox textBox;
                string[] value = values[i].Split(';');
                if (value.Length == 2)
                {
                    textBox = AddTextBox(new Point(point.X + xOffset * i, point.Y), i, width, $"TB{value[0]}{index}", value[1]);
                }
                else
                {
                    textBox = AddTextBox(new Point(point.X + xOffset * i, point.Y), i, width, $"TB{index}[{i}]", values[i]);
                }
                textBoxes[i] = textBox;
            }
            ControlList.Add(index, textBoxes);
        }
        #endregion

        private void TempSetting_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void BTN保存_Click(object sender, EventArgs e)
        {
            try
            {
                for (int i = 0; i < ControlList.Count; i++)
                {
                    if (!int.TryParse(ControlList[i][0].Text, out int temp))
                    {
                        FormMethod.ShowInfoBox("温度请输入一个整数。");
                        return;
                    }
                    else
                    {
                        if (temp < 0)
                        {
                            FormMethod.ShowInfoBox("温度请输入大于0的数。");
                            return;
                        }
                    }
                    if (!int.TryParse(ControlList[i][1].Text, out int interval))
                    {
                        FormMethod.ShowInfoBox("时间请输入一个整数。");
                        return;
                    }
                    else
                    {
                        if (interval < 0)
                        {
                            FormMethod.ShowInfoBox("时间请输入大于0的数。");
                            return;
                        }
                    }
                    if (!int.TryParse(ControlList[i][2].Text, out int air))
                    {
                        FormMethod.ShowInfoBox("风量请输入一个整数。");
                        return;
                    }
                    else
                    {
                        if (air < 0)
                        {
                            FormMethod.ShowInfoBox("风量请输入大于0的数。");
                            return;
                        }
                    }
                    cfg.TemperatureList[i].Temperature = temp;
                    cfg.TemperatureList[i].IntervalTime = interval;
                    cfg.TemperatureList[i].Air = air;
                }
                JsonManager.SaveList("Config", "TemperaturePoint.json", cfg.TemperatureList);
                FormMethod.ShowInfoBox("保存完成。");
            }
            catch (Exception ex)
            {
                FormMethod.ShowErrorBox(ex.Message);
            }
        }

        private void TSB温度点个数设置_Click(object sender, EventArgs e)
        {
            if (int.TryParse(TST温度点数.Text, out var count))
            {
                if (count > 0)
                {
                    TemperaturePoint[] tempList = new TemperaturePoint[cfg.TemperatureList.Count];
                    cfg.TemperatureList.CopyTo(tempList);
                    cfg.TemperatureList.Clear();

                    DataManager.Instance.Config.Change("温度点数", count.ToString());

                    for (int i = 0; i < count; i++)
                    {
                        if (tempList.Length >= i + 1)
                        {
                            cfg.TemperatureList.Add(tempList[i]);
                        }
                        else
                        {
                            TemperaturePoint tp = new($"{i}", 100, 10, 60);
                            cfg.TemperatureList.Add(tp);
                        }
                    }

                    JsonManager.SaveList("Config", "TemperaturePoint.json", cfg.TemperatureList);
                    PN温度点.Controls.Clear();
                    InitializeControl();
                }
                else
                {
                    FormMethod.ShowInfoBox("请输入大于0的整数。");
                }
            }
            else
            {
                FormMethod.ShowInfoBox("请输入整数。");
            }
        }

    }
}
