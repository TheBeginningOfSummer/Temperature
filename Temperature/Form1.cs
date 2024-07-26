using CSharpKit.DataManagement;
using CSharpKit.FileManagement;
using Services;
using System.IO.Ports;

namespace Temperature
{
    public partial class Form1 : Form
    {
        readonly DataManager cfg = DataManager.Instance;
        readonly TempSetting setting = new();
        readonly WorkUnit unit = new();

        bool isAuto = false;
        bool IsAuto
        {
            get { return isAuto; }
            set
            {
                isAuto = value;
                if (isAuto) 
                    FormMethod.OnThread(LB自动, () => LB自动.Text = $"自动模式");
                else 
                    FormMethod.OnThread(LB自动, () => LB自动.Text = $"手动模式");
            }
        }

        public Form1()
        {
            InitializeComponent();

            TSC波特率.Text = "38400";
            TSC串口.Text = "COM3";

            AutoTest.DoWork += AutoTest_DoWork;
            AutoTest.RunWorkerCompleted += AutoTest_RunWorkerCompleted;

            unit.TaskTimer.Count += CountTime;
            NotifyRecord.Notify += ShowMessage;
            unit.WorkDone += WorkingInfo;
            Task.Run(ParseMessage);
            IsAuto = false;
        }

        private void AutoTest_DoWork(object? sender, System.ComponentModel.DoWorkEventArgs e)
        {
            FormMethod.OnThread(TB信息, () => TB信息.Clear());
            if (IsAuto)
                unit.AutoDo();
            else
                unit.ManualDo();
        }

        private void AutoTest_RunWorkerCompleted(object? sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            unit.SendCommand("14680201", [0x03]);//休眠
            unit.IsStop = true;
            foreach (var textBoxes in setting.ControlList.Values)
            {
                foreach (var textBox in textBoxes)
                {
                    FormMethod.OnThread(textBox, () => textBox.BackColor = Color.White);
                    FormMethod.OnThread(textBox, () => textBox.ForeColor = Color.Black);
                }
            }
        }

        #region 方法
        private void CountTime(int time)
        {
            FormMethod.OnThread(LB计时, () => LB计时.Text = $"计时：{time}S");
        }

        private void ShowMessage(string message)
        {
            FormMethod.OnThread(TB信息, () =>
            {
                TB信息.AppendText($"[{DateTime.Now:G}]{Environment.NewLine}{message}{Environment.NewLine}");
            });
        }

        private void WorkingInfo(int index)
        {
            FormMethod.OnThread(TB信息, () =>
            {
                TB信息.AppendText($"[{DateTime.Now:G}]{Environment.NewLine}经过{unit.TaskTimer.CurrentCount}S，第{index}次发送温度：{cfg.TemperatureList[index].Temperature}℃{Environment.NewLine}");
            });
            foreach (var item in setting.ControlList[index])
            {
                FormMethod.OnThread(item, () => item.BackColor = Color.Red);
                FormMethod.OnThread(item, () => item.ForeColor = Color.White);
            }
        }

        private async void ParseMessage()
        {
            while (await unit.SerialPort.Data.Reader.WaitToReadAsync())
            {
                if (unit.SerialPort.Data.Reader.TryRead(out var message))
                    if (message.Length >= 4)
                    {
                        int length = message[3];
                        var data = message.Skip(4).Take(length).ToArray();
                        switch (message[2])
                        {
                            case 0x04:
                                if (data.Length != 2) break;
                                FormMethod.OnThread(LB温度, () => LB温度.Text = $"温度：{DataConverter.BytesToInt(data, true)}");
                                break;
                            case 0x06:
                                if (data.Length != 1) break;
                                FormMethod.OnThread(LB风量, () => LB风量.Text = $"风量：{DataConverter.BytesToInt(data, true)}");
                                break;
                            case 0x07:
                                if (data.Length != 2) break;
                                FormMethod.OnThread(LB时间, () => LB时间.Text = $"时间：{DataConverter.BytesToInt(data, true)}");
                                break;
                        }
                    }
            }
        }
        #endregion

