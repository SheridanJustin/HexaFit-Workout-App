using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FitnessApp.Models;
using Newtonsoft.Json;

namespace FitnessApp
{
    [QueryProperty(nameof(SelectedExercise), "SelectedExercise")]
    public partial class CreateCustomWorkoutPage : ContentPage
    {
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

        public CreateCustomWorkoutPage()
        {
            InitializeComponent();
            _editor = new TemplateEditor(this, CustomExerciseLayout);
            TitleLabel.Text = "Create Workout";
        }

        private async void OnAddExerciseClicked(object sender, EventArgs e)
        {
            var addButton = sender as Button;

            await addButton.ScaleTo(0.9, 100, Easing.CubicIn);
            await addButton.ScaleTo(1.0, 100, Easing.CubicOut);

            await Navigation.PushAsync(new SelectExercisePage());
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

        private async void OnSaveButtonTapped(object sender, EventArgs e)
        {
            var exercises = _editor.BuildExercises(out string invalidMessage);
            if (invalidMessage != null)
            {
                await DisplayAlert("Check your sets", invalidMessage, "OK");
                return;
            }

            if (exercises.Count == 0)
            {
                await DisplayAlert("Error", "Add at least one exercise.", "OK");
                return;
            }

            string workoutTitle = await DisplayPromptAsync("Save Workout", "Enter the name for your workout:", "OK", "Cancel", "Workout Name");
            if (workoutTitle == null)
            {
                return; // Cancelled
            }

            if (string.IsNullOrWhiteSpace(workoutTitle))
            {
                await DisplayAlert("Error", "Workout name cannot be empty.", "OK");
                return;
            }

            workoutTitle = workoutTitle.Trim();
            var directoryPath = FileSystem.AppDataDirectory;

            var session = new WorkoutSession
            {
                Id = CreateUniqueId(directoryPath, workoutTitle),
                Title = workoutTitle,
                Date = DateTime.Today.ToString("yyyy-MM-dd"),
                LastPerformed = string.Empty,
                Exercises = exercises,
                CompletedExercises = new List<CompletedExercise>()
            };

            try
            {
                Directory.CreateDirectory(directoryPath);
                string filePath = Path.Combine(directoryPath, $"{session.Id}.json");
                File.WriteAllText(filePath, JsonConvert.SerializeObject(session, Formatting.Indented));
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

        // Two templates with the same name get different files instead of overwriting each other
        private static string CreateUniqueId(string directoryPath, string title)
        {
            string slug = Regex.Replace(title.ToLowerInvariant(), "[^a-z0-9]+", "_").Trim('_');
            string baseId = $"CustomTemplate_{(string.IsNullOrEmpty(slug) ? "workout" : slug)}";

            string id = baseId;
            int suffix = 2;
            while (File.Exists(Path.Combine(directoryPath, $"{id}.json")))
            {
                id = $"{baseId}_{suffix++}";
            }

            return id;
        }
    }
}
