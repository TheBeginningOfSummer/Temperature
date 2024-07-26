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
                    FormMethod.OnThread(LB�Զ�, () => LB�Զ�.Text = $"�Զ�ģʽ");
                else 
                    FormMethod.OnThread(LB�Զ�, () => LB�Զ�.Text = $"�ֶ�ģʽ");
            }
        }

        public Form1()
        {
            InitializeComponent();

            TSC������.Text = "38400";
            TSC����.Text = "COM3";

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
            FormMethod.OnThread(TB��Ϣ, () => TB��Ϣ.Clear());
            if (IsAuto)
                unit.AutoDo();
            else
                unit.ManualDo();
        }

        private void AutoTest_RunWorkerCompleted(object? sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            unit.SendCommand("14680201", [0x03]);//����
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

        #region ����
        private void CountTime(int time)
        {
            FormMethod.OnThread(LB��ʱ, () => LB��ʱ.Text = $"��ʱ��{time}S");
        }

        private void ShowMessage(string message)
        {
            FormMethod.OnThread(TB��Ϣ, () =>
            {
                TB��Ϣ.AppendText($"[{DateTime.Now:G}]{Environment.NewLine}{message}{Environment.NewLine}");
            });
        }

        private void WorkingInfo(int index)
        {
            FormMethod.OnThread(TB��Ϣ, () =>
            {
                TB��Ϣ.AppendText($"[{DateTime.Now:G}]{Environment.NewLine}����{unit.TaskTimer.CurrentCount}S����{index}�η����¶ȣ�{cfg.TemperatureList[index].Temperature}��{Environment.NewLine}");
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
                                FormMethod.OnThread(LB�¶�, () => LB�¶�.Text = $"�¶ȣ�{DataConverter.BytesToInt(data, true)}");
                                break;
                            case 0x06:
                                if (data.Length != 1) break;
                                FormMethod.OnThread(LB����, () => LB����.Text = $"������{DataConverter.BytesToInt(data, true)}");
                                break;
                            case 0x07:
                                if (data.Length != 2) break;
                                FormMethod.OnThread(LBʱ��, () => LBʱ��.Text = $"ʱ�䣺{DataConverter.BytesToInt(data, true)}");
                                break;
                        }
                    }
            }
        }
        #endregion

        private void BTN���_Click(object sender, EventArgs e)
        {
            FormMethod.OnThread(TB��Ϣ, () => TB��Ϣ.Clear());
        }

        private void TSB�򿪴���_Click(object sender, EventArgs e)
        {
            string port = TSC����.Text.Trim();
            if (!int.TryParse(TSC������.Text, out int baudRate))
            {
                MessageBox.Show("���������ò���ȷ��", "����");
                return;
            }
            if (unit.SerialPort.OpenMySerialPort(baudRate, port, 8, Parity.None, StopBits.One))
            {
                MessageBox.Show("�򿪳ɹ�", "����");
            }
            else
            {
                MessageBox.Show("��ʧ��", "����");
            }
        }

        private void TSB�رմ���_Click(object sender, EventArgs e)
        {
            unit.SerialPort.CloseMySerialPort();
        }

        private void BTN�����¶�_Click(object sender, EventArgs e)
        {
            if (int.TryParse(TB�¶�.Text, out var value) && value >= 100 && value <= 500)
            {
                unit.SendCommand("14680902", BitConverter.GetBytes((short)value));
            }
            else
            {
                FormMethod.ShowErrorBox("�������ݲ���ȷ��100-500");
            }
        }

        private void BTN���÷���_Click(object sender, EventArgs e)
        {
            if (int.TryParse(TB����.Text, out var value) && value >= 0 && value <= 100)
            {
                unit.SendCommand("14680B01", [(byte)value]);
            }
            else
            {
                FormMethod.ShowErrorBox("�������ݲ���ȷ��0-100");
            }
        }

        private void BTN����ʱ��_Click(object sender, EventArgs e)
        {
            if (int.TryParse(TBʱ��.Text, out var value) && value >= 5 && value <= 999)
            {
                unit.SendCommand("14680C02", BitConverter.GetBytes((short)value));
            }
            else
            {
                FormMethod.ShowErrorBox("�������ݲ���ȷ��5-999");
            }
        }

        private void TSM�����¶ȵ�_Click(object sender, EventArgs e)
        {
            setting.Show();
        }

        private void TSB����_Click(object sender, EventArgs e)
        {
            unit.SendCommand("14680201", [0x01]);
        }

        private void TSB����_Click(object sender, EventArgs e)
        {
            unit.SendCommand("14680201", [0x03]);
        }

        private void BTN�ֶ�ģʽ_Click(object sender, EventArgs e)
        {
            if (AutoTest.IsBusy)
            {
                FormMethod.ShowInfoBox("�����С���");
                return;
            }
            var result = FormMethod.ShowQuestionBox("�Ƿ��л����ֶ�ģʽ��");
            if (result == DialogResult.Yes)
            {
                IsAuto = false;
                unit.IsStop = true;
                unit.TaskTimer.IsSuspend = false;
            }
        }

        private void BTN�Զ�ģʽ_Click(object sender, EventArgs e)
        {
            if (AutoTest.IsBusy)
            {
                FormMethod.ShowInfoBox("�����С���");
                return;
            }
            var result = FormMethod.ShowQuestionBox("�Ƿ��л����Զ�ģʽ��");
            if (result == DialogResult.Yes)
            {
                IsAuto = true;
                unit.IsStop = true;
                unit.Manual.Set();
            }
        }

        private void BTN��ʼ_Click(object sender, EventArgs e)
        {
            if (AutoTest.IsBusy)
            {
                FormMethod.ShowInfoBox("�����С���");
                return;
            }
            AutoTest.RunWorkerAsync();
        }

        private void BTN��ͣ_��һ�¶�_Click(object sender, EventArgs e)
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

        private void BTNֹͣ_Click(object sender, EventArgs e)
        {
            unit.IsStop = true;
            unit.Manual.Set();
        }

        

        
    }
}