        private void BTN清除_Click(object sender, EventArgs e)
        {
            FormMethod.OnThread(TB信息, () => TB信息.Clear());
        }

        private void TSB打开串口_Click(object sender, EventArgs e)
        {
            string port = TSC串口.Text.Trim();
            if (!int.TryParse(TSC波特率.Text, out int baudRate))
            {
                MessageBox.Show("波特率设置不正确。", "串口");
                return;
            }
            if (unit.SerialPort.OpenMySerialPort(baudRate, port, 8, Parity.None, StopBits.One))
            {
                MessageBox.Show("打开成功", "串口");
            }
            else
            {
                MessageBox.Show("打开失败", "串口");
            }
        }

        private void TSB关闭串口_Click(object sender, EventArgs e)
        {
            unit.SerialPort.CloseMySerialPort();
        }

        private void BTN设置温度_Click(object sender, EventArgs e)
        {
            if (int.TryParse(TB温度.Text, out var value) && value >= 100 && value <= 500)
            {
                unit.SendCommand("14680902", BitConverter.GetBytes((short)value));
            }
            else
            {
                FormMethod.ShowErrorBox("输入数据不正确。100-500");
            }
        }

        private void BTN设置风量_Click(object sender, EventArgs e)
        {
            if (int.TryParse(TB风量.Text, out var value) && value >= 0 && value <= 100)
            {
                unit.SendCommand("14680B01", [(byte)value]);
            }
            else
            {
                FormMethod.ShowErrorBox("输入数据不正确。0-100");
            }
        }

        private void BTN设置时间_Click(object sender, EventArgs e)
        {
            if (int.TryParse(TB时间.Text, out var value) && value >= 5 && value <= 999)
            {
                unit.SendCommand("14680C02", BitConverter.GetBytes((short)value));
            }
            else
            {
                FormMethod.ShowErrorBox("输入数据不正确。5-999");
            }
        }

        private void TSM设置温度点_Click(object sender, EventArgs e)
        {
            setting.Show();
        }

        private void TSB加热_Click(object sender, EventArgs e)
        {
            unit.SendCommand("14680201", [0x01]);
        }

        private void TSB休眠_Click(object sender, EventArgs e)
        {
            unit.SendCommand("14680201", [0x03]);
        }

        private void BTN手动模式_Click(object sender, EventArgs e)
        {
            if (AutoTest.IsBusy)
            {
                FormMethod.ShowInfoBox("运行中……");
                return;
            }
            var result = FormMethod.ShowQuestionBox("是否切换到手动模式？");
            if (result == DialogResult.Yes)
            {
                IsAuto = false;
                unit.IsStop = true;
                unit.TaskTimer.IsSuspend = false;
            }
        }

        private void BTN自动模式_Click(object sender, EventArgs e)
        {
            if (AutoTest.IsBusy)
            {
                FormMethod.ShowInfoBox("运行中……");
                return;
            }
            var result = FormMethod.ShowQuestionBox("是否切换到自动模式？");
            if (result == DialogResult.Yes)
            {
                IsAuto = true;
                unit.IsStop = true;
                unit.Manual.Set();
            }
        }

        private void BTN开始_Click(object sender, EventArgs e)
        {
            if (AutoTest.IsBusy)
            {
                FormMethod.ShowInfoBox("运行中……");
                return;
            }
            AutoTest.RunWorkerAsync();
        }

        private void BTN暂停_下一温度_Click(object sender, EventArgs e)
        {
            if (IsAuto)
            {
                unit.IsSuspend = !unit.IsSuspend;
                if (unit.IsSuspend)
                {
                    unit.TaskTimer.IsSuspend = true;
                    unit.SendCommand("14680201", [0x03]);
                }
                else
                {
                    unit.TaskTimer.IsSuspend = false;
                    unit.SendCommand("14680201", [0x01]);
                }
            }
            else
            {
                unit.Manual.Set();
            }
        }

        private void BTN停止_Click(object sender, EventArgs e)
        {
            unit.IsStop = true;
            unit.Manual.Set();
        }

        

        
    }
}
