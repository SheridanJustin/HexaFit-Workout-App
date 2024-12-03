namespace WorkoutApplication
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
            // Define the target directory
            string targetDirectory = "/storage/emulated/0/Android/data/com.companyname.workoutapplication/files/Documents/";

            // Check if the files already exist before copying
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
