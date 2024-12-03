using Microsoft.Maui.Controls;
using System;
using System.Timers;

namespace WorkoutApplication
{
    public partial class RestTimerPage : ContentPage
    {
        private System.Timers.Timer _timer;
        private TimeSpan _elapsedTime;
        private TimeSpan _totalTime;
        private readonly object _lock = new object();

        public RestTimerPage()
        {
            InitializeComponent();
            _totalTime = TimeSpan.FromSeconds(Preferences.Get("DefaultRestTime", 180)); // Get the rest time from settings
            StartTimer();
        }

        private void StartTimer()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }

            _elapsedTime = _totalTime;

            _timer = new System.Timers.Timer { Interval = 1000 }; // 1 second interval
            _timer.Elapsed += OnTimedEvent;
            _timer.AutoReset = true;
            _timer.Start();
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            lock (_lock)
            {
                _elapsedTime = _elapsedTime.Subtract(TimeSpan.FromSeconds(1));
                if (_elapsedTime.TotalSeconds <= 0)
                {
                    _timer.Stop();
                    _timer.Dispose();

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        Vibration.Default.Vibrate(500); // Vibrate when the timer ends
                        await Shell.Current.GoToAsync("//MainPage");
                    });
                    return;
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TimerLabel.Text = _elapsedTime.ToString(@"m\:ss");
                });
            }
        }

        private void OnMinus30SecClicked(object sender, EventArgs e)
        {
            lock (_lock)
            {
                _elapsedTime = _elapsedTime.Subtract(TimeSpan.FromSeconds(30));
                if (_elapsedTime.TotalSeconds < 0)
                {
                    _elapsedTime = TimeSpan.Zero;
                }
                TimerLabel.Text = _elapsedTime.ToString(@"m\:ss");
            }
        }

        private void OnPlus30SecClicked(object sender, EventArgs e)
        {
            lock (_lock)
            {
                _elapsedTime = _elapsedTime.Add(TimeSpan.FromSeconds(30));
                TimerLabel.Text = _elapsedTime.ToString(@"m\:ss");
            }
        }

        private async void OnSkipClicked(object sender, EventArgs e)
        {
            _timer.Stop();
            _timer.Dispose();
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}
