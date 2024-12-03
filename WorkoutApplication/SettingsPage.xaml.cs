using System;
using System.Net.Mail;
using System.Net;
using Microsoft.Maui.Controls;

namespace WorkoutApplication
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();
            LoadSettings();
        }

        private async void OnSendButtonClicked(object sender, EventArgs e)
        {
            string message = MessageEditor.Text;

            if (!string.IsNullOrWhiteSpace(message))
            {
                try
                {
                    using (var client = new SmtpClient("smtp.gmail.com", 587))
                    {
                        client.UseDefaultCredentials = false;
                        client.EnableSsl = true;
                        client.Credentials = new NetworkCredential("spamkadyrov@gmail.com", "ywll zbbz hfvs lgvh");

                        var mailMessage = new MailMessage
                        {
                            From = new MailAddress("spamkadyrov@gmail.com"),
                            Subject = "Feature Request/Contact",
                            Body = message,
                            IsBodyHtml = false
                        };
                        mailMessage.To.Add("kadyrovjustin@gmail.com");

                        await client.SendMailAsync(mailMessage);
                    }

                    await DisplayAlert("Success", "Your message has been sent.", "OK");
                    MessageEditor.Text = string.Empty; // Clear the message editor
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"An error occurred while sending the email: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", "Please enter a message.", "OK");
            }
        }


        private void LoadSettings()
        {
            // Load the current settings into the UI
            MeasurementPicker.SelectedIndex = Preferences.Get("MeasurementUnit", 0);
            RestTimeSwitch.IsToggled = Preferences.Get("EnableRestTimer", true);
            RestTimeSlider.Value = Preferences.Get("DefaultRestTime", 60);

            // Update the label to match the slider value
            UpdateRestTimeLabel(RestTimeSlider.Value);
        }

        private void OnRestTimeSliderValueChanged(object sender, ValueChangedEventArgs e)
        {
            var timeInSeconds = (int)e.NewValue;

            // Round the value to the nearest 30-second interval
            timeInSeconds = (int)Math.Round(timeInSeconds / 30.0) * 30;

            // Set the rounded value back to the slider
            RestTimeSlider.Value = timeInSeconds;

            Preferences.Set("DefaultRestTime", timeInSeconds);
            UpdateRestTimeLabel(timeInSeconds);
        }

        private void UpdateRestTimeLabel(double seconds)
        {
            int minutes = (int)seconds / 60;
            int remainingSeconds = (int)seconds % 60;

            // Round down to the nearest 30-second interval
            remainingSeconds = remainingSeconds - (remainingSeconds % 30);

            if (minutes > 0)
            {
                if (remainingSeconds == 0)
                {
                    RestTimeLabel.Text = $"{minutes} minute{(minutes > 1 ? "s" : "")}";
                }
                else
                {
                    RestTimeLabel.Text = $"{minutes} minute{(minutes > 1 ? "s" : "")} {remainingSeconds} second{(remainingSeconds > 1 ? "s" : "")}";
                }
            }
            else
            {
                RestTimeLabel.Text = $"{remainingSeconds} second{(remainingSeconds > 1 ? "s" : "")}";
            }
        }


        private void OnMeasurementPickerChanged(object sender, EventArgs e)
        {
            var selectedIndex = MeasurementPicker.SelectedIndex;
            Preferences.Set("MeasurementUnit", selectedIndex);
        }

        private void OnRestTimeSwitchToggled(object sender, ToggledEventArgs e)
        {
            Preferences.Set("EnableRestTimer", e.Value);
        }
    }
}
