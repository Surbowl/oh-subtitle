using OhSubtitle.Enums;
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
        #region DefaultValue 默认值
        /// <summary>
        /// 窗体透明时的不透明度
        /// </summary>
        const double WindowMinimumOpacity = 0.15d;

        /// <summary>
        /// 窗体默认宽度
        /// </summary>
        const double WindowDefaultWidth = 860;

        /// <summary>
        /// 窗体默认高度
        /// </summary>
        const double WindowDefaultHeight = 65;
        #endregion DefaultValue 默认值

        [DllImport("user32.dll")]
        private static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

        private static readonly IntPtr _hwndTopMost = new IntPtr(-1);

        /// <summary>
        /// 该线程负责周期性地将窗体设为置顶（与视频播放器争夺 TopMost）
        /// </summary>
        private Thread? _setTopMostThread;

        /// <summary>
        /// 结束输入后的计时器
        /// </summary>
        private readonly DispatcherTimer _typingTimer;

        /// <summary>
        /// 是否退出
        /// </summary>
        private bool _isExit = false;

        /// <summary>
        /// 翻译服务，用于翻译句子
        /// </summary>
        private ITranslationService _translationService;

        /// <summary>
        /// 字典服务，用于查询单个单词的释义
        /// </summary>
        private IDictionaryService? _dictionaryService;

        /// <summary>
        /// 笔记服务，用于记笔记
        /// </summary>
        private INoteService _noteService;

        /// <summary>
        /// 主题颜色
        /// </summary>
        private ThemeColors _themeColor;

        /// <summary>
        /// 语言模式
        /// </summary>
        private LangModels _langModel;

        /// <summary>
        /// 主题颜色
        /// </summary>
        protected ThemeColors ThemeColor
        {
            get
            {
                return _themeColor;
            }
            set
            {
                _themeColor = value;

                menuThemeColorWhite.IsChecked = false;
                menuThemeColorDimGray.IsChecked = false;
                menuThemeColorLightGray.IsChecked = false;
                menuThemeColorBlack.IsChecked = false;

                switch (_themeColor)
                {
                    case ThemeColors.White:
                        SetWindowColor(Brushes.White, Brushes.Black);
                        menuThemeColorWhite.IsChecked = true;
                        break;
                    case ThemeColors.LightGray:
                        SetWindowColor(Brushes.LightGray, Brushes.Black);
                        menuThemeColorLightGray.IsChecked = true;
                        break;
                    case ThemeColors.DimGray:
                        SetWindowColor(Brushes.DimGray, Brushes.White);
                        menuThemeColorDimGray.IsChecked = true;
                        break;
                    case ThemeColors.Black:
                    default:
                        SetWindowColor(Brushes.Black, Brushes.FloralWhite);
                        menuThemeColorBlack.IsChecked = true;
                        break;
                }
            }
        }

        /// <summary>
        /// 语言模式
        /// </summary>
        //[MemberNotNull(nameof(_translationService))]
        protected LangModels LangModel
        {
            get
            {
                return _langModel;
            }
            set
            {
                _langModel = value;

                menuLangModelZhEn.IsChecked = false;
                menuLangModelZhJp.IsChecked = false;

                switch (LangModel)
                {
                    case LangModels.ZhJp:
                        _translationService = new GoogleJapaneseTranslationService();
                        _dictionaryService = null;

                        menuLangModelZhJp.IsChecked = true;
                        menuThemeColorWhite.Header = "亮白 白い";
                        menuThemeColorLightGray.Header = "亮灰 薄いグレー";
                        menuThemeColorDimGray.Header = "暗灰 暗いグレー";
                        menuThemeColorBlack.Header = "暗黑 黒";
                        menuExit.Header = "退出 终了";
                        imgWriteNote.ToolTip = "记笔记 ノートをとる";
                        imgReset.ToolTip = "清空输入框 空入力";
                        break;
                    case LangModels.ZhEn:
                    default:
                        _translationService = new GoogleEnglishTranslationService();
                        _dictionaryService = new YoudaoEnglishDictionaryService();

                        menuLangModelZhEn.IsChecked = true;
                        menuThemeColorWhite.Header = "亮白 White";
                        menuThemeColorLightGray.Header = "亮灰 LightGray";
                        menuThemeColorDimGray.Header = "暗灰 DimGray";
                        menuThemeColorBlack.Header = "暗黑 Black";
                        menuExit.Header = "退出 Exit";
                        imgWriteNote.ToolTip = "记笔记 WriteNote";
                        imgReset.ToolTip = "清空输入框 ClearInput";
                        break;
                }

                ResetTypingTimer();
            }
        }

        public MainWindow()
        {
            _typingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(800)
            };
            _typingTimer.Tick += new EventHandler(HandleTypingTimerTimeoutAsync!);

            InitializeComponent();

            // 读取配置文件，设置位置、大小、主题颜色和语言模式
            try
            {
                Rect restoreBounds = Properties.Settings.Default.MainWindowsRect;
                Left = restoreBounds.Left;
                Top = restoreBounds.Top;
                Width = restoreBounds.Width;
                Height = restoreBounds.Height;

                ThemeColor = Properties.Settings.Default.ThemeColor;
                LangModel = Properties.Settings.Default.LangModel;
            }
            catch
            {
                Width = WindowDefaultWidth;
                Height = WindowDefaultHeight;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;

                ThemeColor = ThemeColors.Black;
                LangModel = LangModels.ZhEn;
            }

            if (_translationService == null)
            {
                _translationService = new GoogleJapaneseTranslationService();
            }

            _noteService = new CsvFileNoteService();
        }

        #region WindowEvent & Action 窗体事件与方法
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
                    Opacity = WindowMinimumOpacity;
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
            imgWriteNote.Visibility = Visibility.Hidden;
            imgReset.Visibility = Visibility.Hidden;
            imgLoading.Visibility = Visibility.Visible;

            // 保存当前位置、大小和状态到配置文件
            Properties.Settings.Default.MainWindowsRect = RestoreBounds;
            Properties.Settings.Default.ThemeColor = ThemeColor;
            Properties.Settings.Default.LangModel = LangModel;
            Properties.Settings.Default.Save();

            _isExit = true;
        }

        /// <summary>
        /// 设置窗体颜色
        /// </summary>
        /// <param name="background"></param>
        /// <param name="foreground"></param>
        private void SetWindowColor(Brush background, Brush foreground)
        {
            Background = txtInput.Background = background;
            txtInput.Foreground = txtResult.Foreground = foreground;
            imgLoading.Foreground = imgReset.Foreground = imgWriteNote.Foreground = imgNoteWrote.Foreground = imgEye.Foreground = imgClose.Foreground = foreground;
        }
        #endregion WindowEvent & Action 窗体事件与方法

        #region ImgButton 按钮
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
            Opacity = WindowMinimumOpacity;
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
        /// 笔记按钮
        /// 鼠标单击
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ImgWriteNote_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                imgWriteNote.Visibility = Visibility.Hidden;
                imgNoteWrote.Visibility = Visibility.Visible;

                await _noteService.WriteAsync(txtInput.Text, txtResult.Text);
            }
            catch
            {
                imgWriteNote.Visibility = Visibility.Visible;
                imgNoteWrote.Visibility = Visibility.Hidden;

                txtInput.Text = "笔记记录失败，可能是因为文件被占用或没有写入文件的权限。如果您已打开笔记文件，请将其关闭后再记录笔记；如果依然无法记录笔记，请尝试使用系统管理员权限启动本应用。";
            }
        }
        #endregion ImgButton 按钮

        #region ContextMenu 右键菜单
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
        private void MenuLangModelZhEn_Click(object sender, RoutedEventArgs e)
        {
            LangModel = LangModels.ZhEn;
        }

        /// <summary>
        /// 右键菜单
        /// 中文←→日本語
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuLangModelZhJp_Click(object sender, RoutedEventArgs e)
        {
            LangModel = LangModels.ZhJp;
        }

        /// <summary>
        /// 右键菜单
        /// 亮白
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuThemeColorWhite_Click(object sender, RoutedEventArgs e)
        {
            ThemeColor = ThemeColors.White;
        }

        /// <summary>
        /// 右键菜单
        /// 亮灰
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuThemeColorLightGray_Click(object sender, RoutedEventArgs e)
        {
            ThemeColor = ThemeColors.LightGray;
        }

        /// <summary>
        /// 右键菜单
        /// 暗灰
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuThemeColorDimGray_Click(object sender, RoutedEventArgs e)
        {
            ThemeColor = ThemeColors.DimGray;
        }

        /// <summary>
        /// 右键菜单
        /// 暗黑
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuThemeColorBlack_Click(object sender, RoutedEventArgs e)
        {
            ThemeColor = ThemeColors.Black;
        }
        #endregion ContextMenu 右键菜单

        #region CommandBinding 快捷键
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
                Opacity = WindowMinimumOpacity;
            }
            else
            {
                Opacity = 1;
            }
        }
        #endregion CommandBinding 快捷键

        #region TypingEvent & Timer 输入事件与计时器
        /// <summary>
        /// 重置结束输入后的计时器，计时器倒计时结束后将执行<see cref="HandleTypingTimerTimeoutAsync"/>
        /// </summary>
        private void ResetTypingTimer()
        {
            _typingTimer.Stop();
            _typingTimer.Start();
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
        /// <see cref="_typingTimer"/>倒计时结束后执行该方法，对输入内容进行翻译
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void HandleTypingTimerTimeoutAsync(object sender, EventArgs e)
        {
            imgWriteNote.Visibility = Visibility.Hidden;
            imgNoteWrote.Visibility = Visibility.Hidden;
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
                    if (txtInput.Text.IsAEnglishWord()) // 目前只有英文单词支持查单词
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
            if (!string.IsNullOrWhiteSpace(txtInput.Text))
            {
                imgWriteNote.Visibility = Visibility.Visible;
                imgReset.Visibility = Visibility.Visible;
            }
        }
        #endregion TypingEvent & Timer 输入事件与计时器
    }
}