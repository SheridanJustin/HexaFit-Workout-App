using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Dispatching;
using Newtonsoft.Json;
using WorkoutApplication.Models;
using System.Net.Mail;

namespace WorkoutApplication
{
    [QueryProperty(nameof(SelectedExercise), "SelectedExercise")]
    [QueryProperty(nameof(SelectedWorkout), "SelectedWorkout")]

    public partial class MainPage : ContentPage
    {

        private List<WorkoutSession> workoutPlans;
        private WorkoutSession currentPlan;
        private int workoutCount = 0; // Store the workout count
        private string TitleThatWorks;
        private Label _timerLabel;
        private WorkoutSession _currentWorkoutSession;
        private List<Exercise> _exercises = new List<Exercise>();

        public MainPage ReturnPage { get; set; } // Reference to the MainPage that called this
        public StackLayout ExerciseLayout { get; set; } // The layout where the exercise will be added


        private System.Timers.Timer _timer;
        private TimeSpan _elapsedTime;
        private readonly object _lock = new object(); // Prevents thread race conditions



        public Exercise SelectedExercise
        {
            get => null; // No need to return anything
            set
            {
                if (value != null)
                {
                    // Add the selected exercise with two default sets
                    value.Sets = new List<Set>
                {
                    new Set { SetNumber = 1, Weight = 0, Reps = 0 },
                    new Set { SetNumber = 2, Weight = 0, Reps = 0 }
                };

                    // Add the exercise to the current list of exercises
                    _currentWorkoutSession.Exercises.Add(value);

                    // Update the workout session display to include the newly added exercise
                    DisplayWorkoutPlan(_currentWorkoutSession);
                }
            }
        }






        private void StartTimer()
        {
            // If a timer already exists, stop and dispose of it before creating a new one
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }

            _elapsedTime = TimeSpan.Zero;

            _timer = new System.Timers.Timer { Interval = 1000 }; // 1 second interval
            _timer.Elapsed += OnTimedEvent;
            _timer.AutoReset = true;
            _timer.Start();
        }


