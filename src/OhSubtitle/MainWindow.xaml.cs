using OhSubtitle.Translations.Interfaces;
using OhSubtitle.Translations.Services;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace OhSubtitle
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _typingTimer;
        private readonly ITranslationService _translationService;

        public MainWindow()
        {
            _typingTimer = new DispatcherTimer();
            _typingTimer.Interval = TimeSpan.FromMilliseconds(800);
            _typingTimer.Tick += new EventHandler(HandleTypingTimerTimeout);
            _translationService = new GoogleTranslationService();
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void txtInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Resets the timer
            _typingTimer.Stop();
            _typingTimer.Start();
        }

        private async void HandleTypingTimerTimeout(object sender, EventArgs e)
        {
            imgReset.Visibility = Visibility.Hidden;
            imgLoading.Visibility = Visibility.Visible;
            var timer = sender as DispatcherTimer;
            if (timer != null)
            {
                // The timer must be stopped, We want to act only once per keystroke.
                timer.Stop();
                txtResult.Text = await TranslateText(txtInput.Text);
            }
            imgLoading.Visibility = Visibility.Hidden;
            if (!string.IsNullOrWhiteSpace(txtResult.Text))
            {
                imgReset.Visibility = Visibility.Visible;
            }
        }

        private void imgReset_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _typingTimer.Stop();
            txtInput.Text = string.Empty;
            txtResult.Text = string.Empty;
            imgReset.Visibility = Visibility.Visible;
        }
        private void imgClose_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async Task<string> TranslateText(string orig)
        {
            if (string.IsNullOrWhiteSpace(orig))
            {
                return string.Empty;
            }
            return await _translationService.TranslateTextAsync(orig);
        }
    }
}