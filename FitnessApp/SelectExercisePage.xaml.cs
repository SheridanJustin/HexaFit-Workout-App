using System;
using System.Web;
using Microsoft.Maui.Controls;
using Newtonsoft.Json;
using FitnessApp.Models;

namespace FitnessApp
{
    public partial class SelectExercisePage : ContentPage
    {
        public SelectExercisePageViewModel ViewModel { get; set; }

        private Timer _searchTimer;

        public SelectExercisePage()
        {
            InitializeComponent();
            ViewModel = new SelectExercisePageViewModel();
            BindingContext = ViewModel;
        }

        // This method will handle the search query when the user types in the SearchBar
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_searchTimer != null)
                _searchTimer.Dispose();

            _searchTimer = new Timer(FilterExercises, e.NewTextValue, 300, Timeout.Infinite);
        }

        private async void OnAddExerciseTapped(object sender, EventArgs e)
        {
            // Check if the sender is an Image or Label depending on where the tap is applied
            var view = sender as View; // Generalize to any view that supports gestures (Label, Image, etc.)
            if (view != null)
            {
                // Add animation (scaling down and up)
                await view.ScaleTo(0.6, 100, Easing.CubicIn);
                await view.ScaleTo(1.0, 100, Easing.CubicOut);
            }

            // Show a prompt to get the exercise name
            string exerciseName = await DisplayPromptAsync("New Exercise", "Enter the name for the new exercise:");

            if (string.IsNullOrWhiteSpace(exerciseName))
            {
                await DisplayAlert("Error", "Exercise name cannot be empty.", "OK");
                return;
            }

            // Display a list of muscle groups for the user to select
            string[] muscleGroups = { "Chest", "Back", "Triceps", "Biceps", "Shoulders", "Quads", "Glutes", "Hamstrings", "Calves", "Traps", "Forearms", "Abs" };
            string selectedMuscleGroup = await DisplayActionSheet("Select Muscle Group", "Cancel", null, muscleGroups);

            if (selectedMuscleGroup == "Cancel" || string.IsNullOrEmpty(selectedMuscleGroup))
            {
                return; // User canceled the muscle group selection
            }

            // Add the new exercise with a default image and the selected muscle group
            var newExercise = new Exercise
            {
                Name = exerciseName,
                ImagePath = "muscle.svg", // Default image
                MuscleGroup = selectedMuscleGroup   // Selected muscle group
            };

            ViewModel.AddExercise(newExercise);

            // Show success screen (or message)
            await DisplaySuccessScreen();
        }

        private async Task DisplaySuccessScreen()
        {
            // Success message
            await DisplayAlert("Success", "The exercise was successfully added!", "OK");
        }





        private void FilterExercises(object state)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                ViewModel.SearchQuery = (string)state;
            });
        }



        // This method is called when the user selects an exercise
        private async void OnExerciseSelected(object sender, SelectionChangedEventArgs e)
        {
            var selectedExercise = e.CurrentSelection.FirstOrDefault() as Exercise;
            if (selectedExercise != null)
            {
                var queryParameters = new Dictionary<string, object>
        {
            { "SelectedExercise", selectedExercise }
        };

                // Instead of navigating forward, pop back to the CreateCustomWorkoutPage and pass the selected exercise
                await Shell.Current.GoToAsync("..", queryParameters); // ".." pops the stack and goes back to the previous page
            }
        }

       
    }
}
