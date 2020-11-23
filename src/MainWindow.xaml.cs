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

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            // https://stackoverflow.com/questions/20050426/wpf-always-on-top
            ((Window)sender).Topmost = true;
        }

        private void TxtInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Resets the timer
            _typingTimer.Stop();
            _typingTimer.Start();
        }

        private void ImgReset_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _typingTimer.Stop();
            txtInput.Text = string.Empty;
            txtResult.Text = string.Empty;
            imgReset.Visibility = Visibility.Visible;
        }

        private void ImgEye_MouseEnter(object sender, MouseEventArgs e)
        {
            Opacity = 0.1;
        }

        private void ImgEye_MouseLeave(object sender, MouseEventArgs e)
        {
            Opacity = 1;
        }

        private void ImgClose_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
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

            return new Regex(@"^([a-zA-Z]|-)+$").Match(str).Success;
        }
    }
}