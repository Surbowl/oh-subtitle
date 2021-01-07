using OhSubtitle.Helpers;
using OhSubtitle.Helpers.Enums;
using OhSubtitle.Services;
using OhSubtitle.Services.Interfaces;
using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace OhSubtitle
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 窗体透明时的不透明度
        /// </summary>
        const double MinimumOpacity = 0.15d;

        [DllImport("user32.dll")]
        private static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

        private static readonly IntPtr _hwndTopMost = new IntPtr(-1);

        /// <summary>
        /// 结束输入后的计时器
        /// </summary>
        private readonly DispatcherTimer _typingTimer;

        /// <summary>
        /// 翻译服务
        /// </summary>
        private readonly ITranslationService _translationService;

        /// <summary>
        /// 字典服务
        /// </summary>
        private readonly IDictionaryService _dictionaryService;

        /// <summary>
        /// 该线程负责周期性地将窗体设为置顶（与视频播放器争夺 TopMost）
        /// </summary>
        private Thread? _setTopMostThread;

        /// <summary>
        /// 是否退出
        /// </summary>
        private bool _isExit = false;

        public MainWindow()
        {
            _typingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(800)
            };
            _typingTimer.Tick += new EventHandler(HandleTypingTimerTimeoutAsync!);

            _translationService = new GoogleTranslationService();
            _dictionaryService = new YoudaoDictionaryService();

            InitializeComponent();
        }

        /// <summary>
        /// 窗体
        /// 加载完毕
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var windowHandle = new WindowInteropHelper(this).Handle;

            // 注册系统快捷键
            var hotKeyRegistSuccess = HotKeyHelper.TryRegist(windowHandle, HotKeyModifiers.Ctrl, Key.Q, () =>
            {
                if (Opacity == 1)
                {
                    Opacity = MinimumOpacity;
                }
                else
                {
                    Opacity = 1;
                }
            });
            if (!hotKeyRegistSuccess)
            {
                txtInput.Text = "Ctrl+Q 快捷键已被其他程序占用";
            }

            // 创建一个新线程，每过 800ms 就重新将该窗体设为置顶（与视频播放器争夺 TopMost）
            _setTopMostThread = new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(800);
                    if (_isExit)
                    {
                        break;
                    }
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        SetWindowPos(windowHandle, _hwndTopMost, 0, 0, 0, 0, 0x0003);
                    });
                }
            });
            _setTopMostThread.Start();
        }

        /// <summary>
        /// 窗体
        /// 鼠标单击
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        /// <summary>
        /// 窗体
        /// 关闭中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _isExit = true;
        }

        /// <summary>
        /// 文本输入框
        /// 文本改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Resets the timer
            _typingTimer.Stop();
            _typingTimer.Start();
        }

        /// <summary>
        /// 重置按钮
        /// 鼠标单击
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImgReset_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _typingTimer.Stop();
            txtInput.Text = string.Empty;
            txtResult.Text = string.Empty;
            imgReset.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 关闭按钮
        /// 鼠标单击
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImgClose_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// 眼睛按钮
        /// 鼠标移入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GridEye_MouseEnter(object sender, MouseEventArgs e)
        {
            Opacity = MinimumOpacity;
        }

        /// <summary>
        /// 眼睛按钮
        /// 鼠标移出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GridEye_MouseLeave(object sender, MouseEventArgs e)
        {
            Opacity = 1;
        }

        /// <summary>
        /// 快捷键
        /// 切换窗体不透明度
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CommandBinding_SwitchOpacity(object sender, CanExecuteRoutedEventArgs e)
        {
            if (Opacity == 1)
            {
                Opacity = MinimumOpacity;
            }
            else
            {
                Opacity = 1;
            }
        }

        /// <summary>
        /// 结束输入并等待一段时间后执行该方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void HandleTypingTimerTimeoutAsync(object sender, EventArgs e)
        {
            imgReset.Visibility = Visibility.Hidden;
            imgLoading.Visibility = Visibility.Visible;

            var timer = sender as DispatcherTimer;
            if (timer != null)
            {
                // The timer must be stopped, We want to act only once per keystroke.
                timer.Stop();
                if (string.IsNullOrWhiteSpace(txtInput.Text))
                {
                    txtResult.Text = string.Empty;
                }
                else if (IsAEnglishWord(txtInput.Text))
                {
                    var result = await _dictionaryService.QueryAsync(txtInput.Text);
                    if (string.IsNullOrEmpty(result))
                    {
                        result = await _translationService.TranslateAsync(txtInput.Text);
                    }
                    txtResult.Text = result;
                }
                else
                {
                    txtResult.Text = await _translationService.TranslateAsync(txtInput.Text);
                }
            }

            imgLoading.Visibility = Visibility.Hidden;
            if (!string.IsNullOrWhiteSpace(txtResult.Text))
            {
                imgReset.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 判断是否是单个英文单词
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private bool IsAEnglishWord(string str)
        {
            str = str.Trim();
            if (str.Length > 45)
            {
                return false;
            }

            return new Regex(@"^([a-zA-Z]|-|\.|·|')+$").Match(str).Success;
        }
    }
}