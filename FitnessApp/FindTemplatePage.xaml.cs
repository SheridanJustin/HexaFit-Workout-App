using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Newtonsoft.Json;
using FitnessApp.Models;

namespace FitnessApp
{
    public partial class FindTemplatePage : ContentPage
    {
        public FindTemplatePage()
        {
            InitializeComponent();
            LoadWorkoutTemplates();
            LoadCustomWorkoutTemplates(); // Load custom templates here
            CheckCustomTemplates();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Reload the workout templates every time the page appears
            LoadWorkoutTemplates();
            LoadCustomWorkoutTemplates(); // Also reload custom templates
        }

        private void LoadCustomWorkoutTemplates()
        {
            CustomTemplates.Children.Clear(); // Clear the CustomTemplates layout

            try
            {
                var workoutSessions = LoadCustomWorkoutSessions();

                if (workoutSessions != null && workoutSessions.Count > 0)
                {
                    foreach (var session in workoutSessions)
                    {
                        DisplayCustomWorkoutTemplate(session); // Display each custom template
                    }
                }
                else
                {
                    // If no custom workouts found, display a message
                    var noWorkoutsLabel = new Label
                    {
                        Text = "No custom workouts found.",
                        FontSize = 16,
                        TextColor = Color.FromArgb("#ffffff"),
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.CenterAndExpand,
                        Margin = new Thickness(0, 10, 0, 10)
                    };

                    CustomTemplates.Children.Add(noWorkoutsLabel);
                }
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"Error loading custom workout templates: {ex.Message}", "OK");
                Console.WriteLine($"Error loading custom workout templates: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }

            // Ensure the AddWorkoutButton is always visible
            AddWorkoutButton.IsVisible = true;
        }

        private List<WorkoutSession> LoadCustomWorkoutSessions()
        {
            var workoutSessions = new List<WorkoutSession>();

            try
            {
                var directoryPath = FileSystem.AppDataDirectory; // ✅ fixed
                var files = Directory.GetFiles(directoryPath, "CustomTemplate*.json");

                foreach (var file in files)
                {
                    var json = File.ReadAllText(file);

                    var session = JsonConvert.DeserializeObject<WorkoutSession>(json);

                    if (session != null && !string.IsNullOrEmpty(session.Title) && session.Exercises != null && session.Exercises.Count > 0)
                    {
                        workoutSessions.Add(session);
                    }
                    else
                    {
                        Console.WriteLine($"Skipping invalid or empty workout session from file: {file}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading custom workout sessions: {ex.Message}");
                throw;
            }

            return workoutSessions;
        }

        private void DisplayCustomWorkoutTemplate(WorkoutSession session)
        {
            if (session == null)
            {
                Console.WriteLine("Error: session is null in DisplayCustomWorkoutTemplate.");
                return;
            }

            var workoutFrame = CreateWorkoutFrame(session);

            if (workoutFrame != null)
            {
                var tapGestureRecognizer = new TapGestureRecognizer();
                tapGestureRecognizer.Tapped += async (s, e) =>
                {
                    if (App.IsWorkoutActive)
                    {
                        bool proceed = await DisplayAlert("Warning", "A workout is currently active. Starting a new workout will delete the previous one. Do you want to continue?", "Yes", "No");
                        if (!proceed) return;
                    }

                    await workoutFrame.ScaleTo(0.9, 100, Easing.CubicIn);
                    await workoutFrame.ScaleTo(1, 100, Easing.CubicOut);

                    LoadingOverlay.IsVisible = true;
                    LoadingIndicator.IsRunning = true;

                    await Task.Delay(500);

                    var queryParameters = new Dictionary<string, object>
                    {
                        { "SelectedWorkout", session }
                    };

                    await Shell.Current.GoToAsync("//MainPage", queryParameters);
                    App.IsWorkoutActive = true;

                    LoadingOverlay.IsVisible = false;
                    LoadingIndicator.IsRunning = false;
                };
                workoutFrame.GestureRecognizers.Add(tapGestureRecognizer);

                CustomTemplates.Children.Add(workoutFrame);
            }
        }

        private async void OnAddWorkoutButtonClicked(object sender, EventArgs e)
        {
            var addWorkoutButton = sender as Button;

            await addWorkoutButton.ScaleTo(0.9, 100, Easing.CubicIn);
            await addWorkoutButton.ScaleTo(1, 100, Easing.CubicOut);

            await Navigation.PushAsync(new CreateCustomWorkoutPage());
        }

        private void CheckCustomTemplates()
        {
            AddWorkoutButton.IsVisible = CustomTemplates.Children.Count == 0;
        }

        private void LoadWorkoutTemplates()
        {
            SampleTemplates.Children.Clear();
            try
            {
                var workoutSessions = LoadWorkoutSessions();

                if (workoutSessions != null && workoutSessions.Count > 0)
                {
                    foreach (var session in workoutSessions)
                    {
                        DisplayWorkoutTemplate(session);
                    }
                }
                else
                {
                    SampleTemplates.Children.Add(new Label
                    {
                        Text = "No workout templates found.",
                        TextColor = Color.FromArgb("#ffffff"),
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.CenterAndExpand
                    });
                }
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"Error loading workout templates: {ex.Message}", "OK");
                Console.WriteLine($"Error loading workout templates: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }

        private List<WorkoutSession> LoadWorkoutSessions()
        {
            var workoutSessions = new List<WorkoutSession>();

            try
            {
                var directoryPath = FileSystem.AppDataDirectory; // ✅ fixed
                var files = Directory.GetFiles(directoryPath, "workout*.json");

                foreach (var file in files)
                {
                    var json = File.ReadAllText(file);
                    var session = JsonConvert.DeserializeObject<WorkoutSession>(json);

                    if (session != null && !string.IsNullOrEmpty(session.Title) && session.Exercises != null && session.Exercises.Count > 0)
                    {
                        workoutSessions.Add(session);
                    }
                    else
                    {
                        Console.WriteLine($"Skipping invalid or empty workout session from file: {file}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading workout sessions: {ex.Message}");
                throw;
            }

            return workoutSessions;
        }

        private void DisplayWorkoutTemplate(WorkoutSession session)
        {
            if (session == null)
            {
                Console.WriteLine("Error: session is null in DisplayWorkoutTemplate.");
                return;
            }

            var workoutFrame = CreateWorkoutFrame(session);

            if (workoutFrame != null)
            {
                var tapGestureRecognizer = new TapGestureRecognizer();
                tapGestureRecognizer.Tapped += async (s, e) =>
                {
                    if (App.IsWorkoutActive)
                    {
                        bool proceed = await DisplayAlert("Warning", "A workout is currently active. Starting a new workout will delete the previous one. Do you want to continue?", "Yes", "No");
                        if (!proceed) return;
                    }

                    await workoutFrame.ScaleTo(0.9, 100, Easing.CubicIn);
                    await workoutFrame.ScaleTo(1, 100, Easing.CubicOut);

                    LoadingOverlay.IsVisible = true;
                    LoadingIndicator.IsRunning = true;

                    await Task.Delay(500);

                    var queryParameters = new Dictionary<string, object>
                    {
                        { "SelectedWorkout", session }
                    };

                    await Shell.Current.GoToAsync("//MainPage", queryParameters);
                    App.IsWorkoutActive = true;

                    LoadingOverlay.IsVisible = false;
                    LoadingIndicator.IsRunning = false;
                };
                workoutFrame.GestureRecognizers.Add(tapGestureRecognizer);

                SampleTemplates.Children.Add(workoutFrame);
            }
        }

        private Frame CreateWorkoutFrame(WorkoutSession session)
        {
            var workoutFrame = new Frame
            {
                BorderColor = Color.FromRgb(140, 140, 140),
                BackgroundColor = Color.FromArgb("#333333"),
                CornerRadius = 10,
                Padding = new Thickness(10, 0, 0, 10),
                Margin = new Thickness(0, 5, 0, 10),
                HasShadow = true
            };

            var workoutLayout = new StackLayout
            {
                Spacing = 5
            };

            var titleLabelFrame = new Frame
            {
                BorderColor = Color.FromArgb("#FF0000"),
                BackgroundColor = Color.FromArgb("#4A2A2A"),
                CornerRadius = 5,
                Padding = new Thickness(5),
                HasShadow = false,
                Margin = new Thickness(0, 10, 0, 7),
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Center
            };

            var titleLabel = new Label
            {
                Text = session.Title.ToUpper(),
                TextColor = Color.FromArgb("#FFFFFF"),
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
            };

            titleLabelFrame.Content = titleLabel;

            var titleGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            titleGrid.Children.Add(titleLabelFrame);

            var menuButton = new Button
            {
                ImageSource = "menu.png",
                BackgroundColor = Colors.Transparent,
                WidthRequest = 60,
                HeightRequest = 60,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.End
            };

            menuButton.Clicked += async (s, e) =>
            {
                string action = await DisplayActionSheet("Options", "Cancel", null, "Rename", "Edit", "Delete");

                if (action == "Delete")
                {
                    bool confirm = await DisplayAlert("Delete Confirmation", "Are you sure you want to delete this workout template?", "Yes", "No");
                    if (confirm)
                    {
                        DeleteWorkoutTemplate(session);

                        LoadWorkoutTemplates();
                        LoadCustomWorkoutTemplates();
                    }
                }
                else if (action == "Rename")
                {
                    string newName = await DisplayPromptAsync("Rename Workout", "Enter the new name for the workout:", "OK", "Cancel", session.Title);

                    if (!string.IsNullOrEmpty(newName) && newName != session.Title)
                    {
                        var directoryPath = FileSystem.AppDataDirectory; // ✅ fixed
                        var oldFilePath = Path.Combine(directoryPath, $"{session.Id}.json");

                        session.Title = newName;

                        var updatedJson = JsonConvert.SerializeObject(session, Formatting.Indented);
                        File.WriteAllText(oldFilePath, updatedJson);

                        if (titleLabel != null)
                        {
                            titleLabel.Text = newName.ToUpper();
                        }
                    }
                }
                else if (action == "Edit")
                {
                    LoadingOverlay.IsVisible = true;
                    LoadingIndicator.IsRunning = true;

                    await Navigation.PushAsync(new EditWorkoutPage(session));

                    LoadingOverlay.IsVisible = false;
                    LoadingIndicator.IsRunning = false;
                }
            };

            titleGrid.Children.Add(menuButton);

            workoutLayout.Children.Add(titleGrid);

            var lastPerformedLabel = new Label
            {
                Text = $"Last Performed: {session.LastPerformed}",
                FontSize = 14,
                TextColor = Color.FromArgb("#D3D3D3")
            };
            workoutLayout.Children.Add(lastPerformedLabel);

            foreach (var exercise in session.Exercises)
            {
                var exerciseLabel = new Label
                {
                    Text = $"{exercise.Name} - {exercise.Sets.Count} sets",
                    FontSize = 14,
                    TextColor = Color.FromArgb("#ffffff")
                };
                workoutLayout.Children.Add(exerciseLabel);
            }

            workoutFrame.Content = workoutLayout;

            return workoutFrame;
        }

        private void DeleteWorkoutTemplate(WorkoutSession session)
        {
            try
            {
                var filename = $"{session.Id}.json";
                var directoryPath = FileSystem.AppDataDirectory; // ✅ fixed
                var filePath = Path.Combine(directoryPath, filename);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Console.WriteLine($"Deleted workout template file: {filePath}");
                }
                else
                {
                    Console.WriteLine($"File not found: {filePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting workout template: {ex.Message}");
            }
        }
    }
}
