using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Linq;
using FitnessApp.Models;
using Newtonsoft.Json;

namespace FitnessApp
{
    [QueryProperty(nameof(SelectedExercise), "SelectedExercise")]
    public partial class EditWorkoutPage : ContentPage
    {
        private readonly WorkoutSession _workoutSession;
        private readonly TemplateEditor _editor;

        // Set when the exercise picker returns. The new card is appended without touching the others.
        public Exercise SelectedExercise
        {
            get => null; // No need to return anything
            set
            {
                if (value != null)
                {
                    _editor.AddExercise(value, withDefaultSets: true);
                }
            }
        }

        public EditWorkoutPage(WorkoutSession workoutSession)
        {
            InitializeComponent();

            _workoutSession = workoutSession;
            _editor = new TemplateEditor(this, CustomExerciseLayout);
            TitleLabel.Text = "Edit Workout";

            foreach (var exercise in workoutSession?.Exercises ?? new List<Exercise>())
            {
                _editor.AddExercise(exercise, withDefaultSets: false);
            }
        }

        private async void OnSaveButtonTapped(object sender, EventArgs e)
        {
            if (_workoutSession == null)
            {
                return;
            }

            var exercises = _editor.BuildExercises();

            if (exercises.Count == 0)
            {
                await DisplayAlert("Error", "Add at least one exercise.", "OK");
                return;
            }

            string workoutTitle = await DisplayPromptAsync("Save Workout", "Enter the name for your workout:", initialValue: _workoutSession.Title);
            if (workoutTitle == null)
            {
                return; // Cancelled
            }

            if (string.IsNullOrWhiteSpace(workoutTitle))
            {
                await DisplayAlert("Error", "Workout name cannot be empty.", "OK");
                return;
            }

            _workoutSession.Title = workoutTitle.Trim();
            _workoutSession.Exercises = exercises;

            try
            {
                // Templates live in AppDataDirectory, the same place the Templates page reads them from
                string filePath = Path.Combine(FileSystem.AppDataDirectory, $"{_workoutSession.Id}.json");
                File.WriteAllText(filePath, JsonConvert.SerializeObject(_workoutSession, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving workout: {ex}");
                await DisplayAlert("Error", $"Unable to save workout: {ex.Message}", "OK");
                return;
            }

            await DisplayAlert("Success", "Workout saved successfully!", "OK");
            await Navigation.PopAsync();
        }

        private async void OnCloseButtonTapped(object sender, EventArgs e)
        {
            var closeButton = sender as Button;

            await closeButton.ScaleTo(0.8, 100, Easing.CubicIn);
            await closeButton.ScaleTo(1.0, 100, Easing.CubicOut);

            bool confirm = await DisplayAlert("Confirm", "Are you sure you want to cancel? This action cannot be undone.", "Yes", "No");
            if (confirm)
            {
                await Navigation.PopAsync();
            }
        }

        private async void OnAddExerciseClicked(object sender, EventArgs e)
        {
            var addButton = sender as Button;

            await addButton.ScaleTo(0.9, 100, Easing.CubicIn);
            await addButton.ScaleTo(1.0, 100, Easing.CubicOut);

            await Navigation.PushAsync(new SelectExercisePage());
        }
    }
}
