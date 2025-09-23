namespace FitnessApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("Workout", typeof(MainPage));
        }

    }
}





