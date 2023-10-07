using OhSubtitle.Enums;
using OhSubtitle.Helpers;
using OhSubtitle.Services;
using OhSubtitle.Services.Implements;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace OhSubtitle;

public partial class MainWindow : Window
{
    /// <summary>
    /// 窗体透明时的不透明度
    /// </summary>
    const double WINDOW_MINIUMU_OPACITY = 0.15d;

    /// <summary>
    /// 窗体默认宽度
    /// </summary>
    const double WINDOW_DEFAULT_WIDTH = 860;

    /// <summary>
    /// 窗体默认高度
    /// </summary>
    const double WINDOW_DEFAULT_HEIGHT = 65;

    [DllImport("user32.dll")]
    private static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

    private static readonly IntPtr _hwndTopMost = new(-1);

    /// <summary>
    /// 该线程负责周期性地将窗体设为置顶（与视频播放器争夺 TopMost）
    /// </summary>
    private Thread? _setTopMostThread;

    /// <summary>    
    /// 倒计时结束时将 <see cref="gridMain"/> 设为隐藏
    /// </summary>
    private readonly DispatcherTimer _gridMainHiddenTimer;

    /// <summary>
    /// 是否退出
    /// </summary>
    private bool _isExit = false;

    /// <summary>
    /// 笔记服务，用于记笔记
    /// </summary>
    private INoteService _noteService;

    /// <summary>
    /// 主题颜色
    /// </summary>
    private ThemeColors _themeColor;

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

    public MainWindow()
    {
        _gridMainHiddenTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _gridMainHiddenTimer.Tick += new EventHandler((_, _) =>
        {
            if (!_isExit && gridMain != null)
            {
                // 隐藏输入框与按钮
                gridMain.Visibility = Visibility.Hidden;
                // 隐藏右下角三角标
                ResizeMode = ResizeMode.NoResize;
            }
        });

        InitializeComponent();

        LoadSettingsAndInitializeServices();

        txtInput.Text = "可在此处输入笔记 / 按 Ctrl+Q 快捷键切换不透明度 / 右击悬浮窗边缘可打开菜单";
        gridNoteWrote.Visibility = Visibility.Hidden;
        gridWriteNote.Visibility = Visibility.Hidden;
        gridReset.Visibility = Visibility.Hidden;
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
                Opacity = WINDOW_MINIUMU_OPACITY;
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
    /// 鼠标进入
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Window_MouseEnter(object sender, MouseEventArgs e)
    {
        _gridMainHiddenTimer.Stop();
        // 显示输入框与按钮
        gridMain.Visibility = Visibility.Visible;
        // 显示右下角三角标
        ResizeMode = ResizeMode.CanResizeWithGrip;
    }

    /// <summary>
    /// 窗体
    /// 鼠标离开
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Window_MouseLeave(object sender, MouseEventArgs e)
    {
        if (gridReset.Visibility == Visibility.Hidden)
        {
            _gridMainHiddenTimer.Start();
        }
    }

    /// <summary>
    /// 窗体
    /// 鼠标单击
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && Mouse.LeftButton == MouseButtonState.Pressed)
        {
            ResizeMode = ResizeMode.NoResize; // 防止窗口拖到屏幕边缘自动最大化
            UpdateLayout();

            DragMove(); // 拖动窗体

            ResizeMode = ResizeMode.CanResizeWithGrip;
            UpdateLayout();
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
        gridWriteNote.Visibility = Visibility.Hidden;
        gridReset.Visibility = Visibility.Hidden;

        SaveCurrentSettings();

        _isExit = true;
    }

    /// <summary>
    /// 加载配置并初始化服务
    /// </summary>
    [MemberNotNull(nameof(_noteService))]
    private void LoadSettingsAndInitializeServices()
    {
        // 读取配置文件，设置位置、大小、主题颜色和语言模式
        try
        {
            Rect restoreBounds = Properties.Settings.Default.MainWindowsRect;
            Left = restoreBounds.Left;
            Top = restoreBounds.Top;
            Width = restoreBounds.Width;
            Height = restoreBounds.Height;
            ThemeColor = Properties.Settings.Default.ThemeColor;
        }
        catch
        {
            Width = WINDOW_DEFAULT_WIDTH;
            Height = WINDOW_DEFAULT_HEIGHT;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ThemeColor = ThemeColors.Black;
        }

        _noteService = new CsvFileNoteService();
    }

    /// <summary>
    /// 保存当前配置
    /// </summary>
    private void SaveCurrentSettings()
    {
        // 保存当前位置、大小和状态到配置文件
        Properties.Settings.Default.MainWindowsRect = RestoreBounds;
        Properties.Settings.Default.ThemeColor = ThemeColor;
        Properties.Settings.Default.Save();
    }

    /// <summary>
    /// 设置窗体颜色
    /// </summary>
    /// <param name="background"></param>
    /// <param name="foreground"></param>
    private void SetWindowColor(Brush background, Brush foreground)
    {
        Background = txtInput.Background = background;
        txtInput.Foreground = foreground;
        imgEye.Foreground = imgReset.Foreground = labReset.Foreground = imgWriteNote.Foreground = labWriteNote.Foreground = imgClose.Foreground = foreground;
    }

    /// <summary>
    /// 重置按钮
    /// 鼠标单击
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ImgReset_MouseDown(object sender, MouseButtonEventArgs e)
    {
        txtInput.Text = string.Empty;
        gridReset.Visibility = Visibility.Hidden;
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
        Opacity = WINDOW_MINIUMU_OPACITY;
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
            gridWriteNote.Visibility = Visibility.Hidden;
            gridNoteWrote.Visibility = Visibility.Visible;

            await _noteService.WriteAsync(txtInput.Text);
        }
        catch
        {
            gridWriteNote.Visibility = Visibility.Visible;
            gridNoteWrote.Visibility = Visibility.Hidden;

            txtInput.Text = "笔记记录失败，可能是因为文件被占用或没有写入文件的权限。如果您已打开笔记文件，请将其关闭后再记录笔记；如果依然无法记录笔记，请尝试使用系统管理员权限启动本应用。";
        }
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

    /// <summary>
    /// 快捷键
    /// 切换窗体不透明度
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CommandBinding_SwitchOpacity(object sender, CanExecuteRoutedEventArgs e)
    {
        Opacity = Opacity == 1 ? WINDOW_MINIUMU_OPACITY : 1;
    }

    /// <summary>
    /// 文本输入框
    /// 文本改变
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void TxtInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        gridNoteWrote.Visibility = Visibility.Hidden;

        if (string.IsNullOrWhiteSpace(txtInput.Text))
        {
            gridWriteNote.Visibility = Visibility.Hidden;
            gridReset.Visibility = Visibility.Hidden;
        }
        else
        {
            gridWriteNote.Visibility = Visibility.Visible;
            gridReset.Visibility = Visibility.Visible;
        }
    }
}