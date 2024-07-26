namespace Services
{
    public class FormMethod
    {
        public static void OnThread(Control control, Action method)
        {
            if (control.IsHandleCreated)
                control.Invoke(method);
            else
                method();
        }

        public static void ShowInfoBox(string message, string caption = "提示")
        {
            MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void ShowErrorBox(string message, string caption = "错误")
        {
            MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static DialogResult ShowQuestionBox(string message, string caption = "提示")
        {
            return MessageBox.Show(message, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        }

        public static void UpdateListBox(ListBox list, List<string> data)
        {
            list.Items.Clear();
            foreach (var item in data)
                list.Items.Add(item);
        }

        /// <summary>
        /// 得到一个矩形阵列的坐标
        /// </summary>
        /// <param name="x">阵列起始X坐标</param>
        /// <param name="y">阵列起始Y坐标</param>
        /// <param name="count">阵列元素个数</param>
        /// <param name="length">每行的元素个数</param>
        /// <param name="xInterval">阵列坐标x方向间距</param>
        /// <param name="yInterval">阵列坐标y方向间距</param>
        /// <returns>阵列坐标列表</returns>
        public static List<Point> SetLocation(int x, int y, int count, int length, int xInterval, int yInterval)
        {
            int o = x;
            List<Point> locationList = [];
            for (int i = 0; i < count; i++)
            {
                locationList.Add(new Point(x, y));
                x += xInterval;
                if ((i + 1) % length == 0)
                {
                    x = o;
                    y += yInterval;
                }
            }
            return locationList;
        }

    }

}
