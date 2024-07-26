using CSharpKit.FileManagement;

namespace Temperature
{
    public class DataManager
    {
        #region 单例模式
        private static DataManager? _instance;
        private static readonly object _instanceLock = new();
        public static DataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLock)
                        _instance = new DataManager();
                }
                return _instance;
            }
        }
        #endregion

        public static string RootPath = "Config";
        public KeyValueManager Config = new("Config.json", RootPath);
        public List<TemperaturePoint> TemperatureList = [];

        public DataManager()
        {
            TemperatureList = JsonManager.LoadList<TemperaturePoint>(RootPath, "TemperaturePoint.json");
        }


    }
}
