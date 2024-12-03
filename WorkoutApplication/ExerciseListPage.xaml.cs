using System;
using System.Linq;
using Microsoft.Maui.Controls;
using WorkoutApplication.Models;

namespace WorkoutApplication
{
    public partial class ExerciseListPage : ContentPage
    {
        private SelectExercisePageViewModel ViewModel { get; }

        public ExerciseListPage()
        {
            InitializeComponent();
            ViewModel = new SelectExercisePageViewModel();
            BindingContext = ViewModel;
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.SearchQuery = e.NewTextValue;
        }

        private async void OnExerciseSelected(object sender, SelectionChangedEventArgs e)
        {
            var selectedExercise = e.CurrentSelection.FirstOrDefault() as Exercise;
            if (selectedExercise != null)
            {
                // Navigate to exercise details page or perform any desired action
                await DisplayAlert("Selected Exercise", $"You selected: {selectedExercise.Name}", "OK");
            }

            // Clear the selection
            ExerciseCollectionView.SelectedItem = null;
        }

        private async void OnAddExerciseTapped(object sender, EventArgs e)
        {
            string exerciseName = await DisplayPromptAsync("New Exercise", "Enter the name for the new exercise:");

            if (string.IsNullOrWhiteSpace(exerciseName))
            {
                await DisplayAlert("Error", "Exercise name cannot be empty.", "OK");
                return;
            }

            string[] muscleGroups = { "Chest", "Back", "Triceps", "Biceps", "Shoulders", "Quads", "Glutes", "Hamstrings", "Calves", "Traps", "Forearms", "Abs" };
            string selectedMuscleGroup = await DisplayActionSheet("Select Muscle Group", "Cancel", null, muscleGroups);

            if (selectedMuscleGroup == "Cancel" || string.IsNullOrEmpty(selectedMuscleGroup))
            {
                return;
            }

            var newExercise = new Exercise
            {
                Name = exerciseName,
                ImagePath = "muscle.svg",
                MuscleGroup = selectedMuscleGroup
            };

            ViewModel.AddExercise(newExercise);

            await DisplayAlert("Success", "The exercise was successfully added!", "OK");
        }





    }
}