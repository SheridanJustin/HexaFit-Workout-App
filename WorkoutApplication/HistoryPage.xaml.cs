using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Maui.Controls;
using WorkoutApplication.Models;

namespace WorkoutApplication
{
    public partial class HistoryPage : ContentPage
    {
        public HistoryPage()
        {
            InitializeComponent();
            LoadWorkoutHistory();
        }

        // Refresh button event handler 
        private async void OnRefreshButtonClicked(object sender, EventArgs e)
        {
            var refreshButton = sender as Button;

            // Scale down the button to 90% of its size
            await refreshButton.ScaleTo(0.9, 100, Easing.CubicIn);

            // Scale it back to its original size
            await refreshButton.ScaleTo(1, 100, Easing.CubicOut);

            // Reload the workout history
            LoadWorkoutHistory();
        }


        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Automatically refresh the workout history every time the page appears
            LoadWorkoutHistory();
        }

        private void DisplayNoWorkoutMessage()
        {
            HistoryContent.Children.Clear();

            var messageLabel = new Label
            {
                Text = "No workouts saved.",
                TextColor = Color.FromArgb("#ffffff"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.CenterAndExpand,
                FontSize = 18,
                Margin = new Thickness(0, 20, 0, 10)
            };

            HistoryContent.Children.Add(messageLabel);
        }

        private void LoadWorkoutHistory()
        {



            string directoryPath = "/storage/emulated/0/Android/data/com.companyname.workoutapplication/files/Documents";

            if (!Directory.Exists(directoryPath))
            {
                DisplayNoWorkoutMessage(); // Display a message if the directory does not exist
                return;
            }

            var files = Directory.GetFiles(directoryPath, "History_*.json");

            // Clear the existing history UI before reloading
            HistoryContent.Children.Clear();


            // Create the Frame
            Frame frame = new Frame
            {
                BorderColor = Color.FromHex("#8C8C8C"),
                BackgroundColor = Color.FromHex("#333333"),
                CornerRadius = 10,
                Padding = new Thickness(10),
                Margin = new Thickness(0),
                HasShadow = true
            };

            // Create the Label
            Label label = new Label
            {
                Text = "Previous Workouts",
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center,
                TextColor = Color.FromHex("#e74040")
            };

            // Add the Label to the Frame
            frame.Content = label;

            HistoryContent.Children.Add(frame);


            if (files.Length == 0)
            {
                DisplayNoWorkoutMessage(); // Display a message if no workout history files are found
                return;
            }

            // Sort files by their last modified date in descending order
            var sortedFiles = files.OrderByDescending(File.GetLastWriteTime).ToArray();

            foreach (var file in sortedFiles)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var workoutSession = JsonConvert.DeserializeObject<WorkoutSession>(json);

                    if (workoutSession != null)
                    {
                        DisplayHistoryPlan(file, workoutSession); // Pass the file path to DisplayHistoryPlan
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading file {file}: {ex.Message}");
                }
            }
        }




        private void DisplayHistoryPlan(string filePath, WorkoutSession session)
        {


            // Add the workout date and title at the top
            var workoutTitleLayout = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            var workoutTitleLabel = new Label
            {
                Text = $"{session.Title} - {session.Date}",
                FontAttributes = FontAttributes.Bold,
                FontSize = 20,
                TextColor = Color.FromArgb("#ffffff"),
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(2, 0, 0, 0),
                HorizontalOptions = LayoutOptions.StartAndExpand
            };

            var menuButton = new Button
            {
                ImageSource = "menu.png",
                BackgroundColor = Colors.Transparent,
                WidthRequest = 62,
                HeightRequest = 62,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.End
            };

            menuButton.Clicked += async (s, e) =>
            {
                string action = await DisplayActionSheet("Options", "Cancel", null, "Delete Workout");

                if (action == "Delete Workout")
                {
                    bool confirm = await DisplayAlert("Delete Confirmation", "Are you sure you want to delete this workout history?", "Yes", "No");
                    if (confirm)
                    {
                        DeleteWorkoutHistory(filePath);
                        LoadWorkoutHistory(); // Refresh the history after deletion
                    }
                }
            };


            workoutTitleLayout.Children.Add(workoutTitleLabel);
            workoutTitleLayout.Children.Add(menuButton);

            HistoryContent.Children.Add(workoutTitleLayout);

            // Create a frame to hold all exercises of the workout
            var workoutFrame = new Frame
            {
                BorderColor = Color.FromRgb(140, 140, 140),
                BackgroundColor = Color.FromArgb("#333333"), // Background color of the frame
                CornerRadius = 10, // Optional: Set a corner radius
                Padding = new Thickness(10), // Padding inside the frame
                Margin = new Thickness(0, 0, 0, 0), // Margin outside the frame
                HasShadow = true // Optional: Enable shadow
            };

            var workoutLayout = new StackLayout
            {
                Spacing = 5,
                Margin = new Thickness(0, 0, 0, 0)
            };

            foreach (var exercise in session.CompletedExercises)
            {
                var muscleGroupGrid = new Grid
                {
                    ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
                };

                var muscleGroupFrame = new Frame
                {
                    BorderColor = Color.FromArgb("#FF0000"),
                    BackgroundColor = Color.FromArgb("#4A2A2A"),
                    CornerRadius = 5,
                    Padding = new Thickness(5),
                    HasShadow = false,
                    Margin = new Thickness(0, 5, 0, 7),
                    HorizontalOptions = LayoutOptions.Start,
                    VerticalOptions = LayoutOptions.Center,
                    Content = new Label
                    {
                        Text = exercise.Name.ToUpper(),
                        TextColor = Color.FromArgb("#FFFFFF"),
                        FontAttributes = FontAttributes.Bold,
                        VerticalOptions = LayoutOptions.Center
                    }
                };

                muscleGroupGrid.Children.Add(muscleGroupFrame);
                Grid.SetColumn(muscleGroupFrame, 0);

                workoutLayout.Children.Add(muscleGroupGrid);

                foreach (var set in exercise.Sets)
                {
                    var setLayout = new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        Spacing = 10,
                        Margin = new Thickness(0, 5, 0, 5)
                    };

                    // Label for the weight
                    var weightLabel = new Label
                    {
                        Text = $"{set.Weight} lbs  X",
                        FontSize = 14,
                        TextColor = Color.FromArgb("#ffffff"),
                        VerticalOptions = LayoutOptions.Center
                    };

                    // Label for the reps
                    var repsLabel = new Label
                    {
                        Text = $"{set.Reps} reps",
                        FontSize = 14,
                        TextColor = Color.FromArgb("#ffffff"),
                        VerticalOptions = LayoutOptions.Center
                    };

                    setLayout.Children.Add(weightLabel);
                    setLayout.Children.Add(repsLabel);

                    workoutLayout.Children.Add(setLayout);
                }
            }
           
            workoutFrame.Content = workoutLayout;

            HistoryContent.Children.Add(workoutFrame);
        }



        private void DeleteWorkoutHistory(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            // After deletion, check if there are any files left
            string directoryPath = Path.GetDirectoryName(filePath);
            var remainingFiles = Directory.GetFiles(directoryPath, "*.json");

            if (remainingFiles.Length == 0)
            {
                // If no files remain, clear the HistoryContent
                HistoryContent.Children.Clear();
            }
        }


    }

}
