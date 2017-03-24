using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;

namespace ClipBoardDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IntPtr windowHandle;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void init()
        {
            windowHandle = new WindowInteropHelper(this).Handle;
            HwndSource source = HwndSource.FromHwnd(windowHandle);
            if(source != null)
            {
                source.AddHook(WndProc);
            }
            HotKey.RegisterHotKey(windowHandle, 100, HotKey.KeyModifiers.Ctrl, System.Windows.Forms.Keys.C);
        }

        private void copyButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(copyTextBox.Text);
        }

        private void pasteButton_Click(object sender, RoutedEventArgs e)
        {
            pasteTextBox.Text = Clipboard.GetText();
        }


        //Reference http://blog.csdn.net/ismycxp/article/details/1698942

        class HotKey
        {
            //如果函数执行成功，返回值不为0。
            //如果函数执行失败，返回值为0。要得到扩展错误信息，调用GetLastError。
            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool RegisterHotKey(
                IntPtr hWnd,                //要定义热键的窗口的句柄
                int id,                     //定义热键ID（不能与其它ID重复）
                KeyModifiers fsModifiers,   //标识热键是否在按Alt、Ctrl、Shift、Windows等键时才会生效
                System.Windows.Forms.Keys vk                     //定义热键的内容
                );
            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool UnregisterHotKey(
                IntPtr hWnd,                //要取消热键的窗口的句柄
                int id                      //要取消热键的ID
                );
            //定义了辅助键的名称（将数字转变为字符以便于记忆，也可去除此枚举而直接使用数值）
            [Flags()]
            public enum KeyModifiers
            {
                None = 0,
                Alt = 1,
                Ctrl = 2,
                Shift = 4,
                WindowsKey = 8
            }
        }
        //简单说明一下：
        //“public static extern bool RegisterHotKey()”这个函数用于注册热键。由于这个函数需要引用user32.dll动态链接库后才能使用，并且
        //user32.dll是非托管代码，不能用命名空间的方式直接引用，所以需要用“DllImport”进行引入后才能使用。于是在函数前面需要加上
        //“[DllImport("user32.dll", SetLastError = true)]”这行语句。
        //“public static extern bool UnregisterHotKey()”这个函数用于注销热键，同理也需要用DllImport引用user32.dll后才能使用。
        //“public enum KeyModifiers{}”定义了一组枚举，将辅助键的数字代码直接表示为文字，以方便使用。这样在调用时我们不必记住每一个辅
        //助键的代码而只需直接选择其名称即可。
        //（2）以窗体FormA为例，介绍HotKey类的使用
        //在FormA的Activate事件中注册热键，本例中注册Shift+S，Ctrl+Z，Alt+D这三个热键。这里的Id号可任意设置，但要保证不被重复。

        private void Form_Activated(object sender, EventArgs e)
        {
            //注册热键Alt+F12，Id号为100。HotKey.KeyModifiers.Shift也可以直接使用数字4来表示。
            HotKey.RegisterHotKey(windowHandle, 100, HotKey.KeyModifiers.Alt, System.Windows.Forms.Keys.F12);
            //注册热键Ctrl+B，Id号为101。HotKey.KeyModifiers.Ctrl也可以直接使用数字2来表示。
            HotKey.RegisterHotKey(windowHandle, 101, HotKey.KeyModifiers.Ctrl, System.Windows.Forms.Keys.B);
            //注册热键Alt+D，Id号为102。HotKey.KeyModifiers.Alt也可以直接使用数字1来表示。
            HotKey.RegisterHotKey(windowHandle, 102, HotKey.KeyModifiers.Alt, System.Windows.Forms.Keys.D);
        }
        //在FormA的Leave事件中注销热键。
        private void FrmSale_Leave(object sender, EventArgs e)
        {
            //注销Id号为100的热键设定
            HotKey.UnregisterHotKey(windowHandle, 100);
            //注销Id号为101的热键设定
            HotKey.UnregisterHotKey(windowHandle, 101);
            //注销Id号为102的热键设定
            HotKey.UnregisterHotKey(windowHandle, 102);
        }

        //重载FromA中的WndProc函数
        /// 
        /// 监视Windows消息
        /// 重载WndProc方法，用于实现热键响应
        /// 
        /// 
        protected virtual IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {

            const int WM_HOTKEY = 0x0312;

            //按快捷键 
            switch (msg)
            {
                case WM_HOTKEY:
                    infoLabel.Content = msg.ToString() + "|" + WM_HOTKEY.ToString() + "|" + wParam.ToInt32();

                    switch (wParam.ToInt32())
                    {
                        case 100:    //按下的是Alt+F12
                                     //此处填写快捷键响应代码 

                            MessageBox.Show("Hello");
                            break;
                        case 101:    //按下的是Ctrl+B
                            //此处填写快捷键响应代码
                            break;
                        case 102:    //按下的是Alt+D
                            //此处填写快捷键响应代码
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }


        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            init();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            HotKey.UnregisterHotKey(windowHandle, 100);
        }
    }
}
