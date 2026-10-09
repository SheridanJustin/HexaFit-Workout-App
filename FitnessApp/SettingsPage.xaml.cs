namespace FitnessApp;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        VersionLabel.Text = $"HexaFit {AppInfo.Current.VersionString}";
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshStats();
    }

    private static string[] GetHistoryFiles()
    {
        string directoryPath = FileSystem.AppDataDirectory;
        return Directory.Exists(directoryPath)
            ? Directory.GetFiles(directoryPath, "History_*.json")
            : Array.Empty<string>();
    }

    private void RefreshStats()
    {
        WorkoutCountLabel.Text = Preferences.Get("WorkoutCount", 0).ToString();
        HistoryCountLabel.Text = GetHistoryFiles().Length.ToString();
    }

    private async void OnResetCounterClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Reset Counter", "Set your completed workout count back to 0? Your saved history is kept.", "Reset", "Cancel");
        if (!confirm)
        {
            return;
        }

        Preferences.Set("WorkoutCount", 0);
        RefreshStats();
    }

    private async void OnDeleteHistoryClicked(object sender, EventArgs e)
    {
        var files = GetHistoryFiles();
        if (files.Length == 0)
        {
            await DisplayAlert("Nothing to Delete", "You have no saved workouts.", "OK");
            return;
        }

        bool confirm = await DisplayAlert("Delete All History", $"Permanently delete all {files.Length} saved workouts? Your templates and their previous weights are kept.", "Delete", "Cancel");
        if (!confirm)
        {
            return;
        }

        foreach (var file in files)
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not delete {file}: {ex.Message}");
            }
        }

        RefreshStats();
    }
}
