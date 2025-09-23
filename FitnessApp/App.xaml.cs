namespace FitnessApp
{
    public partial class App : Application
    {
        public static bool IsWorkoutActive { get; set; } = false;

        public App()
        {
            InitializeComponent();

            // Perform any initialization logic
            CopyJsonFilesToDevice();

            MainPage = new AppShell();
        }

        private async void CopyJsonFilesToDevice()
        {
            // Safe app-specific directory
            string targetDirectory = FileSystem.AppDataDirectory;

            if (!File.Exists(Path.Combine(targetDirectory, "workout1.json")))
            {
                await JsonFileHelper.CopyJsonFileToDeviceAsync("workout1.json", targetDirectory);
            }

            if (!File.Exists(Path.Combine(targetDirectory, "workout2.json")))
            {
                await JsonFileHelper.CopyJsonFileToDeviceAsync("workout2.json", targetDirectory);
            }
        }

    }
}