        private void OnTimedEvent(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (_lock) // Ensures thread safety
            {
                _elapsedTime = _elapsedTime.Add(TimeSpan.FromSeconds(1));

                // Update the timer label on the UI thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _timerLabel.Text = _elapsedTime.ToString(@"hh\:mm\:ss");
                });
            }
        }

        private void StopTimer()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }


        public class WorkoutPlan
        {
            public string Title { get; set; }
            public int DaysPerWeek { get; set; }
            public List<DayPlan> Plan { get; set; }
        }

        public class DayPlan
        {
            public string Day { get; set; }
            public string WorkoutType { get; set; }
            public List<Exercise> Exercises { get; set; }
        }


        public MainPage()
        {
            InitializeComponent();

            LoadWorkoutCount();

            if (workoutPlans == null || workoutPlans.Count == 0)
            {
                DisplayNoWorkoutMessage(); // Display default message if no workout is loaded
            }
        }

        private WorkoutSession _selectedWorkout;

        public WorkoutSession SelectedWorkout
        {
            get => _selectedWorkout;
            set
            {
                _selectedWorkout = value;
                if (_selectedWorkout != null)
                {
                    DisplayWorkoutPlan(_selectedWorkout);
                }
            }
        }



        // Display a message and button to select a workout
        private void DisplayNoWorkoutMessage()
        {


           PlanContent.Children.Clear();

            var image = new Image
            {
                Source = "machoman2.png",
                HeightRequest = 200,
                WidthRequest = 200,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 50, 0, 80)
            };

            var messageLabel = new Label
            {
                Text = "Select a workout from the templates page.",
                TextColor = Color.FromArgb("#ffffff"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Start, // Align to the top, respecting the margin
                FontSize = 16,
                Margin = new Thickness(0, -20, 0, 30) // Margin from the top
            };

            var selectButton = new Button
            {
                Text = "Go to Templates",
                BackgroundColor = Color.FromArgb("#4CAF50"),
                TextColor = Color.FromArgb("#ffffff"),
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 10,
                HeightRequest = 50,
                Margin = new Thickness(0, 10, 0, 0), // Margin from the bottom of the label
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Start, // Align to the top, respecting the margin
            };

            // Add the Clicked event with animation
            selectButton.Clicked += async (s, e) =>
            {
                // Scale down the button to 90% of its size
                await selectButton.ScaleTo(0.9, 100, Easing.CubicIn);

                // Scale it back to its original size
                await selectButton.ScaleTo(1, 100, Easing.CubicOut);


                // Navigate to the templates page after the animation
                await Shell.Current.GoToAsync("//Templates");
            };



            PlanContent.Children.Add(image);
            PlanContent.Children.Add(messageLabel);
            PlanContent.Children.Add(selectButton);
        }



        private async void OnAddExerciseBelow(StackLayout currentExerciseLayout)
        {
            // Navigate to SelectExercisePage and wait for the user to select an exercise
            await Navigation.PushAsync(new SelectExercisePage());
        }

        public void AddSelectedExercise(Exercise selectedExercise, StackLayout currentExerciseLayout)
        {
            if (selectedExercise != null)
            {
                // Add two default sets to the newly added exercise
                selectedExercise.Sets = new List<Set>
        {
            new Set { SetNumber = 1, Weight = 0, Reps = 0 },
            new Set { SetNumber = 2, Weight = 0, Reps = 0 }
        };

                // Create the new exercise frame
                var newExerciseFrame = CreateExerciseFrame(selectedExercise);

                // Find the index of the current exercise frame
                int currentFrameIndex = PlanContent.Children.IndexOf(currentExerciseLayout.Parent as Frame);

                // Insert the new exercise frame right after the current one
                PlanContent.Children.Insert(currentFrameIndex + 1, newExerciseFrame);

                // Add the new exercise to the current workout session's exercises list
                _currentWorkoutSession.Exercises.Add(selectedExercise);
            }
        }


        public async void DisplayWorkoutPlan(WorkoutSession session)
        {
            _currentWorkoutSession = session;
            PlanContent.Children.Clear();

            TitleThatWorks = session.Title;

            if (session == null)
            {
                PlanContent.Children.Add(new Label
                {
                    Text = "No workout plan available.",
                    TextColor = Color.FromArgb("#ffffff"),
                    HorizontalOptions = LayoutOptions.Center
                });
                return;
            }

            var uiElements = new List<View>();

            // Grid to organize the timer, title, and cancel button
            var titleGrid = new Grid
            {
                ColumnDefinitions =
        {
            new ColumnDefinition { Width = GridLength.Star },
            new ColumnDefinition { Width = GridLength.Auto }
        },
                RowDefinitions =
        {
            new RowDefinition { Height = GridLength.Auto },
            new RowDefinition { Height = GridLength.Auto }
        }
            };


            var HeaderFrame = new Frame
            {
                BorderColor = Color.FromRgb(140, 140, 140),
                BackgroundColor = Color.FromArgb("#333333"),
                CornerRadius = 10,
                Padding = new Thickness(10),
                Margin = new Thickness(0, 5, 0, 10),
                HasShadow = true
            };


            _timerLabel = new Label
            {
                Text = "00:00:00",
                FontAttributes = FontAttributes.Bold,
                FontSize = 18,
                TextColor = Color.FromArgb("#cccccc"),
                Margin = new Thickness(10, 0, 10, 0),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start
            };

            var titleLabel = new Label
            {
                Text = session.Title,
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(10, 0, 10, 0),
                FontSize = 20,
                TextColor = Color.FromArgb("#ffffff"),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                BackgroundColor = Color.FromArgb("#FF0000"),
                TextColor = Color.FromArgb("#ffffff"),
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.End,
                Padding = new Thickness(10)
            };


           

            cancelButton.Clicked += (s, e) => OnCancelButtonClicked(s, e, session);

            titleGrid.Children.Add(titleLabel);
            Grid.SetRow(titleLabel, 0);
            Grid.SetColumn(titleLabel, 0);

            titleGrid.Children.Add(cancelButton);
            Grid.SetRow(cancelButton, 0);
            Grid.SetColumn(cancelButton, 1);

            titleGrid.Children.Add(_timerLabel);
            Grid.SetRow(_timerLabel, 1);
            Grid.SetColumn(_timerLabel, 0);

            HeaderFrame.Content = titleGrid;

            uiElements.Add(HeaderFrame);
            StartTimer();

            foreach (var exercise in session.Exercises)
            {
                var exerciseFrame = new Frame
                {
                    BorderColor = Color.FromRgb(140, 140, 140),
                    BackgroundColor = Color.FromArgb("#333333"),
                    CornerRadius = 10,
                    Padding = new Thickness(10,0,0,10),
                    Margin = new Thickness(0, 5, 0, 10),
                    HasShadow = true
                };

                var exerciseLayout = new StackLayout
                {
                    Spacing = 5,
                    Margin = new Thickness(0, 5, 0, 7)
                };

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
                    VerticalOptions = LayoutOptions.Center
                };

                var muscleGroupLabel = new Label
                {
                    Text = exercise.MuscleGroup.ToUpper(),
                    TextColor = Color.FromArgb("#FFFFFF"),
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center
                };

                muscleGroupFrame.Content = muscleGroupLabel;
                muscleGroupGrid.Children.Add(muscleGroupFrame);
                Grid.SetColumn(muscleGroupFrame, 0);

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
                    string action = await DisplayActionSheet("Options", "Cancel", null, "Delete Exercise", "Rename Exercise", "Add Set", "Add Exercise Below");

                    if (action == "Delete Exercise")
                    {
                        PlanContent.Children.Remove(exerciseFrame);
                    }
                    else if (action == "Rename Exercise")
                    {
                        string newName = await DisplayPromptAsync("Rename Exercise", "Enter the new name for the exercise:", "OK", "Cancel", "Exercise Name");

                        if (!string.IsNullOrEmpty(newName))
                        {
                            var exerciseNameLabel = exerciseLayout.Children.OfType<Label>().FirstOrDefault();
                            if (exerciseNameLabel != null)
                            {
                                exerciseNameLabel.Text = newName;
                            }
                        }
                    }
                    else if (action == "Add Set")
                    {
                        AddSetToExercise(exerciseLayout);
                    }
                    else if (action == "Add Exercise Below")
                    {
                        OnAddExerciseBelow(exerciseLayout); // Add the new exercise below the current one
                    }
                };

                muscleGroupGrid.Children.Add(menuButton);
                Grid.SetColumn(menuButton, 1);

                exerciseLayout.Children.Add(muscleGroupGrid);

                exerciseLayout.Children.Add(new Label
                {
                    Text = exercise.Name,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 18,
                    TextColor = Color.FromArgb("#ffffff")
                });

                var labelGrid = new Grid
                {
                    ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto }
            },
                    Margin = new Thickness(0, 5, 0, 0)
                };

                var weightLabel = new Label
                {
                    Text = "Weight",
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 14,
                    TextColor = Color.FromArgb("#D3D3D3"),
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(25, 0, 0, 0)
                };
                labelGrid.Children.Add(weightLabel);
                Grid.SetColumn(weightLabel, 1);

                var repsLabel = new Label
                {
                    Text = "Reps",
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 14,
                    TextColor = Color.FromArgb("#D3D3D3"),
                    HorizontalOptions = LayoutOptions.Start,
                    Margin = new Thickness(38, 0, 0, 0)
                };
                labelGrid.Children.Add(repsLabel);
                Grid.SetColumn(repsLabel, 2);

                exerciseLayout.Children.Add(labelGrid);

                foreach (var set in exercise.Sets)
                {
                    var grid = new Grid
                    {
                        ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                        Margin = new Thickness(0, 5, 0, 5)
                    };

                    var actionButton = new Button
                    {
                        ImageSource = "menu.png",
                        BackgroundColor = Color.FromRgba(0, 0, 0, 0),
                        WidthRequest =40,
                        HeightRequest = 40,
                        Padding = new Thickness(5),
                        Margin = new Thickness(0, 0, 10, 0)
                    };

                    actionButton.Clicked += async (s, e) =>
                    {
                        string action = await DisplayActionSheet("Options", "Cancel", null, "Remove Set", "Add Set Below");

                        if (action == "Remove Set")
                        {
                            exerciseLayout.Children.Remove(grid);
                        }
                        else if (action == "Add Set Below")
                        {
                            AddSetBelow(grid, exerciseLayout);
                        }
                    };

                    int measurementUnitIndex = Preferences.Get("MeasurementUnit", 0);
                    string weightUnit = measurementUnitIndex == 0 ? "lbs" : "kg";


                    var weightEntry = new Entry
                    {
                        Placeholder = set.PreviousWeight > 0 ? $"{set.PreviousWeight} {weightUnit}" : $"{(measurementUnitIndex == 0 ? "Pounds/Lb" : "Kilograms")}",
                        Keyboard = Keyboard.Numeric,
                        TextColor = Color.FromArgb("#ffffff"),
                        BackgroundColor = Color.FromArgb("#2A2A2A"),
                        WidthRequest = 100,
                        HeightRequest = 35,
                        Margin = new Thickness(10, 5, 0, 0)
                    };

                    var weightEntryFrame = new Frame
                    {
                        Padding = new Thickness(1),
                        BackgroundColor = Color.FromArgb("#2A2A2A"),
                        WidthRequest = 100,
                        HeightRequest = 35,
                        BorderColor = Color.FromRgb(140, 140, 140),
                        HasShadow = false
                    };

                    weightEntryFrame.Content = weightEntry;

                    var repsEntry = new Entry
                    {
                        Placeholder = set.PreviousReps > 0 ? $"{set.PreviousReps} reps" : "Reps",
                        Keyboard = Keyboard.Numeric,
                        TextColor = Color.FromArgb("#ffffff"),
                        BackgroundColor = Color.FromArgb("#2A2A2A"),
                        WidthRequest = 75,
                        HeightRequest = 35,
                        Margin = new Thickness(10, 5, 0, 0)
                    };

                    var repsEntryFrame = new Frame
                    {
                        Padding = new Thickness(1),
                        BackgroundColor = Color.FromArgb("#2A2A2A"),
                        WidthRequest = 75,
                        HeightRequest = 35,
                        BorderColor = Color.FromRgb(140, 140, 140),
                        HasShadow = false
                    };

                    repsEntryFrame.Content = repsEntry;

                    var checkButton = CreateCheckButton(weightEntry, repsEntry);

                    grid.Children.Add(actionButton);
                    Grid.SetColumn(actionButton, 0);

                    grid.Children.Add(weightEntryFrame);
                    Grid.SetColumn(weightEntryFrame, 1);

                    grid.Children.Add(repsEntryFrame);
                    Grid.SetColumn(repsEntryFrame, 2);

                    grid.Children.Add(checkButton);
                    Grid.SetColumn(checkButton, 3);

                    exerciseLayout.Children.Add(grid);
                }

                exerciseFrame.Content = exerciseLayout;
                uiElements.Add(exerciseFrame);
            }

            var finishButton = new Button
            {
                Text = "Finish Workout",
                BackgroundColor = Color.FromArgb("#4CAF50"),
                TextColor = Color.FromArgb("#ffffff"),
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 10,
                HeightRequest = 50,
                Margin = new Thickness(0, 20, 0, 0),
                HorizontalOptions = LayoutOptions.FillAndExpand
            };

            finishButton.Clicked += (s, e) => OnFinishButtonClicked(s, e, session);

            uiElements.Add(finishButton);

            // Manually add each element to PlanContent.Children
            foreach (var element in uiElements)
            {
                PlanContent.Children.Add(element);
            }
        }

        private Button CreateCheckButton(Entry weightEntry, Entry repsEntry)
        {
            var checkButton = new Button
            {
                Text = "✔", // Placeholder text, you can replace with an icon if needed
                TextColor = Color.FromRgba("#212121"),
                BackgroundColor = Color.FromRgba(0, 0, 0, 0), // Fully transparent background
                WidthRequest = 35, // Adjust the size as needed
                HeightRequest = 35,
                Padding = new Thickness(5), // Add padding to ensure the icon is centered
                Margin = new Thickness(0, 0, 10, 0), // Add space to the right (10 units in this example)
                IsEnabled = false // Initially disabled
            };

            weightEntry.TextChanged += (s, e) => ValidateSetCompletion(weightEntry, repsEntry, checkButton);
            repsEntry.TextChanged += (s, e) => ValidateSetCompletion(weightEntry, repsEntry, checkButton);

            checkButton.Clicked += async (s, e) =>
            {
                // If the button is already green, it means it has been pressed before
                if (checkButton.BackgroundColor == Color.FromArgb("#4CAF50"))
                {
                    return; // Exit early, preventing further clicks
                }

                // Perform the animation
                await checkButton.ScaleTo(0.9, 100, Easing.CubicIn);
                await checkButton.ScaleTo(1, 100, Easing.CubicOut);

                // Check if the rest timer is enabled
                bool isRestTimerEnabled = Preferences.Get("EnableRestTimer", true);

                if (isRestTimerEnabled)
                {
                    // Navigate to the RestTimerPage
                    await Navigation.PushAsync(new RestTimerPage());
                }

                // Mark the button as pressed by changing its color and disabling it
                checkButton.BackgroundColor = Color.FromArgb("#4CAF50"); // Green color
                checkButton.TextColor = Color.FromArgb("#ffffff");
                checkButton.IsEnabled = false; // Disable the button to prevent further clicks
            };

            return checkButton;
        }




        private void AddSetBelow(Grid gridAbove, StackLayout exerciseLayout)
        {
            var grid = new Grid
            {
                ColumnDefinitions =
        {
            new ColumnDefinition { Width = GridLength.Auto }, // Icon Button
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, // Weight Entry
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, // Reps Entry
            new ColumnDefinition { Width = GridLength.Auto }  // Checkmark Button
        },
                Margin = new Thickness(0, 5, 0, 5)
            };

            var actionButton = new Button
            {
                ImageSource = "menu.png",
                BackgroundColor = Colors.Transparent, // Fully transparent background
                WidthRequest = 40,
                HeightRequest = 40,
                Padding = new Thickness(5),
                Margin = new Thickness(0, 0, 10, 0) // Add space to the right
            };

            actionButton.Clicked += async (s, e) =>
            {
                string action = await DisplayActionSheet("Options", "Cancel", null, "Remove Set", "Add Set Below");

                if (action == "Remove Set")
                {
                    // Remove the current grid (set)
                    exerciseLayout.Children.Remove(grid);
                }
                else if (action == "Add Set Below")
                {
                    // Add a new set below the current one
                    AddSetBelow(grid, exerciseLayout);
                }
            };

            // Define the Entry for weight
            var weightEntry = new Entry
            {
                Placeholder = $"Pounds/Lb",
                Keyboard = Keyboard.Numeric,
                TextColor = Color.FromArgb("#ffffff"),  // White color
                BackgroundColor = Color.FromArgb("#2A2A2A"),  // Darker background for input
                WidthRequest = 100, // Adjust the size as needed
                HeightRequest = 35,
                Margin = new Thickness(10, 5, 0, 0)

            };



            // Define the Frame for the weight entry
            var weightEntryFrame = new Frame
            {
                Padding = new Thickness(1), // Set padding to create a border effect
                BackgroundColor = Color.FromArgb("#2A2A2A"),
                WidthRequest = 100, // Adjust the size as needed
                HeightRequest = 35,
                BorderColor = Color.FromRgb(140, 140, 140), // Set the border color
                HasShadow = false // Optional: Disable the shadow if you only want the border
            };

            // Add the weightEntry to the weightEntryFrame
            weightEntryFrame.Content = weightEntry;

            var repsEntry = new Entry
            {
                Placeholder = $"Reps",
                Keyboard = Keyboard.Numeric,
                TextColor = Color.FromArgb("#ffffff"),  // White color
                BackgroundColor = Color.FromArgb("#2A2A2A"),  // Darker background for input
                WidthRequest = 75, // Adjust the size as needed
                HeightRequest = 35,
                Margin = new Thickness(10, 5, 0, 0)
            };

            var repsEntryFrame = new Frame
            {
                Padding = new Thickness(1), // Set padding to create a border effect
                BackgroundColor = Color.FromArgb("#2A2A2A"), // Set background color
                WidthRequest = 75, // Adjust the size as needed
                HeightRequest = 35,
                BorderColor = Color.FromRgb(140, 140, 140), // Set the border color
                HasShadow = false // Optional: Disable the shadow if you only want the border
            };



            // Add the repsEntry to the repsEntryFrame
            repsEntryFrame.Content = repsEntry;


            Button checkmarkButton = CreateCheckButton(weightEntry, repsEntry);

            // Add the Button (icon), Weight, Reps Entry, and Checkmark Button to the grid
            grid.Children.Add(actionButton);
            Grid.SetColumn(actionButton, 0);

            grid.Children.Add(weightEntryFrame);
            Grid.SetColumn(weightEntryFrame, 1);

            grid.Children.Add(repsEntryFrame);
            Grid.SetColumn(repsEntryFrame, 2);

            grid.Children.Add(checkmarkButton);
            Grid.SetColumn(checkmarkButton, 3);

            // Add the grid below the existing grid
            int index = exerciseLayout.Children.IndexOf(gridAbove);
            exerciseLayout.Children.Insert(index + 1, grid);
        } // Full method involoving everything needed to create a set below

        private void ValidateSetCompletion(Entry weightEntry, Entry repsEntry, Button checkmarkButton)
        {
            if (!string.IsNullOrEmpty(weightEntry.Text) && !string.IsNullOrEmpty(repsEntry.Text))
            {
                checkmarkButton.IsEnabled = true;
                checkmarkButton.BackgroundColor = Color.FromArgb("#999999"); // Gray background when both entries are filled
            }
            else
            {
                checkmarkButton.IsEnabled = false;
                checkmarkButton.BackgroundColor = Color.FromRgba(0, 0, 0, 0); // Fully transparent when disabled
            }
        }

        private async void OnFinishButtonClicked(object sender, EventArgs e, WorkoutSession workoutSession)
        {
            try
            {
                // Collect workout data and check for empty sets
                workoutSession = CollectWorkoutData(workoutSession, out bool hasEmptySets);

                if (workoutSession == null)
                {
                    await DisplayAlert("Warning", "Please input at least one set!", "Ok");
                    return;
                }

                if (hasEmptySets)
                {
                    bool proceed = await DisplayAlert("Warning", "Some exercises have empty sets and will not be saved. Do you want to continue?", "Yes", "No");
                    if (!proceed)
                    {
                        return; // Exit if the user doesn't want to proceed
                    }
                }

                string json = JsonConvert.SerializeObject(workoutSession, Formatting.Indented);

#if ANDROID
        var path = Android.App.Application.Context.GetExternalFilesDir(Android.OS.Environment.DirectoryDocuments).AbsolutePath;

        if (path == null)
        {
            Console.WriteLine("Error: path is null.");
            return;
        }

        string historyFileName = $"History_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_Workout.json";
        string filePath = Path.Combine(path, historyFileName);

        if (filePath == null)
        {
            Console.WriteLine("Error: filePath is null.");
            return;
        }

        // Save the new workout session
        File.WriteAllText(filePath, json);

        // Update the original workout file with the latest data
        UpdateOriginalWorkoutFile(workoutSession, path);

        // Increment and save the workout count
        workoutCount++;
        SaveWorkoutCount();
        StopTimer();

        // Clear the content after successfully saving the workout
        PlanContent.Children.Clear();
        DisplayNoWorkoutMessage();

        // Flag the workout as inactive
        App.IsWorkoutActive = false;

        await DisplayAlert("Workout Finished", $"You have completed {workoutCount} workouts. View previous workouts under the history tab.", "OK");
#else
                await DisplayAlert("Error", "Saving workouts is not supported on this platform.", "OK");
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
                await DisplayAlert("Error", "An unexpected error occurred.", "OK");
            }
        }





        private void UpdateOriginalWorkoutFile(WorkoutSession completedSession, string directory)
        {
            // Construct the file path directly using the Id
            string originalWorkoutFilePath = Path.Combine(directory, $"{completedSession.Id}.json");

            if (!File.Exists(originalWorkoutFilePath))
            {
                Console.WriteLine($"Error: Original workout file with Id {completedSession.Id} not found.");
                return;
            }

            // Load the original workout session
            var originalWorkoutJson = File.ReadAllText(originalWorkoutFilePath);
            var originalWorkout = JsonConvert.DeserializeObject<WorkoutSession>(originalWorkoutJson);

            // Update the original workout with the latest data
            foreach (var completedExercise in completedSession.CompletedExercises)
            {
                var originalExercise = originalWorkout.Exercises.FirstOrDefault(e => e.Name == completedExercise.Name);

                if (originalExercise != null)
                {
                    for (int i = 0; i < completedExercise.Sets.Count; i++)
                    {
                        var completedSet = completedExercise.Sets[i];
                        var originalSet = originalExercise.Sets[i];

                        if (originalSet != null)
                        {
                            originalSet.PreviousWeight = completedSet.Weight;
                            originalSet.PreviousReps = completedSet.Reps;
                        }
                    }
                }
            }

            originalWorkout.LastPerformed = DateTime.Now.ToString("yyyy-MM-dd");

            // Save the updated workout session back to the original file
            var updatedWorkoutJson = JsonConvert.SerializeObject(originalWorkout, Formatting.Indented);
            File.WriteAllText(originalWorkoutFilePath, updatedWorkoutJson);
        }



        private WorkoutSession CollectWorkoutData(WorkoutSession workoutSession, out bool hasEmptySets)
        {
            hasEmptySets = false;


            workoutSession.Date = DateTime.Today.ToString("yyyy-MM-dd");
            Console.WriteLine($"CollectWorkoutData DATE = {workoutSession.Date}");

            foreach (var exerciseFrame in PlanContent.Children)
            {
                if (exerciseFrame is Frame frame && frame.Content is StackLayout exerciseLayout)
                {
                    var exerciseName = ((Label)exerciseLayout.Children[1]).Text;

                    if (string.IsNullOrEmpty(exerciseName))
                    {
                        Console.WriteLine("Error: exerciseName is null or empty.");
                        continue;
                    }

                    var completedExercise = new CompletedExercise
                    {
                        Name = exerciseName,
                        Sets = new List<CompletedSet>()
                    };

                    foreach (var setGrid in exerciseLayout.Children.OfType<Grid>())
                    {
                        if (setGrid.Children.Count == 4)
                        {
                            var weightEntry = ((Frame)setGrid.Children[1]).Content as Entry;
                            var repsEntry = ((Frame)setGrid.Children[2]).Content as Entry;

                            if (!string.IsNullOrEmpty(weightEntry?.Text) && !string.IsNullOrEmpty(repsEntry?.Text))
                            {
                                var completedSet = new CompletedSet
                                {
                                    Weight = double.Parse(weightEntry.Text),
                                    Reps = int.Parse(repsEntry.Text),
                                    Completed = true
                                };

                                completedExercise.Sets.Add(completedSet);
                            }
                            else
                            {
                                hasEmptySets = true;
                            }
                        }
                    }

                    if (completedExercise.Sets.Count > 0)
                    {
                        workoutSession.CompletedExercises.Add(completedExercise);
                    }
                }
            }

            if (workoutSession.CompletedExercises.Count == 0)
            {
                return null;
            }

            return workoutSession;
        }



        private async void OnCancelButtonClicked(object sender, EventArgs e, WorkoutSession workoutSession)
        {
            try
            {
                var workoutSessionWithSets = CollectWorkoutData(workoutSession, out bool hasEmptySets);

                if (workoutSessionWithSets != null)
                {
                    bool proceed = await DisplayAlert("Warning", "Some sets have data that will be lost. Do you want to continue?", "Yes", "No");
                    if (!proceed)
                    {
                        return; // Exit if the user doesn't want to proceed
                    }
                }

                // Stop the timer and clear the content
                StopTimer();
                PlanContent.Children.Clear();
                DisplayNoWorkoutMessage();

                // Flag the workout as inactive
                App.IsWorkoutActive = false;

                await DisplayAlert("Workout Canceled", "Your workout has been canceled.", "OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
                await DisplayAlert("Error", "An unexpected error occurred.", "OK");
            }
        }



        private void AddSetToExercise(StackLayout exerciseLayout)
        {
            // Create a grid to hold the Button (icon), Weight, Reps entries, and Checkmark Button side by side
            var grid = new Grid
            {
                ColumnDefinitions =
        {
            new ColumnDefinition { Width = GridLength.Auto }, // Icon Button
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, // Weight Entry
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, // Reps Entry
            new ColumnDefinition { Width = GridLength.Auto }  // Checkmark Button
        },
                Margin = new Thickness(0, 5, 0, 5)
            };

            // Create the action button for set options
            var actionButton = new Button
            {
                ImageSource = "menu.png", // Replace with your icon file
                BackgroundColor = Color.FromRgba(0, 0, 0, 0), // Fully transparent background
                WidthRequest = 60,
                HeightRequest = 60,
                Padding = new Thickness(5), // Add padding to ensure the icon is centered
                Margin = new Thickness(0, 0, 10, 0) // Add space to the right (10 units in this example)
            };

            actionButton.Clicked += async (s, e) =>
            {
                string action = await DisplayActionSheet("Options", "Cancel", null, "Remove Set", "Add Set Below");

                if (action == "Remove Set")
                {
                    // Remove the current grid (set)
                    exerciseLayout.Children.Remove(grid);
                }
                else if (action == "Add Set Below")
                {
                    // Add a new set below the current one
                    AddSetBelow(grid, exerciseLayout);
                }
            };

            // Entry for weight with frame
            var weightEntryFrame = new Frame
            {
                Padding = new Thickness(1), // Set padding to create a border effect
                BackgroundColor = Color.FromArgb("#2A2A2A"),
                WidthRequest = 100, // Adjust the size as needed
                HeightRequest = 35,
                BorderColor = Color.FromRgb(140, 140, 140), // Set the border color
                Content = new Entry
                {
                    Placeholder = "Pounds/Lb",
                    Keyboard = Keyboard.Numeric,
                    TextColor = Color.FromArgb("#FFFFFF"),  // White color
                    BackgroundColor = Color.FromArgb("#2A2A2A"),  // Darker background for input
                    WidthRequest = 100, // Adjust the size as needed
                    HeightRequest = 35,
                    Margin = new Thickness(5, 5, 0, 0)
                }
            };

            // Entry for reps with frame
            var repsEntryFrame = new Frame
            {
                Padding = new Thickness(1), // Set padding to create a border effect
                BackgroundColor = Color.FromArgb("#2A2A2A"), // Set background color to gray
                WidthRequest = 75, // Adjust the size as needed
                HeightRequest = 35,
                BorderColor = Color.FromRgb(140, 140, 140), // Set the border color
                Content = new Entry
                {
                    Placeholder = "Reps",
                    Keyboard = Keyboard.Numeric,
                    TextColor = Color.FromArgb("#FFFFFF"),  // White color
                    BackgroundColor = Color.FromArgb("#2A2A2A"),  // Darker background for input
                    WidthRequest = 75, // Adjust the size as needed
                    HeightRequest = 35,
                    Margin = new Thickness(5, 5, 0, 0)
                }
            };

            // Checkmark Button
            var checkmarkButton = new Button
            {
                Text = "✔", // Placeholder text, you can replace with an icon if needed
                TextColor = Color.FromRgba("#333333"),
                BackgroundColor = Color.FromRgba(0, 0, 0, 0), // Fully transparent background
                WidthRequest = 35, // Adjust the size as needed
                HeightRequest = 35,
                Padding = new Thickness(5), // Add padding to ensure the icon is centered
                Margin = new Thickness(0, 0, 10, 0), // Add space to the right (10 units in this example)
                IsEnabled = false // Initially disabled
            };

            checkmarkButton.Clicked += (s, e) =>
            {
                // Mark the set as completed (change color to green)
                checkmarkButton.BackgroundColor = Color.FromArgb("#4CAF50");
                checkmarkButton.TextColor = Color.FromArgb("#ffffff");
                checkmarkButton.IsEnabled = false; // Disable the button after checking
            };

            // Enable the checkmark button only if both weight and reps entries are not null or empty
            var weightEntry = (Entry)weightEntryFrame.Content;
            var repsEntry = (Entry)repsEntryFrame.Content;
            weightEntry.TextChanged += (s, e) => ValidateSetCompletion(weightEntry, repsEntry, checkmarkButton);
            repsEntry.TextChanged += (s, e) => ValidateSetCompletion(weightEntry, repsEntry, checkmarkButton);

            // Add the Button (icon), Weight, Reps Entry, and Checkmark Button to the grid
            grid.Children.Add(actionButton);
            Grid.SetColumn(actionButton, 0);

            grid.Children.Add(weightEntryFrame);
            Grid.SetColumn(weightEntryFrame, 1);

            grid.Children.Add(repsEntryFrame);
            Grid.SetColumn(repsEntryFrame, 2);

            grid.Children.Add(checkmarkButton);
            Grid.SetColumn(checkmarkButton, 3);

            // Add the new grid to the exercise layout
            exerciseLayout.Children.Add(grid);
        }


        private async void AddExerciseBelow(StackLayout exerciseLayout)
        {
            // Prompt the user to enter a new exercise name
            string newExerciseName = await DisplayPromptAsync("New Exercise", "Enter the name for the new exercise:", "OK", "Cancel", "Exercise Name");

            if (string.IsNullOrEmpty(newExerciseName))
            {
                return; // Exit if the user cancels or doesn't enter a name
            }

            // Create a new Exercise object
            var newExercise = new Exercise
            {
                Name = newExerciseName,
                MuscleGroup = "New Muscle Group", // You can set this dynamically if needed
                Sets = new List<Set> { new Set { Reps = 0, Weight = 0 } } // Start with one default set
            };

            // Create a new frame for the exercise
            var newExerciseFrame = CreateExerciseFrame(newExercise);

            // Find the index of the current exercise frame
            int currentFrameIndex = PlanContent.Children.IndexOf(exerciseLayout.Parent as Frame);

            // Insert the new exercise frame right after the current one
            PlanContent.Children.Insert(currentFrameIndex + 1, newExerciseFrame);
        }

        // Method to create a new exercise frame
        private Frame CreateExerciseFrame(Exercise exercise)
        {
            var exerciseFrame = new Frame
            {
                BorderColor = Color.FromRgb(140, 140, 140),
                BackgroundColor = Color.FromArgb("#333333"),
                CornerRadius = 10,
                Padding = new Thickness(10),
                Margin = new Thickness(0, 5, 0, 10),
                HasShadow = true
            };

            var exerciseLayout = new StackLayout
            {
                Spacing = 5,
                Margin = new Thickness(0, 5, 0, 7)
            };

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
                VerticalOptions = LayoutOptions.Center
            };

            var muscleGroupLabel = new Label
            {
                Text = exercise.MuscleGroup.ToUpper(),
                TextColor = Color.FromArgb("#FFFFFF"),
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center
            };

            muscleGroupFrame.Content = muscleGroupLabel;
            muscleGroupGrid.Children.Add(muscleGroupFrame);
            Grid.SetColumn(muscleGroupFrame, 0);

            var menuButton = new Button
            {
                ImageSource = "menu.png",
                BackgroundColor = Color.FromRgba(0, 0, 0, 0),
                WidthRequest = 60,
                HeightRequest = 60,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.End
            };

            menuButton.Clicked += async (s, e) =>
            {
                string action = await DisplayActionSheet("Options", "Cancel", null, "Delete Exercise", "Rename Exercise", "Add Set", "Add Exercise2 Below");

                if (action == "Delete Exercise")
                {
                    PlanContent.Children.Remove(exerciseFrame);
                }
                else if (action == "Rename Exercise")
                {
                    string newName = await DisplayPromptAsync("Rename Exercise", "Enter the new name for the exercise:", "OK", "Cancel", "Exercise Name");

                    if (!string.IsNullOrEmpty(newName))
                    {
                        var exerciseNameLabel = exerciseLayout.Children.OfType<Label>().FirstOrDefault();
                        if (exerciseNameLabel != null)
                        {
                            exerciseNameLabel.Text = newName;
                        }
                    }
                }
                else if (action == "Add Set")
                {
                    AddSetToExercise(exerciseLayout);
                }
                else if (action == "Add Exercise Below")
                {
                    AddExerciseBelow(exerciseLayout);
                }
            };

            muscleGroupGrid.Children.Add(menuButton);
            Grid.SetColumn(menuButton, 1);

            exerciseLayout.Children.Add(muscleGroupGrid);

            exerciseLayout.Children.Add(new Label
            {
                Text = exercise.Name,
                FontAttributes = FontAttributes.Bold,
                FontSize = 18,
                TextColor = Color.FromArgb("#ffffff")
            });

            var labelGrid = new Grid
            {
                ColumnDefinitions =
        {
            new ColumnDefinition { Width = GridLength.Auto },
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
            new ColumnDefinition { Width = GridLength.Auto }
        },
                Margin = new Thickness(0, 5, 0, 0)
            };

            var weightLabel = new Label
            {
                Text = "Weight",
                FontAttributes = FontAttributes.Bold,
                FontSize = 14,
                TextColor = Color.FromArgb("#D3D3D3"),
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(25, 0, 0, 0)
            };
            labelGrid.Children.Add(weightLabel);
            Grid.SetColumn(weightLabel, 1);

            var repsLabel = new Label
            {
                Text = "Reps",
                FontAttributes = FontAttributes.Bold,
                FontSize = 14,
                TextColor = Color.FromArgb("#D3D3D3"),
                HorizontalOptions = LayoutOptions.Start,
                Margin = new Thickness(38, 0, 0, 0)
            };
            labelGrid.Children.Add(repsLabel);
            Grid.SetColumn(repsLabel, 2);

            exerciseLayout.Children.Add(labelGrid);

            // Add the default set
            AddSetToExercise(exerciseLayout);

            exerciseFrame.Content = exerciseLayout;
            return exerciseFrame;
        }






        private void LoadWorkoutCount()
        {
            workoutCount = Preferences.Get("WorkoutCount", 0); // Default to 0 if not found
        }


        private void SaveWorkoutCount()
        {
            Preferences.Set("WorkoutCount", workoutCount);
        }




    }
}
