using OhSubtitle.Helpers;
using OhSubtitle.Helpers.Enums;
using OhSubtitle.Services;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
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
        private ITranslationService _translationService;

        /// <summary>
        /// 字典服务
        /// </summary>
        private IDictionaryService? _dictionaryService;

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

            _translationService = new GoogleEnglishTranslationService();
            _dictionaryService = new YoudaoEnglishDictionaryService();

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
            ResetTypingTimer();
        }

        /// <summary>
        /// 重置结束输入后的计时器，计时器倒计时结束后将执行<see cref="HandleTypingTimerTimeoutAsync"/>
        /// </summary>
        private void ResetTypingTimer()
        {
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
            _isExit = true;
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
        /// 右键菜单
        /// 退出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            _isExit = true;
            Application.Current.Shutdown();
        }

        /// <summary>
        /// 右键菜单
        /// 中文←→English
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuModelZhEn_Click(object sender, RoutedEventArgs e)
        {
            menuModelZhEn.IsChecked = true;
            menuModelZhJp.IsChecked = false;

            RefreshMenuHeaders();

            _translationService = new GoogleEnglishTranslationService();
            _dictionaryService = new YoudaoEnglishDictionaryService();

            ResetTypingTimer();
        }

        /// <summary>
        /// 右键菜单
        /// 中文←→日本語
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuModelZhJp_Click(object sender, RoutedEventArgs e)
        {
            menuModelZhEn.IsChecked = false;
            menuModelZhJp.IsChecked = true;

            RefreshMenuHeaders();

            _translationService = new GoogleJapaneseTranslationService();
            _dictionaryService = null;

            ResetTypingTimer();
        }

        /// <summary>
        /// 右键菜单
        /// 亮白
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuColorWhite_Click(object sender, RoutedEventArgs e)
        {
            menuColorWhite.IsChecked = true;
            menuColorLightGray.IsChecked = false;
            menuColorDimGray.IsChecked = false;
            menuColorBlack.IsChecked = false;

            RefreshWindowColor();
        }

        /// <summary>
        /// 右键菜单
        /// 亮灰
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuColorLightGray_Click(object sender, RoutedEventArgs e)
        {
            menuColorWhite.IsChecked = false;
            menuColorLightGray.IsChecked = true;
            menuColorDimGray.IsChecked = false;
            menuColorBlack.IsChecked = false;

            RefreshWindowColor();
        }

        /// <summary>
        /// 右键菜单
        /// 暗灰
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuColorDimGray_Click(object sender, RoutedEventArgs e)
        {
            menuColorWhite.IsChecked = false;
            menuColorLightGray.IsChecked = false;
            menuColorDimGray.IsChecked = true;
            menuColorBlack.IsChecked = false;

            RefreshWindowColor();
        }

        /// <summary>
        /// 右键菜单
        /// 暗黑
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuColorBlack_Click(object sender, RoutedEventArgs e)
        {
            menuColorWhite.IsChecked = false;
            menuColorDimGray.IsChecked = false;
            menuColorLightGray.IsChecked = false;
            menuColorBlack.IsChecked = true;

            RefreshWindowColor();
        }

        /// <summary>
        /// 刷新窗体颜色
        /// </summary>
        private void RefreshWindowColor()
        {
            if (menuColorWhite.IsChecked)
            {
                Background = txtInput.Background = Brushes.White;
                txtInput.Foreground = txtResult.Foreground = Brushes.Black;
                imgLoading.Foreground = imgReset.Foreground = imgEye.Foreground = imgClose.Foreground = Brushes.Black;
                return;
            }
            if (menuColorLightGray.IsChecked)
            {
                Background = txtInput.Background = Brushes.LightGray;
                txtInput.Foreground = txtResult.Foreground = Brushes.Black;
                imgLoading.Foreground = imgReset.Foreground = imgEye.Foreground = imgClose.Foreground = Brushes.Black;
                return;
            }
            if (menuColorDimGray.IsChecked)
            {
                Background = txtInput.Background = Brushes.DimGray;
                txtInput.Foreground = txtResult.Foreground = Brushes.White;
                imgLoading.Foreground = imgReset.Foreground = imgEye.Foreground = imgClose.Foreground = Brushes.White;
                return;
            }
            Background = txtInput.Background = Brushes.Black;
            txtInput.Foreground = txtResult.Foreground = Brushes.FloralWhite;
            imgLoading.Foreground = imgReset.Foreground = imgEye.Foreground = imgClose.Foreground = Brushes.FloralWhite;
        }

        /// <summary>
        /// 刷新右键菜单的 Header
        /// </summary>
        private void RefreshMenuHeaders()
        {
            if (menuModelZhEn.IsChecked)
            {
                menuColorWhite.Header = "亮白 White";
                menuColorLightGray.Header = "亮灰 LightGray";
                menuColorDimGray.Header = "暗灰 DimGray";
                menuColorBlack.Header = "暗黑 Black";
                menuExit.Header = "退出 Exit";
                return;
            }
            menuColorWhite.Header = "亮白 白い";
            menuColorLightGray.Header = "亮灰 薄いグレー";
            menuColorDimGray.Header = "暗灰 暗いグレー";
            menuColorBlack.Header = "暗黑 黒";
            menuExit.Header = "退出 终了";
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
        /// <see cref="_typingTimer"/>倒计时结束后执行该方法，对输入内容进行翻译
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
                bool isFinish = false;
                if (string.IsNullOrWhiteSpace(txtInput.Text)) // Empty
                {
                    txtResult.Text = string.Empty;
                    isFinish = true;
                }

                if (!isFinish && _dictionaryService != null) // 查单词
                {
                    if (menuModelZhEn.IsChecked && txtInput.Text.IsAEnglishWord()) // 目前只有英文单词支持查单词
                    {
                        var result = await _dictionaryService.QueryAsync(txtInput.Text);
                        if (string.IsNullOrEmpty(result))
                        {
                            result = await _translationService.TranslateAsync(txtInput.Text);
                        }
                        txtResult.Text = result;
                        isFinish = true;
                    }
                }

                if (!isFinish) // 翻译句子
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
    }
}