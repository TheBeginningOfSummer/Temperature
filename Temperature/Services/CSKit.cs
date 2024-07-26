using CSharpKit.DataManagement;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace CSharpKit
{
    namespace Communication
    {
        public class GetIPAddress
        {
            public static List<IPAddress> GetIPV4AddressList()
            {
                List<IPAddress> ipAddressList = [];
                string name = Dns.GetHostName();
                IPAddress[] ipadrlist = Dns.GetHostAddresses(name);
                foreach (IPAddress ipa in ipadrlist)
                {
                    if (ipa.AddressFamily == AddressFamily.InterNetwork)
                        ipAddressList.Add(ipa);
                }
                return ipAddressList;
            }

            public static IPAddress? GetTargetIPV4Address(string partialIP)
            {
                string[] targetPart = partialIP.Split('.');
                foreach (var item in GetIPV4AddressList())
                {
                    string[] part = item.ToString().Split('.');
                    for (int i = 0; i < targetPart.Length; i++)
                    {
                        if (part[i].Equals(targetPart[i]))
                        {
                            return item;
                        }
                    }
                }
                return null;
            }
        }

        public class SocketTool(int byteLength = 4096)
        {
            //基本参数
            public Socket? SocketItem { get; set; }
            public byte[] DataCache { get; set; } = new byte[byteLength];
            //所需参数
            public Dictionary<string, Socket> ClientDic { get; set; } = [];
            public IPAddress? IP { get; set; }
            public int Port { get; set; }
            public IPEndPoint? IPEndPoint { get; set; }
            //数据收发更新委托
            public Action? ClientListUpdate;
            public Action<Socket, byte[]>? ReceiveFromClient;
            public Action<byte[]>? ReceiveFromServer;

            /// <summary>
            /// 截取特定长度的字节数组
            /// </summary>
            /// <param name="byteArr">数组</param>
            /// <param name="satrt">开始位置</param>
            /// <param name="length">截取长度</param>
            /// <returns></returns>
            private static byte[] GetByteArray(byte[] byteArr, int satrt, int length)
            {
                byte[] res = new byte[length];
                if (byteArr != null && byteArr.Length >= length)
                {
                    for (int i = 0; i < length; i++)
                    {
                        res[i] = byteArr[i + satrt];
                    }
                }
                return res;
            }

            #region 客户端
            public bool Connection(string IP, int Port, out string error)
            {
                try
                {
                    SocketItem = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    if (!IPAddress.TryParse(IP, out IPAddress? iPAddress))
                    {
                        error = "ip地址不正确";
                        return false;
                    }
                    IAsyncResult result = SocketItem.BeginConnect(iPAddress, Port, null, null);
                    var isConnect = result.AsyncWaitHandle.WaitOne(5000, true);
                    //SocketItem.Connect(iPAddress, Port);
                    if (!isConnect)
                    {
                        error = "PLC连接超时。";
                        SocketItem.Close();
                        return false;
                    }
                    SocketItem.EndConnect(result);
                    Task.Run(ReceiveData);
                }
                catch (Exception e)
                {
                    error = e.Message;
                    return false;
                }
                error = "OK";
                return true;
            }

            public bool Disconnection()
            {
                try
                {
                    if (SocketItem != null)
                    {
                        SocketItem.Shutdown(SocketShutdown.Both);
                        SocketItem.Close();
                    }
                }
                catch (Exception)
                {
                    return false;
                }
                return true;
            }

            public void ReceiveData()
            {
                while (true)
                {
                    int length = -1;
                    try
                    {
                        length = SocketItem!.Receive(DataCache);
                    }
                    catch (SocketException e)
                    {
                        if (e.ErrorCode == 10004) return;//数据接收阻塞被取消
                    }
                    catch (Exception)
                    {
                        return;
                    }
                    if (length > 0)
                    {
                        byte[] result = GetByteArray(DataCache, 0, length);//读取plc通信区的数据——写入区和数据读取区并截取
                        ReceiveFromServer?.Invoke(result);//接收数据
                    }
                    else
                    {
                        SocketItem!.Shutdown(SocketShutdown.Both);
                        SocketItem.Close();
                        return;
                    }
                    System.Threading.Thread.Sleep(10);
                }
            }
            #endregion

            #region 服务端
            public bool StartListening(IPAddress ip, int port)
            {
                try
                {
                    SocketItem = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    IPEndPoint = new IPEndPoint(ip, port);
                    SocketItem.Bind(IPEndPoint);
                    SocketItem.Listen(200);
                    ThreadPool.QueueUserWorkItem(new WaitCallback(StartAcceptClient), SocketItem);
                }
                catch (Exception)
                {
                    return false;
                }
                return true;
            }

            public bool StopListening()
            {
                try
                {
                    if (SocketItem != null)
                    {
                        SocketItem.Close();
                        ClientDic.Clear();
                    }
                }
                catch (Exception)
                {
                    return false;
                }
                return true;
            }

            public void StartAcceptClient(object? server)//会自动在其他线程上启动ReceiveData（client）方法
            {
                var socketServer = (Socket?)server;
                if (socketServer == null) return;
                while (true)
                {
                    try
                    {
                        Socket socketClient = socketServer.Accept();
                        ClientDic.Add(socketClient.RemoteEndPoint!.ToString()!, socketClient);
                        ClientListUpdate?.Invoke();//更新客户端列表//更新客户端列表
                        ThreadPool.QueueUserWorkItem(new WaitCallback(ReceiveData), socketClient);
                    }
                    catch (Exception)
                    {
                        return;
                    }
                }
            }

            public void ReceiveData(object? client)//服务端消息监听
            {
                Socket? socketClient = (Socket?)client;
                if (socketClient == null) return;
                while (true)
                {
                    int length = -1;
                    try
                    {
                        length = socketClient.Receive(DataCache)!;//plc设置的通信数据区字节数
                    }
                    catch (Exception)
                    {
                        ClientDic.Remove(socketClient.RemoteEndPoint!.ToString()!);
                        socketClient.Shutdown(SocketShutdown.Both);
                        socketClient.Close(100);
                        ClientListUpdate?.Invoke();//更新客户端列表
                        return;
                    }
                    if (length > 0)
                    {
                        byte[] result = GetByteArray(DataCache, 0, length);//读取plc通信区的数据——写入区和数据读取区并截取
                        ReceiveFromClient?.Invoke(socketClient, result);//服务端应答委托//服务端应答委托
                    }
                    else
                    {
                        ClientDic.Remove(socketClient.RemoteEndPoint!.ToString()!);
                        socketClient.Shutdown(SocketShutdown.Both);
                        socketClient.Close();
                        ClientListUpdate?.Invoke();//更新客户端列表
                        return;
                    }
                    Thread.Sleep(10);
                }
            }
            #endregion

            public void SendUTF8(string data)
            {
                SocketItem!.Send(Encoding.UTF8.GetBytes(data));//覆盖plc写入区的数据
            }
        }

        public class ModbusTCP
        {
            /// <summary>
            /// 站号
            /// </summary>
            public byte StationID { get; private set; }
            /// <summary>
            /// 数据连接
            /// </summary>
            public SocketTool Connection { get; set; }

            /// <summary>
            /// 保持寄存器区
            /// </summary>
            public byte[] HoldingRegister;
            /// <summary>
            /// 输入寄存器区
            /// </summary>
            public byte[] InputRegister;

            public ModbusTCP(byte stationID, int holdingRegister, int inputRegister)
            {
                StationID = stationID;
                HoldingRegister = new byte[holdingRegister];
                InputRegister = new byte[inputRegister];
                Connection = new SocketTool();
            }

            public ModbusTCP(byte stationID = 1)
            {
                StationID = stationID;
                HoldingRegister = new byte[20000];
                InputRegister = new byte[20000];
                Connection = new SocketTool();
                Connection.ReceiveFromClient += MessageHandling;
            }

            #region 发送请求
            /// <summary>
            /// 读取的报文
            /// </summary>
            /// <param name="stationID">站号</param>
            /// <param name="address">地址</param>
            /// <param name="amount">读取地址数</param>
            /// <param name="isLittleEndian"></param>
            /// <returns></returns>
            public static byte[] ReadDataMessage(byte stationID, byte code, ushort address, ushort amount, bool isLittleEndian = false)
            {
                byte[] requestMessage = new byte[12];
                requestMessage[5] = 0x06;
                requestMessage[6] = stationID;
                requestMessage[7] = code;
                if (isLittleEndian)
                {
                    BitConverter.GetBytes(address).CopyTo(requestMessage, 8);
                    BitConverter.GetBytes(amount).CopyTo(requestMessage, 10);
                    return requestMessage;
                }
                else
                {
                    byte[] addressBytes = BitConverter.GetBytes(address);
                    byte[] amountBytes = BitConverter.GetBytes(amount);
                    Array.Reverse(addressBytes);
                    Array.Reverse(amountBytes);
                    addressBytes.CopyTo(requestMessage, 8);
                    amountBytes.CopyTo(requestMessage, 10);
                    return requestMessage;
                }
            }
            /// <summary>
            /// 写入单个寄存器的报文
            /// </summary>
            /// <param name="stationID">站号</param>
            /// <param name="address">写入的地址</param>
            /// <param name="data">写入的数据</param>
            /// <param name="isLittleEndian"></param>
            /// <returns></returns>
            public static byte[] WriteHoldingRegisterMessage(byte stationID, ushort address, byte[] data, bool isLittleEndian = false)
            {
                byte[] requestMessage = new byte[10];
                ushort dataLength = (ushort)(4 + data.Length);
                BitConverter.GetBytes(dataLength).CopyTo(requestMessage, 4);
                requestMessage[6] = stationID;
                requestMessage[7] = 0x06;
                if (isLittleEndian)
                {
                    BitConverter.GetBytes(address).CopyTo(requestMessage, 8);
                }
                else
                {
                    byte[] addressBytes = BitConverter.GetBytes(address);
                    Array.Reverse(addressBytes);
                    addressBytes.CopyTo(requestMessage, 8);
                }
                return BytesTool.SpliceBytes(requestMessage, data);
            }
            /// <summary>
            /// 写入多个寄存器的报文
            /// </summary>
            /// <param name="stationID">站号</param>
            /// <param name="address">写入的地址</param>
            /// <param name="amount">写入的地址数</param>
            /// <param name="data">写入的数据，两个字节占一个地址</param>
            /// <param name="isLittleEndian"></param>
            /// <returns></returns>
            public static byte[] WriteHoldingRegisterMessage(byte stationID, ushort address, ushort amount, byte[] data, bool isLittleEndian = false)
            {
                byte[] requestMessage = new byte[13];
                ushort dataLength = (ushort)(7 + data.Length);
                byte[] bytesLength = BitConverter.GetBytes(dataLength);
                Array.Reverse(bytesLength);
                bytesLength.CopyTo(requestMessage, 4);
                requestMessage[6] = stationID;
                requestMessage[7] = 16;
                if (isLittleEndian)
                {
                    BitConverter.GetBytes(address).CopyTo(requestMessage, 8);
                    BitConverter.GetBytes(amount).CopyTo(requestMessage, 10);
                }
                else
                {
                    byte[] addressBytes = BitConverter.GetBytes(address);
                    byte[] amountBytes = BitConverter.GetBytes(amount);
                    Array.Reverse(addressBytes);
                    Array.Reverse(amountBytes);
                    addressBytes.CopyTo(requestMessage, 8);
                    amountBytes.CopyTo(requestMessage, 10);
                }
                requestMessage[12] = (byte)data.Length;
                return BytesTool.SpliceBytes(requestMessage, data);
            }
            /// <summary>
            /// 解析响应报文
            /// </summary>
            /// <returns>响应数据</returns>
            public static byte[]? ParseResponse(byte[] data, out ushort code)
            {
                if (data.Length < 8)
                {
                    code = 0;
                    return default;
                }
                switch (data[7])
                {
                    case 3://查看保持寄存器的数据
                        code = 3;
                        return data.Skip(9).ToArray();
                    case 4://查看输入寄存器的数据
                        code = 4;
                        return data.Skip(9).ToArray();
                    case 6://写入保持寄存器成功后返回的数据
                        code = 6;
                        return data.Skip(10).ToArray();
                    default:
                        code = 0;
                        return default;
                }
            }
            #endregion

            #region 接收请求
            #region 保持寄存器操作
            /// <summary>
            /// 读取寄存器
            /// </summary>
            /// <param name="register">指定寄存器</param>
            /// <param name="address">地址</param>
            /// <param name="amount">地址数</param>
            /// <returns></returns>
            public byte[]? ReadHoldingRegister(ushort address, ushort amount = 1)
            {
                if (address < 0 || address >= 10000) return null;
                if (10000 - address < amount) return null;
                return HoldingRegister.Skip(address * 2).Take(amount * 2).ToArray();
            }
            /// <summary>
            /// 设置寄存器值
            /// </summary>
            /// <param name="register">指定寄存器</param>
            /// <param name="inputData">数据</param>
            /// <param name="address">地址</param>
            public void SetHoldingRegister(byte[] inputData, ushort address)
            {
                if (inputData.Length < 2) return;
                inputData.CopyTo(HoldingRegister, address * 2);
            }
            /// <summary>
            /// 设置寄存器值
            /// </summary>
            /// <param name="register">指定寄存器</param>
            /// <param name="inputData">数据</param>
            /// <param name="address">地址</param>
            public void SetHoldingRegister(ushort inputData, ushort address)
            {
                byte[] data = BitConverter.GetBytes(inputData);
                Array.Reverse(data);
                data.CopyTo(HoldingRegister, address * 2);
            }
            #endregion

            #region 输入寄存器操作
            /// <summary>
            /// 读取寄存器
            /// </summary>
            /// <param name="register">指定寄存器</param>
            /// <param name="address">地址</param>
            /// <param name="amount">地址数</param>
            /// <returns></returns>
            public byte[]? ReadInputRegister(ushort address, ushort amount = 1)
            {
                return ReadRegister(InputRegister, address, amount);
            }
            /// <summary>
            /// 设置寄存器值
            /// </summary>
            /// <param name="register">指定寄存器</param>
            /// <param name="inputData">数据</param>
            /// <param name="address">地址</param>
            public void SetInputRegister(byte[] inputData, ushort address)
            {
                SetRegister(InputRegister, inputData, address);
            }
            /// <summary>
            /// 设置寄存器值
            /// </summary>
            /// <param name="register">指定寄存器</param>
            /// <param name="inputData">数据</param>
            /// <param name="address">地址</param>
            public void SetInputRegister(ushort inputData, ushort address)
            {
                SetRegister(InputRegister, inputData, address);
            }
            #endregion

            #region 寄存器操作
            /// <summary>
            /// 读取寄存器
            /// </summary>
            /// <param name="register">指定寄存器</param>
            /// <param name="address">地址</param>
            /// <param name="amount">地址数</param>
            /// <returns></returns>
            public static byte[]? ReadRegister(byte[] register, ushort address, ushort amount = 1)
            {
                if (address < 0 || address >= 10000) return null;
                if (10000 - address < amount) return null;
                return register.Skip(address * 2).Take(amount * 2).ToArray();
            }
            /// <summary>
            /// 设置寄存器值
            /// </summary>
            /// <param name="register">指定寄存器</param>
            /// <param name="inputData">数据</param>
            /// <param name="address">地址</param>
            public static void SetRegister(byte[] register, byte[] inputData, ushort address)
            {
                if (inputData.Length < 2) return;
                inputData.CopyTo(register, address * 2);
            }
            /// <summary>
            /// 设置寄存器值
            /// </summary>
            /// <param name="register">指定寄存器</param>
            /// <param name="inputData">数据</param>
            /// <param name="address">地址</param>
            public static void SetRegister(byte[] register, ushort inputData, ushort address)
            {
                byte[] data = BitConverter.GetBytes(inputData);
                Array.Reverse(data);
                data.CopyTo(register, address * 2);
            }
            #endregion

            #region 报文解析
            /// <summary>
            /// 解析请求报文头
            /// </summary>
            /// <param name="data">报文</param>
            /// <param name="address">地址</param>
            /// <param name="amount">地址数量</param>
            /// <returns>功能码以及之前的报文</returns>
            public static byte[] ParseRequestHeader(byte[] data, out ushort address, out ushort amount)
            {
                byte[] mbap = data.Take(8).ToArray();
                address = (ushort)DataConverter.TwoBytesToUInt(data.Skip(8).Take(2).ToArray());
                amount = (ushort)DataConverter.TwoBytesToUInt(data.Skip(10).Take(2).ToArray());
                return mbap;
            }
            /// <summary>
            /// 读取寄存器的返回报文
            /// </summary>
            /// <param name="data">数据</param>
            /// <param name="register">寄存器</param>
            /// <returns>响应报文</returns>
            public static byte[]? ReadResponse(byte[] data, byte[] register)
            {
                byte[] mbap = ParseRequestHeader(data, out ushort address, out ushort amount);
                //截取地址所示的数据范围
                byte[]? returnData = ReadRegister(register, address, amount);
                if (returnData == null) return default;
                byte[] responseData = new byte[returnData.Length + 1];
                responseData[0] = (byte)returnData.Length;//因为只有一个字节所以可以直接转换
                returnData.CopyTo(responseData, 1);
                //计算数据长度
                ushort dataLength = (ushort)(amount * 2 + 3);
                byte[] dataLengthBytes = BitConverter.GetBytes(dataLength);
                Array.Reverse(dataLengthBytes);
                dataLengthBytes.CopyTo(mbap, 4);
                //返回读取响应
                return BytesTool.SpliceBytes(mbap, responseData);
            }
            /// <summary>
            /// 写多个寄存器的返回报文
            /// </summary>
            /// <param name="data">报文</param>
            /// <returns>写多个寄存器的返回报文</returns>
            public static byte[] WriteResponse(byte[] data, byte[] register, bool isMultiple)
            {
                _ = ParseRequestHeader(data, out ushort address, out _);
                if (isMultiple)
                {
                    byte[] writeMultipleData = data.Skip(13).ToArray();
                    writeMultipleData.CopyTo(register, address * 2);

                    byte[] response = data.Take(12).ToArray();
                    response[4] = 0;
                    response[5] = 6;
                    return response;
                }
                else
                {
                    byte[] writeData = data.Skip(10).ToArray();
                    writeData.CopyTo(register, address * 2);
                    return data;
                }
            }

            /// <summary>
            /// 解析报文并返回响应报文
            /// </summary>
            /// <param name="data">要解析的报文</param>
            /// <returns>响应报文</returns>
            public byte[]? ParseMessage(byte[] data)
            {
                if (data.Length < 12) return null;
                switch (data[7])
                {
                    case 3://读保持寄存器
                        return ReadResponse(data, HoldingRegister);
                    case 4://读输入寄存器
                        return ReadResponse(data, InputRegister);
                    case 6://写单个保持寄存器
                        return WriteResponse(data, HoldingRegister, false);
                    case 16://写多个保持寄存器
                        return WriteResponse(data, HoldingRegister, true);
                    default:
                        return default;
                }
            }
            /// <summary>
            /// 数据接收函数
            /// </summary>
            /// <param name="client">客户端</param>
            /// <param name="data">数据</param>
            public void MessageHandling(Socket client, byte[] data)
            {
                byte[]? response = ParseMessage(data);
                if (response != null)
                    client.Send(response);
            }
            #endregion
            #endregion
        }

        /// <summary>
        /// 静态Fins工具类
        /// </summary>
        public class FinsTCP
        {
            public int PLCNode { get; set; }
            public int LocalNode { get; set; }

            public FinsTCP(int plcNode, int localNode)
            {
                PLCNode = plcNode;
                LocalNode = localNode;
            }

            public FinsTCP()
            {

            }

            /// <summary>
            /// Fins协议握手指令
            /// </summary>
            /// <param name="localAddress">本地IP最后一段</param>
            /// <returns>所需16进制字符串握手指令</returns>
            public static string HandshakeString(int localAddress)
            {
                if (localAddress >= 0 && localAddress <= 255)
                {
                    return "46494E53" + "0000000C" + "00000000" + "00000000" + "000000" + localAddress.ToString("X2");
                }
                return "";
            }
            /// <summary>
            /// 计算数据所占地址长度
            /// </summary>
            /// <param name="data"></param>
            /// <returns></returns>
            public static int CalculateDataLength(byte[] data)
            {
                int dataLength;
                if (data.Length % 2 != 0)
                    dataLength = data.Length / 2 + 1;
                else
                    dataLength = data.Length / 2;
                return dataLength;
            }
            /// <summary>
            /// Fins协议读取PLC指定内存的数据
            /// </summary>
            /// <param name="remoteAddress">PLCIP最后一段地址，16进制1字节0-255</param>
            /// <param name="localAddress">本地IP最后一段地址，16进制1字节0-255</param>
            /// <param name="memoryArea">PLC内存地址代码，16进制1字节</param>
            /// <param name="startAddress">读取数据起始地址，16进制2字节</param>
            /// <param name="dataLength">读取数据长度，16进制2字节</param>
            /// <returns>所需16进制字节读取指令</returns>
            public static byte[] ReadBytes(int remoteAddress, int localAddress, string memoryArea, int startAddress, int dataLength)
            {
                string readCommand = "";
                if (localAddress >= 0 && localAddress <= 255 && remoteAddress >= 0 && remoteAddress <= 255)
                    readCommand = "46494E53" + "0000001A" + "00000002" + "00000000" + "80" + "0002" +
                            "00" + remoteAddress.ToString("X2") + "00" + "00" + localAddress.ToString("X2") + "00" +
                            "FF0101" + memoryArea + startAddress.ToString("X4") + "00" + dataLength.ToString("X4");
                return DataConverter.HexStringToBytes(readCommand);
            }
            /// <summary>
            /// Fins协议读取PLC指定内存的数据
            /// </summary>
            /// <param name="memoryArea">PLC内存地址代码，16进制1字节</param>
            /// <param name="startAddress">读取数据起始地址，16进制2字节</param>
            /// <param name="dataLength">读取数据长度，16进制2字节</param>
            /// <returns>所需16进制字节读取指令</returns>
            public byte[] ReadBytes(string memoryArea, int startAddress, int dataLength)
            {
                return ReadBytes(PLCNode, LocalNode, memoryArea, startAddress, dataLength);
            }
            /// <summary>
            /// Fins协议写入PLC指定内存数据
            /// </summary>
            /// <param name="remoteAddress">PLCIP最后一段地址，16进制1字节</param>
            /// <param name="localAddress">本地IP最后一段地址，16进制1字节</param>
            /// <param name="memoryArea">PLC内存地址代码，16进制1字节</param>
            /// <param name="startAddress">写入数据起始地址，16进制2字节</param>
            /// <param name="data">写入的数据</param>
            /// <returns>所需的发送字节</returns>
            public static byte[] WriteBytes(int remoteAddress, int localAddress, string memoryArea, int startAddress, byte[] data)
            {
                int codeLength = 0x0000001A + data.Length;
                int addressLength = CalculateDataLength(data);
                string prefixString = "46494E53" + codeLength.ToString("X8") + "00000002" + "00000000" + "80" + "0002" +
                        "00" + remoteAddress.ToString("X2") + "00" + "00" + localAddress.ToString("X2") + "00" +
                        "FF0102" + memoryArea + startAddress.ToString("X4") + "00" + addressLength.ToString("X4");
                return BytesTool.SpliceBytes(DataConverter.HexStringToBytes(prefixString), data);
            }
            /// <summary>
            /// Fins协议写入PLC指定内存数据
            /// </summary>
            /// <param name="memoryArea">PLC内存地址代码，16进制1字节</param>
            /// <param name="startAddress">写入数据起始地址，16进制2字节</param>
            /// <param name="data">写入的数据</param>
            /// <returns>所需的发送字节</returns>
            public byte[] WriteBytes(string memoryArea, int startAddress, byte[] data)
            {
                return WriteBytes(PLCNode, LocalNode, memoryArea, startAddress, data);
            }
            /// <summary>
            /// 解析FINS协议数据头，读取信息
            /// </summary>
            /// <param name="header">FINS协议数据头</param>
            /// <param name="finsDataLength">FINS协议数据长度（字节）</param>
            /// <param name="command">FINS协议命令代码</param>
            /// <returns>是否解析成功</returns>
            public static bool ParseHeader(byte[] header, out int finsDataLength, out int command)
            {
                finsDataLength = -1;
                command = -1;
                if (header == null) return false;
                if (header.Length < 16) return false;
                if (header[0] != 0x46 || header[1] != 0x49 || header[2] != 0x4E || header[3] != 0x53) return false;
                finsDataLength = DataConverter.FourBytesToInt(BytesTool.CutBytesByLength(header, 4, 4));
                command = (int)header[11];
                return true;
            }
        }

        public class CRC16
        {
            //High-Order Byte Table
            /* Table of CRC values for high–order byte */
            static readonly byte[] auchCRCHi = [
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
            0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
            0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01,
            0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81,
            0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
            0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01,
            0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
            0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
            0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01,
            0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
            0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
            0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01,
            0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
            0x40];
            //Low-Order Byte Table
            /* Table of CRC values for low–order byte */
            static readonly byte[] auchCRCLo = new byte[256]{
            0x00, 0xC0, 0xC1, 0x01, 0xC3, 0x03, 0x02, 0xC2, 0xC6, 0x06, 0x07, 0xC7, 0x05, 0xC5, 0xC4,
            0x04, 0xCC, 0x0C, 0x0D, 0xCD, 0x0F, 0xCF, 0xCE, 0x0E, 0x0A, 0xCA, 0xCB, 0x0B, 0xC9, 0x09,
            0x08, 0xC8, 0xD8, 0x18, 0x19, 0xD9, 0x1B, 0xDB, 0xDA, 0x1A, 0x1E, 0xDE, 0xDF, 0x1F, 0xDD,
            0x1D, 0x1C, 0xDC, 0x14, 0xD4, 0xD5, 0x15, 0xD7, 0x17, 0x16, 0xD6, 0xD2, 0x12, 0x13, 0xD3,
            0x11, 0xD1, 0xD0, 0x10, 0xF0, 0x30, 0x31, 0xF1, 0x33, 0xF3, 0xF2, 0x32, 0x36, 0xF6, 0xF7,
            0x37, 0xF5, 0x35, 0x34, 0xF4, 0x3C, 0xFC, 0xFD, 0x3D, 0xFF, 0x3F, 0x3E, 0xFE, 0xFA, 0x3A,
            0x3B, 0xFB, 0x39, 0xF9, 0xF8, 0x38, 0x28, 0xE8, 0xE9, 0x29, 0xEB, 0x2B, 0x2A, 0xEA, 0xEE,
            0x2E, 0x2F, 0xEF, 0x2D, 0xED, 0xEC, 0x2C, 0xE4, 0x24, 0x25, 0xE5, 0x27, 0xE7, 0xE6, 0x26,
            0x22, 0xE2, 0xE3, 0x23, 0xE1, 0x21, 0x20, 0xE0, 0xA0, 0x60, 0x61, 0xA1, 0x63, 0xA3, 0xA2,
            0x62, 0x66, 0xA6, 0xA7, 0x67, 0xA5, 0x65, 0x64, 0xA4, 0x6C, 0xAC, 0xAD, 0x6D, 0xAF, 0x6F,
            0x6E, 0xAE, 0xAA, 0x6A, 0x6B, 0xAB, 0x69, 0xA9, 0xA8, 0x68, 0x78, 0xB8, 0xB9, 0x79, 0xBB,
            0x7B, 0x7A, 0xBA, 0xBE, 0x7E, 0x7F, 0xBF, 0x7D, 0xBD, 0xBC, 0x7C, 0xB4, 0x74, 0x75, 0xB5,
            0x77, 0xB7, 0xB6, 0x76, 0x72, 0xB2, 0xB3, 0x73, 0xB1, 0x71, 0x70, 0xB0, 0x50, 0x90, 0x91,
            0x51, 0x93, 0x53, 0x52, 0x92, 0x96, 0x56, 0x57, 0x97, 0x55, 0x95, 0x94, 0x54, 0x9C, 0x5C,
            0x5D, 0x9D, 0x5F, 0x9F, 0x9E, 0x5E, 0x5A, 0x9A, 0x9B, 0x5B, 0x99, 0x59, 0x58, 0x98, 0x88,
            0x48, 0x49, 0x89, 0x4B, 0x8B, 0x8A, 0x4A, 0x4E, 0x8E, 0x8F, 0x4F, 0x8D, 0x4D, 0x4C, 0x8C,
            0x44, 0x84, 0x85, 0x45, 0x87, 0x47, 0x46, 0x86, 0x82, 0x42, 0x43, 0x83, 0x41, 0x81, 0x80,
            0x40};

            public static byte[] CRC16_1(byte[] value)
            {
                byte uchCRCHi = 0xFF; /* 高CRC字节初始化 */
                byte uchCRCLo = 0xFF; /* 低CRC字节初始化 */
                int uIndex; /* CRC循环中的索引 */
                for (int i = 0; i < value.Length; i++)
                {
                    uIndex = uchCRCLo ^ value[i];
                    uchCRCLo = (byte)int.Parse((uchCRCHi ^ auchCRCHi[uIndex]).ToString("X"), System.Globalization.NumberStyles.HexNumber);
                    uchCRCHi = auchCRCLo[uIndex];
                }
                byte[] crcValue = new byte[value.Length + 2];
                value.CopyTo(crcValue, 0);
                crcValue[^2] = uchCRCLo;
                crcValue[crcValue.Length - 1] = uchCRCHi;
                return crcValue;
            }

            public static byte[] RFSum(byte[] value)
            {
                byte btSum = 0;
                for (int i = 0; i < value.Length; i++)
                {
                    btSum ^= value[i];
                }
                btSum ^= 0x14;
                byte[] btSumValue = new byte[value.Length + 1];
                value.CopyTo(btSumValue, 0);
                btSumValue[btSumValue.Length - 1] = btSum;
                return btSumValue;
            }

            public static byte[] CRC16_2(byte[] data)
            {
                uint crc = 0xffff;
                for (int i = 0; i < data.Length; i++)
                {
                    crc = (crc & 0xff00) + (((crc & 0xff) ^ (data[i])) & 0xff);
                    for (int kn = 0; kn < 8; kn++)
                    {
                        byte kc = (byte)(crc & 1);
                        crc >>= 1;
                        if (kc != 0) crc ^= 0xa001;
                    }
                }
                return BitConverter.GetBytes(crc).Take(2).ToArray();
            }

            public static byte[] CRC16_3(byte[] value)
            {
                byte uchCRCHi = 0xFF; /* 高CRC字节初始化 */
                byte uchCRCLo = 0xFF; /* 低CRC字节初始化 */
                int uIndex; /* CRC循环中的索引 */
                for (int i = 0; i < value.Length; i++)
                {
                    uIndex = uchCRCLo ^ value[i];
                    uchCRCLo = (byte)int.Parse((uchCRCHi ^ auchCRCHi[uIndex]).ToString("X"), System.Globalization.NumberStyles.HexNumber);
                    uchCRCHi = auchCRCLo[uIndex];
                }
                byte[] crcValue = [uchCRCLo, uchCRCHi];
                return crcValue;
            }
        }

        /// <summary>
        /// 字节工具接收类
        /// </summary>
        public class BytesReceiver
        {
            public Channel<byte[]> DataChannel = Channel.CreateUnbounded<byte[]>();
            public ChannelReader<byte[]> BytesReader { get { return DataChannel.Reader; } }
            public byte[] PackageMark;
            public int PackageMarkLength;
            public byte[] DataCache;
            public int PackageLength;
            public Action<byte[]>? ReceiveBytes;

            public BytesReceiver(byte[] packageMark, int cacheLength = 2048)
            {
                PackageMark = packageMark;
                PackageMarkLength = PackageMark.Length;
                DataCache = new byte[cacheLength];
                PackageLength = 0;
            }

            public async Task WriteDataAsync(byte[] data)
            {
                await DataChannel.Writer.WriteAsync(data);
            }

            public async Task ParseMessageAsync(Action<byte[]> parseAction)
            {
                while (await DataChannel.Reader.WaitToReadAsync())
                {
                    if (DataChannel.Reader.TryRead(out var message))
                        parseAction?.Invoke(message);
                }
            }

            /// <summary>
            /// 将接收到的字节数组按包头包尾的标记拼包与分包
            /// </summary>
            /// <param name="receivedData">接收的字节数据</param>
            public async Task DataReceiveAsync(byte[] receivedData, bool actionOrChannel = true)
            {
                if (receivedData.Length == 0) return;
                Array.Copy(receivedData, 0, DataCache, PackageLength, receivedData.Length);
                //下一次接收的起始位置以及现有的数据长度
                PackageLength += receivedData.Length;

                while (PackageLength > 0)
                {
                    //检测包头包尾
                    BytesTool.CheckPackage(DataCache, PackageMark, out int head, out int tail);
                    //无包尾,返回继续拼接
                    if (tail == -1) return;
                    //有包头且在0位置
                    if (head == 0)
                    {
                        //拼接好的数据包要放入的字节数组
                        byte[] data = new byte[tail + PackageMarkLength];
                        //将缓存中的数据拷贝到字节数组中
                        Array.Copy(DataCache, 0, data, 0, tail + PackageMarkLength);
                        if (actionOrChannel)
                            //传出数据
                            ReceiveBytes?.Invoke(data);
                        else
                            await DataChannel.Writer.WriteAsync(data);
                        //将提取的数据消除，将后面的数据前置
                        ClearDataCache(tail + PackageMarkLength);
                        //重新计算缓存区字节长度
                        PackageLength -= (tail + PackageMarkLength);
                    }
                    else
                    {
                        //将提取的数据消除，将后面的数据前置
                        ClearDataCache(tail + PackageMarkLength);
                        //重新计算缓存区字节长度
                        PackageLength -= (tail + PackageMarkLength);
                    }
                }
            }
            /// <summary>
            /// 将指定长度的数据清除，并用后面的数据覆盖
            /// </summary>
            /// <param name="clearLength">清除数据的长度</param>
            public void ClearDataCache(int clearLength)
            {
                //byte[] tempData = new byte[clearLength];
                for (int i = 0; i < DataCache.Length - clearLength; i++)
                {
                    DataCache[i] = DataCache[clearLength + i];
                }
            }

            public void ClearCache()
            {
                PackageLength = 0;
            }
        }

        public class SerialPortTool
        {
            public readonly SerialPort MySerialPort;//串口对象
            public Action<string>? ReceivedString;
            public Action<byte[]>? ReceivedByte;
            //public delegate void ShowMsgDelegate(string msg);
            //public ShowMsgDelegate SendMsg;

            int receivedByteCount = 0;
            byte reveivedByte;
            readonly byte[] dataCache;
            readonly static BoundedChannelOptions boundedOptions = new(50) { FullMode = BoundedChannelFullMode.DropOldest };
            public Channel<byte[]> Data = Channel.CreateBounded<byte[]>(boundedOptions);

            public SerialPortTool(int byteLength = 1024)
            {
                MySerialPort = new SerialPort();
                dataCache = new byte[byteLength];
            }

            private static byte[] GetByteArray(byte[] byteArr, int satrt, int length)//截取特定长度的字节数组
            {
                byte[] res = new byte[length];
                if (byteArr != null && byteArr.Length >= length)
                {
                    for (int i = 0; i < length; i++)
                    {
                        res[i] = byteArr[i + satrt];
                    }
                }
                return res;
            }

            public bool OpenMySerialPort(int iBaudRate, string portName, int dataBits, Parity iParity, StopBits iStopBits)
            {
                try
                {
                    if (MySerialPort.IsOpen)
                    {
                        MySerialPort.Close();
                    }
                    MySerialPort.BaudRate = iBaudRate;
                    MySerialPort.PortName = portName;
                    MySerialPort.DataBits = dataBits;
                    MySerialPort.Parity = iParity;
                    MySerialPort.StopBits = iStopBits;

                    MySerialPort.ReceivedBytesThreshold = 1;
                    MySerialPort.DataReceived += MySerialPortDataReceived;//绑定接收事件

                    MySerialPort.Open();
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            public bool OpenMySerialPort(int iBaudRate, string portName)
            {
                try
                {
                    if (MySerialPort.IsOpen)
                    {
                        MySerialPort.Close();
                    }
                    MySerialPort.BaudRate = iBaudRate;
                    MySerialPort.PortName = portName;
                    MySerialPort.DataBits = 8;
                    MySerialPort.Parity = Parity.None;
                    MySerialPort.StopBits = StopBits.One;

                    MySerialPort.ReceivedBytesThreshold = 1;//缓存中数据多少个时才触发DataReceived事件，默认为1
                    MySerialPort.DataReceived += MySerialPortDataReceived;//绑定接收事件

                    MySerialPort.Open();
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            private async void MySerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)//接收事件，接收到数据时触发
            {
                //接收数据
                receivedByteCount = 0;
                while (MySerialPort.BytesToRead > 0)
                {
                    reveivedByte = (byte)MySerialPort.ReadByte();
                    dataCache[receivedByteCount] = reveivedByte;
                    receivedByteCount++;

                    if (receivedByteCount >= 1024)
                    {
                        receivedByteCount = 0;
                        MySerialPort.DiscardInBuffer();
                        return;
                    }
                }

                if (receivedByteCount > 0)
                {
                    byte[] b = GetByteArray(dataCache, 0, receivedByteCount);
                    await Data.Writer.WriteAsync(b);
                    ReceivedString?.Invoke(Encoding.ASCII.GetString(b));//通过委托传出接收到的数据
                    ReceivedByte?.Invoke(b);
                }
            }

            public void SendMessage(string msg)
            {
                MySerialPort.Write(msg);
            }

            public void SendBytes(byte[] data)
            {
                MySerialPort.Write(data, 0, data.Length);
            }

            public bool CloseMySerialPort()
            {
                if (MySerialPort.IsOpen)
                {
                    MySerialPort.Close();
                }
                return true;
            }
        }
    }

    namespace DataManagement
    {
        public static class ConvertionExtensions
        {
            public static T? ConvertTo<T>(this IConvertible value) where T : struct
            {
                if (null == value) return null;
                return (T?)Convert.ChangeType(value, typeof(T));
            }

            public static object? ConvertTo(this IConvertible value, Type valueType)
            {
                if (valueType.IsGenericType && valueType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                {
                    if (value == null) return null;
                    //t = Nullable.GetUnderlyingType(t);
                    NullableConverter nullableConverter = new(valueType);
                    return Convert.ChangeType(value, nullableConverter.UnderlyingType);
                }
                return Convert.ChangeType(value, valueType);
            }
        }
        /// <summary>
        /// 静态数据转换类
        /// </summary>
        public class DataConverter
        {
            //16进制字符串转字节数组
            public static byte[] HexStringToBytes(string hexString)
            {
                hexString = hexString.Trim();
                if ((hexString.Length % 2) != 0)
                    hexString += " ";
                byte[] bytes = new byte[hexString.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = Convert.ToByte(hexString.Substring(2 * i, 2).Trim(), 16);
                }
                return bytes;
            }
            //字节数组转16进制字符串
            public static string BytesToHexString(byte[] bytes)
            {
                string hexString = "";
                if (bytes != null)
                {
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        hexString += bytes[i].ToString("X2");
                    }
                }
                return hexString;
            }
            //16进制字符串转整数字符串
            public static string HexStringToIntString(string hexString)
            {
                if (int.TryParse(hexString, System.Globalization.NumberStyles.HexNumber, null, out int result))
                {
                    return result.ToString();
                }
                return string.Empty;
            }
            //16进制字符串转整数
            public static int HexStringToInt(string hexString)
            {
                if (int.TryParse(hexString, System.Globalization.NumberStyles.HexNumber, null, out int result))
                {
                    return result;
                }
                return -1;
            }

            public static string IntStringToHexString(string intString, string hexDigits = "X2")
            {
                if (int.TryParse(intString, out int result))
                {
                    if (result < 0)
                        return "";
                    return result.ToString(hexDigits);
                }
                return "";
            }

            public static int BytesToInt(byte[] bytes, bool isLittleEndian = false)
            {
                if (!isLittleEndian) Array.Reverse(bytes);
                if (bytes.Length == 1) return bytes[0];
                if (bytes.Length == 2) return BitConverter.ToInt16(bytes, 0);
                if (bytes.Length == 4) return BitConverter.ToInt32(bytes, 0);
                return 0;
            }

            public static int TwoBytesToInt(byte[] bytes, bool isLittleEndian = false)
            {
                if (isLittleEndian)
                {
                    return BitConverter.ToInt16(bytes, 0);
                }
                else
                {
                    Array.Reverse(bytes);
                    return BitConverter.ToInt16(bytes, 0);
                }
            }

            public static int TwoBytesToUInt(byte[] bytes, bool isLittleEndian = false)
            {
                if (isLittleEndian)
                {
                    return BitConverter.ToUInt16(bytes, 0);
                }
                else
                {
                    Array.Reverse(bytes);
                    return BitConverter.ToUInt16(bytes, 0);
                }
            }

            public static int FourBytesToInt(byte[] bytes, bool isLittleEndian = false)
            {
                if (isLittleEndian)
                {
                    return BitConverter.ToInt32(bytes, 0);
                }
                else
                {
                    Array.Reverse(bytes);
                    return BitConverter.ToInt32(bytes, 0);
                }
            }
            //高低位互换
            public static string ToLowHigh(string hexString)
            {
                byte[] bytes = HexStringToBytes(hexString);
                Array.Reverse(bytes);
                return BytesToHexString(bytes);
            }

            //public static Stream BitmapToStream(Image<Rgba32> image)
            //{
            //    var stream = new MemoryStream();
            //    image.SaveAsBmp(stream);
            //    stream.Seek(0, SeekOrigin.Begin);
            //    return stream;
            //}
        }
        /// <summary>
        /// 静态字节工具类
        /// </summary>
        public class BytesTool
        {
            //==========静态函数==========//
            #region 插入与拼接
            public static byte[] SpliceBytes(byte[] beginningArray, byte[] endingArray)
            {
                byte[] bytes = new byte[beginningArray.Length + endingArray.Length];
                beginningArray.CopyTo(bytes, 0);
                endingArray.CopyTo(bytes, beginningArray.Length);
                return bytes;
            }
            /// <summary>
            /// 在insertIndex处插入一个字节数组
            /// </summary>
            /// <param name="sourceBytes">被插入的字节数组</param>
            /// <param name="insertBytes">要插入的字节数组</param>
            /// <param name="insertIndex">要插入的数组索引处</param>
            /// <returns>插入后的字节数组</returns>
            public static byte[] InsertBytes(byte[] sourceBytes, byte[] insertBytes, int insertIndex)
            {
                byte[] bytes = new byte[sourceBytes.Length + insertBytes.Length];
                if (insertIndex < 0) return sourceBytes;
                if (insertIndex > sourceBytes.Length) return sourceBytes;
                Array.ConstrainedCopy(sourceBytes, 0, bytes, 0, insertIndex);
                Array.ConstrainedCopy(insertBytes, 0, bytes, insertIndex, insertBytes.Length);
                Array.ConstrainedCopy(sourceBytes, insertIndex, bytes, insertIndex + insertBytes.Length, sourceBytes.Length - insertIndex);
                return bytes;
            }
            /// <summary>
            /// 在数组中的标记字节后插入一个数组  与插入的数组长度还无关联
            /// </summary>
            /// <param name="sourceArray">源数组</param>
            /// <param name="mark">标记字节</param>
            /// <param name="insertArray">要插入的数组</param>
            /// <returns>插入后的字节数组</returns>
            public static byte[] InsertBytesBackOfMark(byte[] sourceArray, byte mark, byte[] insertArray)
            {
                List<int> index = new List<int>();
                for (int i = 0; i < sourceArray.Length; i++)
                {
                    if (sourceArray[i] == mark)
                    {
                        index.Add(i);
                    }
                }
                if (index.Count == 0) return sourceArray;
                return LoopInsert(sourceArray, insertArray, index, index.Count);
            }

            public static byte[] LoopInsert(byte[] sourceArray, byte[] insertArray, List<int> index, int count)
            {
                if (count == 1)
                {
                    return InsertBytes(sourceArray, insertArray, index[count - 1] + count);
                }
                else
                {
                    return InsertBytes(LoopInsert(sourceArray, insertArray, index, count - 1), insertArray, index[count - 1] + count + (insertArray.Length - 1) * (count - 1));
                }
            }
            #endregion

            #region 去除与剪切
            /// <summary>
            /// 从源字节数组中剪裁出指定首尾索引中间的字节数组
            /// </summary>
            /// <param name="sourceBytes">源字节数组</param>
            /// <param name="beginningIndex">要开始剪裁的字节索引</param>
            /// <param name="endingIndex">要结束剪裁的字节索引</param>
            /// <returns>包括首尾索引字节的字节数组</returns>
            public static byte[] CutBytes(byte[] sourceBytes, int beginningIndex, int endingIndex)
            {
                if (beginningIndex < 0 || endingIndex < 0) return sourceBytes;
                if (beginningIndex >= endingIndex) return sourceBytes;
                byte[] bytes = new byte[endingIndex - beginningIndex + 1];
                Array.ConstrainedCopy(sourceBytes, beginningIndex, bytes, 0, bytes.Length);
                return bytes;
            }

            public static byte[] CutBytesByLength(byte[] sourceBytes, int beginningIndex, int dataLength)
            {
                if (beginningIndex < 0 || dataLength < 0) return sourceBytes;
                if (sourceBytes.Length - beginningIndex < dataLength) return sourceBytes;
                byte[] bytes = new byte[dataLength];
                Array.ConstrainedCopy(sourceBytes, beginningIndex, bytes, 0, dataLength);
                return bytes;
            }
            /// <summary>
            /// 去除指定索引处的字节
            /// </summary>
            /// <param name="sourceBytes"></param>
            /// <param name="redundantIndex"></param>
            /// <returns></returns>
            public static byte[] RemoveByte(byte[] sourceBytes, int redundantIndex)
            {
                if (redundantIndex > sourceBytes.Length - 1) return sourceBytes;
                byte[] bytes = new byte[sourceBytes.Length - 1];
                Array.ConstrainedCopy(sourceBytes, 0, bytes, 0, redundantIndex);
                Array.ConstrainedCopy(sourceBytes, redundantIndex + 1, bytes, redundantIndex, bytes.Length - redundantIndex);
                return bytes;
            }
            /// <summary>
            /// 移除特殊字节后的标记字节
            /// </summary>
            /// <param name="sourceArray">源字节数组</param>
            /// <param name="specialByte">特殊字节</param>
            /// <param name="mark">标记字节</param>
            public static byte[] RemoveMark(byte[] sourceArray, byte specialByte, byte mark)
            {
                List<int> index = new List<int>(sourceArray.Length);
                for (int i = 0; i < sourceArray.Length; i++)
                {
                    if (sourceArray[i] == specialByte)
                    {
                        if (i < sourceArray.Length - 1)
                        {
                            if (sourceArray[i + 1] == mark)
                                index.Add(i + 1);
                        }
                    }
                }
                if (index.Count == 0) return sourceArray;
                return LoopRemove(sourceArray, index, index.Count);
            }

            public static byte[] LoopRemove(byte[] sourceArray, List<int> index, int count)
            {
                if (count == 1)
                {
                    return RemoveByte(sourceArray, index[index.Count - count]);//从后向前减，减少代码复杂度
                }
                else
                {
                    return RemoveByte(LoopRemove(sourceArray, index, count - 1), index[index.Count - count]);
                }
            }
            #endregion

            #region 检测
            /// <summary>
            /// 检查字节数组中标记数组的数量
            /// </summary>
            /// <param name="sourceArray">源字节数组</param>
            /// <param name="frameMark">需要检查的标记字节数组</param>
            /// <returns>标记的数量</returns>
            public static int CheckFrameMarkCount(byte[] sourceArray, byte[] frameMark)
            {
                if (sourceArray.Length == 0) return 0;
                if (sourceArray.Length < frameMark.Length) return 0;
                int count = 0;
                for (int i = 0; i < sourceArray.Length; i++)
                {
                    if (sourceArray[i] == frameMark[0])
                    {
                        //如果剩余长度已经小于标记字节数组的长度，直接返回
                        if (frameMark.Length > sourceArray.Length - i) return count;
                        for (int j = 0; j < frameMark.Length; j++)
                        {
                            if (sourceArray[i + j] != frameMark[j])
                            {
                                break;
                            }
                            else
                            {
                                if (j == frameMark.Length - 1)
                                {
                                    i += j;
                                    count++;
                                }
                            }
                        }
                    }
                }
                return count;
            }
            //得到标记字节数组的第一个位置索引（头尾索引）
            public static void FindPackage(byte[] sourceArray, byte[] frameMark, out int head, out int tail)
            {
                head = -1;
                tail = -1;
                for (int i = 0; i < sourceArray.Length; i++)
                {
                    if (sourceArray[i] == frameMark[0])
                    {
                        for (int j = 0; j < frameMark.Length; j++)
                        {
                            if (head == -1)
                            {
                                if (i + j >= sourceArray.Length)
                                {
                                    head = -1;
                                    break;
                                }
                                if (sourceArray[i + j] != frameMark[j])
                                {
                                    head = -1;
                                    break;
                                }
                                else
                                {
                                    if (j == frameMark.Length - 1)
                                        head = i;
                                }
                            }
                            else
                            {
                                if (i + j >= sourceArray.Length)
                                {
                                    tail = -1;
                                    break;
                                }
                                if (sourceArray[i + j] != frameMark[j])
                                {
                                    tail = -1;
                                    break;
                                }
                                else
                                {
                                    if (j == frameMark.Length - 1)
                                    {
                                        if (i - head < 2)
                                        {
                                            tail = -1;
                                        }
                                        else
                                        {
                                            tail = i;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (head != -1 && tail != -1) break;
                }
            }
            //得到数组开头与下一个位置是标记字节的索引位置（头尾索引）
            public static void CheckPackage(byte[] sourceArray, byte[] packageMark, out int head, out int tail)
            {
                head = -1;
                tail = -1;
                if (packageMark.Length > sourceArray.Length) return;
                for (int i = 0; i < sourceArray.Length; i++)
                {
                    if (i < packageMark.Length)
                    {
                        for (int j = 0; j < packageMark.Length; j++)
                        {
                            if (i + j >= sourceArray.Length)
                            {
                                head = -1;
                                break;
                            }
                            if (head == -1)
                            {
                                if (sourceArray[i + j] != packageMark[j])
                                {
                                    head = -1;
                                    break;
                                }
                                else
                                {
                                    if (j == packageMark.Length - 1) head = i;
                                    //if (head != 0) head = -1;
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int j = 0; j < packageMark.Length; j++)
                        {
                            if (i + j >= sourceArray.Length)
                            {
                                tail = -1;
                                break;
                            }
                            if (tail == -1)
                            {
                                if (sourceArray[i + j] != packageMark[j])
                                {
                                    tail = -1;
                                    break;
                                }
                                else
                                {
                                    if (j == packageMark.Length - 1)
                                    {
                                        tail = i;
                                    }
                                }
                            }
                        }
                    }
                    if (tail != -1) break;
                }
            }
            //比较两个数组是否相等
            public static bool CheckEquals(byte[] b1, byte[] b2)
            {
                if (b1 == null || b2 == null) return false;
                if (b1.Length != b2.Length) return false;
                for (int i = 0; i < b1.Length; i++)
                {
                    if (b1[i] != b2[i]) return false;
                }
                return true;
            }
            #endregion

            /// <summary>
            /// 字节按每个字颠倒
            /// </summary>
            /// <param name="bytes">要调整的字节</param>
            /// <returns>返回的结果</returns>
            public static byte[] WordByteReverse(byte[] bytes)
            {
                if (bytes == null) return Encoding.ASCII.GetBytes("null:0");
                if (bytes.Length == 0) return Encoding.ASCII.GetBytes("null:0");
                List<byte> list = new List<byte>();
                if ((bytes.Length % 2) == 0)
                {
                    for (int i = 0; i < bytes.Length; i += 2)
                    {
                        list.Add(bytes[i + 1]);
                        list.Add(bytes[i]);
                    }
                    return list.ToArray();
                }
                else
                {
                    for (int i = 0; i < bytes.Length - 1; i += 2)
                    {
                        list.Add(bytes[i + 1]);
                        list.Add(bytes[i]);
                    }
                    list.Add(bytes.Last());
                    return list.ToArray();
                }
            }

        }
    }

    namespace FileManagement
    {
        /// <summary>
        /// 静态文件管理类
        /// </summary>
        public class FileManager
        {
            public static string GetLocalAppPath(string fileName)
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), fileName);
            }

            public static byte[] GetFileBinary(string path)
            {
                FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                byte[] data = new byte[fileStream.Length];
                fileStream.Read(data, 0, data.Length);
                fileStream.Close();
                return data;
            }

            public static Stream GetFileStream(string path, int cacheLength = 10240)
            {
                using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var buffer = new byte[cacheLength];
                int bytesRead;
                Stream stream = new MemoryStream();
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    stream.Write(buffer, 0, bytesRead);
                }
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            }

            public static async Task WriteStreamAsync(string path, string fileName, Stream message, FileMode fileMode = FileMode.OpenOrCreate)
            {
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                path += "/" + fileName;
                byte[] buffer = new byte[10240]; int length;
                using FileStream file = new FileStream(path, fileMode);
                while ((length = await message.ReadAsync(buffer)) != 0)
                    await file.WriteAsync(buffer.AsMemory(0, length));
                //await file.WriteAsync(buffer, 0, length);
            }

            public static async Task WriteStreamProgressAsync(string path, string fileName, int fileSize, Stream message, IProgress<string> progress, FileMode fileMode = FileMode.OpenOrCreate)
            {
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                path += "/" + fileName;
                byte[] buffer = new byte[10240]; int length; int progressLength = 0;
                using FileStream file = new FileStream(path, fileMode);
                while ((length = await message.ReadAsync(buffer)) != 0)
                {
                    await file.WriteAsync(buffer.AsMemory(0, length));
                    progressLength += length;
                    progress.Report($"{length * 100 / fileSize}%");
                }
            }

            public static void AppendFlieString(string path, string fileName, string message, FileMode fileMode)
            {
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                path += "/" + fileName;
                byte[] data = Encoding.UTF8.GetBytes(message);
                FileStream file = new FileStream(path, fileMode);
                file.Write(data, 0, data.Length);
                file.Flush();
                file.Close();
                file.Dispose();
            }

            public static void SetTableHeader(string path, string fileName, string tableHeader)
            {
                //using FileStream read = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                //using (StreamReader sr = new StreamReader(read))
                //{
                //    if (sr.ReadToEnd() == string.Empty)
                //    {

                //    }
                //}
                FileInfo fileInfo = new FileInfo(path + "/" + fileName);
                if (!fileInfo.Exists || fileInfo.Length == 0)
                    AppendFlieString(path, fileName, tableHeader, FileMode.Append);
            }

            public static void AppendLog(string path, string fileName, string tableHeader, string message)
            {
                string log = $"{DateTime.Now:yyy-MM-dd HH:mm:ss}\t{message}{Environment.NewLine}";
                SetTableHeader(path, fileName, tableHeader);
                AppendFlieString(path, fileName, log, FileMode.Append);
            }

            public static void AppendLog(string path, string fileName, string message)
            {
                string log = $"{DateTime.Now:yyy-MM-dd HH:mm:ss}\t{message}{Environment.NewLine}";
                AppendFlieString(path, $"{DateTime.Now:yyy-MM-dd}{fileName}.log", log, FileMode.Append);
            }
        }
        /// <summary>
        /// 静态日志表格纪录类
        /// </summary>
        public class MessageRecorder
        {
            public static readonly string AndroidPath = "/storage/emulated/0";
            public static readonly string AndroidDocumentPath = $"{AndroidPath}/Documents/Log";

            public static void RecordError(string error, string solution, string path = "Log", string fileName = "错误记录.xls")
            {
                string rowstr = error;
                if (rowstr.IndexOf('\n') > 0)
                    rowstr = rowstr.Replace("\n", " ");
                if (rowstr.IndexOf("\r\n") > 0)
                    rowstr = rowstr.Replace("\r\n", " ");
                if (rowstr.IndexOf('\t') > 0)
                    rowstr = rowstr.Replace("\t", " ");
                FileManager.AppendLog(path, $"{DateTime.Now:yyy-MM-dd}{fileName}",
                   $"日期\t错误信息\t处理方法{Environment.NewLine}", $"{rowstr}\t{solution}");
            }

            public static void RecordProduction(string[] tableName, string[] message, string path = "Log", string fileName = "生产记录.xls")
            {
                if (tableName.Length == 0 || tableName == null) return;
                if (message.Length == 0 || message == null) return;
                string header = ""; string content = "";
                for (int i = 0; i < tableName.Length; i++)
                {
                    if (i == tableName.Length - 1)
                        header += tableName[i] + Environment.NewLine;
                    else
                        header += tableName[i] + "\t";
                }
                for (int i = 0; i < message.Length; i++)
                {
                    if (i == message.Length - 1)
                        content += message[i] + Environment.NewLine;
                    else
                        content += message[i] + "\t";
                }
                FileManager.AppendLog(path, $"{DateTime.Now:yyy-MM-dd}{fileName}", header, content);
            }
        }
        /// <summary>
        /// 静态Json管理类
        /// </summary>
        public class JsonManager
        {
            public static void SaveJsonString(string path, string fileName, object data)
            {
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                path += "/" + fileName;
                string jsonString = JsonSerializer.Serialize(data);
                byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);
                FileStream file = new FileStream(path, FileMode.Create);
                file.Write(jsonBytes, 0, jsonBytes.Length);//整块写入
                file.Flush();
                file.Close();
            }

            public static T? ReadJsonString<T>(string path, string fileName)
            {
                try
                {
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    path += "/" + fileName;
                    FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    StreamReader stream = new StreamReader(file);
                    T? jsonData = JsonSerializer.Deserialize<T?>(stream.ReadToEnd());
                    file.Flush();
                    file.Close();
                    //T jsonData = JsonMapper.ToObject<T>(File.ReadAllText(path));
                    return jsonData;
                }
                catch (Exception)
                {
                    return default;
                }
            }

            //public static JsonData ReadSimpleJsonString(string path)
            //{
            //    JsonData jsonData = JsonMapper.ToObject(File.ReadAllText(path));
            //    return jsonData;
            //}

            public static void SaveDic<T>(string path, string fileName, Dictionary<string, T> data)
            {
                data ??= [];
                JsonManager.SaveJsonString(path, fileName, data);
            }

            public static Dictionary<string, T> LoadDic<T>(string path, string fileName, Dictionary<string, T>? defaultData = null)
            {
                var dic = ReadJsonString<Dictionary<string, T>>(path, fileName);
                if (dic == null)
                {
                    if (defaultData != null)
                        dic = defaultData;
                    else
                        dic = [];
                    SaveJsonString(path, fileName, dic);
                }
                return dic;
            }

            public static void SaveList<T>(string path, string fileName, List<T> data)
            {
                data ??= [];
                JsonManager.SaveJsonString(path, fileName, data);
            }

            public static List<T> LoadList<T>(string path, string fileName, List<T>? defaultData = null)
            {
                var list = ReadJsonString<List<T>>(path, fileName);
                if (list == null)
                {
                    if (defaultData != null)
                        list = defaultData;
                    else
                        list = [];
                    SaveJsonString(path, fileName, list);
                }
                return list;
            }
        }
        /// <summary>
        /// 文件配置存储类
        /// </summary>
        public class KeyValueManager
        {
            public string FileName { get; set; }
            public string ConfigurationPath { get; set; }
            public Dictionary<string, string>? KeyValueList;

            public KeyValueManager(string fileName, string path, params string[] keyValues)
            {
                FileName = fileName;
                ConfigurationPath = path;
                KeyValueList = JsonManager.ReadJsonString<Dictionary<string, string>>(ConfigurationPath, FileName);
                
                KeyValueList ??= [];
                if (keyValues.Length % 2 == 0 && keyValues.Length != 0)
                {
                    for (int i = 0; i < keyValues.Length; i += 2)
                    {
                        if (!KeyValueList.ContainsKey(keyValues[i]))
                        {
                            KeyValueList.Add(keyValues[i], keyValues[i + 1]);
                            JsonManager.SaveJsonString(ConfigurationPath, FileName, KeyValueList);
                        }
                    }
                }
            }

            public void Add(string key, string value)
            {
                KeyValueList!.Add(key, value);
                JsonManager.SaveJsonString(ConfigurationPath, FileName, KeyValueList);
            }

            public void Remove(string key)
            {
                KeyValueList!.Remove(key);
                JsonManager.SaveJsonString(ConfigurationPath, FileName, KeyValueList);
            }

            public void Change(string key, string value)
            {
                KeyValueList ??= [];
                if (KeyValueList.ContainsKey(key))
                {
                    KeyValueList[key] = value;
                    JsonManager.SaveJsonString(ConfigurationPath, FileName, KeyValueList);
                }
                else
                {
                    Add(key, value);
                }
            }

            public string Load(string key)
            {
                try
                {
                    if (KeyValueList!.TryGetValue(key, out string? value))
                        return value;
                    else
                        return "";
                }
                catch (Exception)
                {
                    return "";
                }
            }

            public T Load<T>(string key)
            {
                string value = Load(key);
                if (value == "") return default!;
                T result = (T)Convert.ChangeType(value, typeof(T));
                return result;
            }

            public string Load(string key, string defaultValue)
            {
                if (!KeyValueList!.ContainsKey(key))
                    Change(key, defaultValue);
                return KeyValueList[key];
            }
        }

        public class NotifyRecord
        {
            public enum LogType
            {
                Error, Warning, Modification, Clue
            }

            public static Action<string>? Notify;

            public NotifyRecord() { }

            public static void Record(string log, LogType logType)
            {
                switch (logType)
                {
                    case LogType.Error:
                        FileManager.AppendLog("Log\\Error", "错误记录", log);
                        Notify?.Invoke(log);
                        break;
                    case LogType.Warning:
                        FileManager.AppendLog("Log\\Warning", "报警记录", log);
                        Notify?.Invoke(log);
                        break;
                    case LogType.Modification:
                        FileManager.AppendLog("Log\\Modification", "更改记录", log);
                        Notify?.Invoke(log);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// 计时工具类
    /// </summary>
    public class TimerToolkit
    {
        #region 组件
        public AutoResetEvent CheckTime = new AutoResetEvent(false);
        //时间组件
        public System.Threading.Timer ThreadTimer;
        //计数锁
        private readonly object countLock = new object();
        #endregion

        #region 属性
        public int Mode { get; set; }

        public bool IsSuspend { get; set; } = false;
        //是否正在计时
        public bool IsTiming { get; private set; }
        //计时时间
        public int Timeout { get; set; }
        //当前时间
        private int currentCount;
        public int CurrentCount
        {
            get { return currentCount; }
            set
            {
                currentCount = value;
                if (Mode == 1)
                {
                    TimeAutoSet();
                    TimeRunsOut();
                }
            }
        }
        #endregion

        //计时
        public Action<int>? Count;
        //计时时间到
        public Action? TimesUp;

        public TimerToolkit()
        {
            ThreadTimer = new System.Threading.Timer(
                new TimerCallback(TimerUp), null, System.Threading.Timeout.Infinite, 1000);
            CurrentCount = 0;
            IsTiming = false;
        }

        #region 基础功能
        private void TimerUp(object? value)
        {
            lock (countLock)
            {
                if (!IsSuspend)
                {
                    CurrentCount += 1;
                    Count?.Invoke(CurrentCount);
                }
            }
        }

        public void Start()
        {
            ThreadTimer.Change(0, 1000);
        }

        public void Suspend()
        {
            IsSuspend = !IsSuspend;
        }

        public void Stop()
        {
            ThreadTimer.Change(System.Threading.Timeout.Infinite, 1000);
        }

        public void ClearCount()
        {
            lock (countLock)
            {
                CurrentCount = 0;
                Count?.Invoke(CurrentCount);
            }
        }

        /// <summary>
        /// 计时停止
        /// </summary>
        public void Reset()
        {
            IsTiming = false;
            Stop();
            ClearCount();
        }
        #endregion

        #region 暂停功能
        //暂停指定的时间
        public void Suspend(int timeout)
        {
            Timeout = timeout;
            Stop();
            ClearCount();
            Start();
            CheckTime.WaitOne();
            Stop();
            ClearCount();
        }
        //超时自动set，在属性变化中调用
        private void TimeAutoSet()
        {
            if (CurrentCount > Timeout)
            {
                CheckTime.Set();
            }
        }
        //未超时的手动set，外部调用
        public void TimerSet()
        {
            if (CurrentCount <= Timeout)
            {
                CheckTime.Set();
            }
        }
        #endregion

        #region 计时功能
        /// <summary>
        /// 开始计时
        /// </summary>
        /// <param name="timeout"></param>
        public void Time(int timeout)
        {
            Timeout = timeout;
            IsTiming = true;
            Stop();
            ClearCount();
            Start();
        }
        /// <summary>
        /// 时间耗尽
        /// </summary>
        private void TimeRunsOut()
        {
            if (CurrentCount > Timeout)
            {
                Stop();
                ClearCount();
                TimesUp?.Invoke();
                IsTiming = false;
            }
        }
        /// <summary>
        /// 计时时间重置
        /// </summary>
        public void TimerReset()
        {
            if (IsTiming)
            {
                ClearCount();
            }
        }
        #endregion
    }
    /// <summary>
    /// 监视工具
    /// </summary>
    public class Monitor
    {
        /// <summary>
        /// 监视开关
        /// </summary>
        public AutoResetEvent MonitorSwitch { get; private set; }
        /// <summary>
        /// 监视标记，为true时暂停监视
        /// </summary>
        private bool condition = false;
        public bool Condition
        {
            get { return condition; }
            set { condition = value; if (condition) ConditionAction?.Invoke(); }
        }
        /// <summary>
        /// 标记触发事件
        /// </summary>
        public Action? ConditionAction { get; set; }

        public Monitor()
        {
            MonitorSwitch = new AutoResetEvent(false);
        }

        /// <summary>
        /// 监视某个指定函数对标记值的改变，如果条件改变则暂停监视，直到放开开关
        /// </summary>
        /// <param name="method">监视的方法</param>
        /// <param name="interval">监测时间间隔</param>
        /// <param name="strParams">监测方法参数</param>
        public void ConditionMonitor(Func<string[], bool> method, int interval = 500, params string[] strParams)
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        if (!method(strParams)) break;
                        Thread.Sleep(interval);
                        if (Condition) MonitorSwitch.WaitOne();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        break;
                    }
                }
            });
        }
    }
    /// <summary>
    /// 进程管理工具
    /// </summary>
    public class Processkit
    {
        public Process TargetProcess { get; set; }
        public string Output { get; set; }

        public Processkit(string processPath)
        {
            TargetProcess = new Process();
            Output = "Default";
            TargetProcess.StartInfo.FileName = processPath;
            TargetProcess.StartInfo.UseShellExecute = false;
            TargetProcess.StartInfo.CreateNoWindow = true;
            //p.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
            TargetProcess.StartInfo.RedirectStandardInput = true;
            TargetProcess.StartInfo.RedirectStandardOutput = true;
            TargetProcess.StartInfo.RedirectStandardError = true;
            //TargetProcess.OutputDataReceived += TargetProcess_OutputDataReceived;
            //TargetProcess.ErrorDataReceived += TargetProcess_ErrorDataReceived;
        }

        /// <summary>
        /// 开启一个进程
        /// </summary>
        /// <param name="processArguments">初始参数</param>
        public void StartProcessAsync(string processArguments = "start")
        {
            TargetProcess.StartInfo.Arguments = processArguments;
            TargetProcess.Start();
            TargetProcess.BeginOutputReadLine();
        }
        /// <summary>
        /// 异步向标准输入流输入
        /// </summary>
        /// <param name="input">输入的信息</param>
        public async void ProcessInputAsync(string input)
        {
            await TargetProcess.StandardInput.WriteLineAsync(input);
        }
        /// <summary>
        /// 输出接收委托
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">传输的数据</param>
        private void TargetProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
                Output = e.Data;
        }
        /// <summary>
        /// 错误接收委托
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">传输的数据</param>
        /// <exception cref="NotImplementedException"></exception>
        private void TargetProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public string StartProcess(string processArguments)
        {
            TargetProcess.StartInfo.Arguments = processArguments;
            TargetProcess.Start();
            Output = TargetProcess.StandardOutput.ReadToEnd();
            TargetProcess.WaitForExit();
            TargetProcess.Close();
            return Output;
        }
        /// <summary>
        /// 向标准输入流输入
        /// </summary>
        /// <param name="input">输入的信息</param>
        public void ProcessInput(string input)
        {
            TargetProcess.StandardInput.WriteLine(input);
            //TargetProcess.StandardInput.Close();
        }
        /// <summary>
        /// 进程退出
        /// </summary>
        public void ProcessClose()
        {
            //TargetProcess.CancelOutputRead();
            TargetProcess.WaitForExit();
            TargetProcess.Close();
        }

        public static void StartTask(ref Task? task, Action action)
        {
            if (task == null)
            {
                task = new Task(action);
            }
            else
            {
                task.Wait();
                task = new Task(action);
            }
            task.Start();
        }
    }

}
