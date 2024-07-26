namespace Temperature
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Exception exception = e.Exception;
            MessageBox.Show($"捕获到的异常：{exception.GetType()}{Environment.NewLine}异常信息：{exception.Message}{Environment.NewLine}异常堆栈：{exception.StackTrace}", "异常",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            //bool isStop = e.IsTerminating;//程序是否崩溃
            Exception? exception = e.ExceptionObject as Exception;
            if (exception == null) return;
            MessageBox.Show($"捕获到的异常：{exception.GetType()}{Environment.NewLine}异常信息：{exception.Message}{Environment.NewLine}异常堆栈：{exception.StackTrace}", "线程异常",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}