using CSharpKit;
using CSharpKit.Communication;
using CSharpKit.DataManagement;
using CSharpKit.FileManagement;
using Temperature;

namespace Services
{
    public class WorkUnit
    {
        #region 组件
        readonly DataManager cfg = DataManager.Instance;
        public readonly SerialPortTool SerialPort = new();
        public readonly TimerToolkit TaskTimer = new();
        public readonly AutoResetEvent Manual = new(false);
        #endregion

        public Action<int>? WorkDone;

        bool isStop = false;//测试结束
        public bool IsStop
        {
            get { return isStop; }
            set
            {
                isStop = value;
                if (isStop)
                    TaskTimer.Reset();
            }
        }

        bool isSuspend = false;
        public bool IsSuspend
        {
            get { return isSuspend; }
            set { isSuspend = value; }
        }

        public WorkUnit()
        {
            Task.Run(UpdateDeviceState);//设备信息更新
        }

        #region 通信方法
        public void UpdateDeviceState()
        {
            byte[] readTem = GetSendBytes("14670402");
            byte[] readAir = GetSendBytes("14670601");
            byte[] readTime = GetSendBytes("14670702");
            while (true)
            {
                try
                {
                    lock (SerialPort.MySerialPort)
                    {
                        if (SerialPort.MySerialPort.IsOpen)
                        {
                            Thread.Sleep(20);
                            SerialPort.MySerialPort.Write(readTem, 0, readTem.Length);
                            Thread.Sleep(20);
                            SerialPort.MySerialPort.Write(readAir, 0, readAir.Length);
                            Thread.Sleep(20);
                            SerialPort.MySerialPort.Write(readTime, 0, readTime.Length);
                            Thread.Sleep(20);
                        }
                        else
                        {
                            Thread.Sleep(500);
                        }
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        public static byte[] GetSendBytes(string code, byte[]? data = null)
        {
            byte[] sendBytes = DataConverter.HexStringToBytes(code);
            if (data != null)
                sendBytes = BytesTool.SpliceBytes(sendBytes, data);
            byte[] crcBytes = CRC16.CRC16_3(sendBytes);
            sendBytes = BytesTool.SpliceBytes(sendBytes, crcBytes);
            return sendBytes;
        }

        public bool SendCommand(string header, byte[] data)
        {
            try
            {
                byte[] bytes = GetSendBytes(header, data);
                lock (SerialPort.MySerialPort)
                    SerialPort.MySerialPort.Write(bytes, 0, bytes.Length);
                return true;
            }
            catch (Exception e)
            {
                NotifyRecord.Record($"指令发送失败。{e.Message}", NotifyRecord.LogType.Error);
                return false;
            }
        }
        #endregion

        #region 工作方法
        private static void Delay()
        {
            Thread.Sleep(20);
        }

        public bool DoWork(int index = 0)
        {
            Delay();
            byte[] t = BitConverter.GetBytes((short)cfg.TemperatureList[index].Temperature);//温度发送
            if (!SendCommand("14680902", t)) return false;
            Delay();
            if (!SendCommand("14680B01", [(byte)cfg.TemperatureList[index].Air])) return false;//风量发送
            WorkDone?.Invoke(index);
            return true;
        }

        public void AutoDo()
        {
            if (cfg.TemperatureList.Count == 0)
            {
                FormMethod.ShowInfoBox("请添加至少一个温度点。");
                return;
            }
            IsStop = false;
            IsSuspend = false;

            if (!SendCommand("14680201", [0x01])) return;//开始加热
            if (!DoWork()) return;
            int time = cfg.TemperatureList[0].IntervalTime;
            TaskTimer.Start();
            for (int i = 1; i < cfg.TemperatureList.Count;)
            {
                Thread.Sleep(200);
                if (TaskTimer.CurrentCount >= time)
                {
                    if (!DoWork(i)) return;
                    time += cfg.TemperatureList[i].IntervalTime;
                    i++;
                }
                if (IsStop) return;
            }
            Thread.Sleep(cfg.TemperatureList[^1].IntervalTime * 1000);
        }

        public void ManualDo()
        {
            if (cfg.TemperatureList.Count == 0)
            {
                FormMethod.ShowInfoBox("请添加至少一个温度点。");
                return;
            }
            IsStop = false;
            IsSuspend = false;
            Manual.Reset();

            if (!SendCommand("14680201", [0x01])) return;//开始加热
            TaskTimer.Start();
            for (int i = 0; i < cfg.TemperatureList.Count; i++)
            {
                if (!DoWork(i)) return;
                Manual.WaitOne();
                if (IsStop) break;
            }
        }
        #endregion
    }
}
