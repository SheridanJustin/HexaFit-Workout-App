using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Linq;
using WorkoutApplication.Models;
using Newtonsoft.Json;
using System.Web;
using WorkoutApplication.Models;


namespace WorkoutApplication
{


    [QueryProperty(nameof(SelectedExercise), "SelectedExercise")]
    public partial class CreateCustomWorkoutPage : ContentPage
    {
        private Exercise _selectedExercise; // Backing field for the property
        private WorkoutSession _workoutSession;
        private List<Exercise> _exercises = new List<Exercise>(); // Keep track of added exercises

        public Exercise SelectedExercise
        {
            get => null; // No need to return anything
            set
            {
                if (value != null)
                {
                    _exercises.Add(value); // Add the new exercise to the list
                    DisplayExercises();     // Display all exercises including the new one
                }
            }
        }

        private void DisplayExercises()
        {
            CustomExerciseLayout.Children.Clear(); // Clear the layout first

            foreach (var exercise in _exercises)
            {
                // Add each exercise to the layout
                var exerciseFrame = CreateExerciseFrame(exercise);
                CustomExerciseLayout.Children.Add(exerciseFrame);

                // Add two default sets to each exercise
                var exerciseLayout = exerciseFrame.Content as StackLayout;
                if (exerciseLayout != null)
                {
                    AddSetToExercise(exerciseLayout, new Set { SetNumber = 1, Weight = 0, Reps = 0 });
                    AddSetToExercise(exerciseLayout, new Set { SetNumber = 2, Weight = 0, Reps = 0 });
                }
            }
        }

        public CreateCustomWorkoutPage(Exercise selectedExercise = null)
        {
            InitializeComponent();

            // Check if CustomExerciseLayout is properly initialized
            if (CustomExerciseLayout == null)
            {
                Console.WriteLine("CustomExerciseLayout is null after InitializeComponent.");
            }

            if (selectedExercise != null)
            {
                Console.WriteLine($"SelectedExercise received: {selectedExercise.Name}");
                SelectedExercise = selectedExercise;
            }
            else
            {
                Console.WriteLine("SelectedExercise is null in constructor.");
            }
        }


        public CreateCustomWorkoutPage()
        {
            InitializeComponent();
        }



        public CreateCustomWorkoutPage(WorkoutSession workoutSession = null)
        {
            InitializeComponent();

            if (workoutSession != null)
            {
                _workoutSession = workoutSession;
                TitleLabel.Text = "Edit Workout";

                // Populate _exercises list with pre-existing exercises from the workout session
                _exercises = workoutSession.Exercises.ToList(); // Add pre-existing exercises to the list

                LoadWorkoutSession(workoutSession); // Load existing exercises into the UI
            }
            else
            {
                TitleLabel.Text = "Create Workout";
            }
        }



        private void LoadWorkoutSession(WorkoutSession session)
        {
            CustomExerciseLayout.Children.Clear(); // Clear the layout first

            foreach (var exercise in session.Exercises)
            {
                // Add each exercise to the layout
                var exerciseFrame = CreateExerciseFrame(exercise);
                CustomExerciseLayout.Children.Add(exerciseFrame);

                // Add the corresponding number of sets
                var exerciseLayout = exerciseFrame.Content as StackLayout;
                if (exerciseLayout != null)
                {
                    foreach (var set in exercise.Sets)
                    {
                        AddSetToExercise(exerciseLayout, set);
                    }
                }
            }
        }


        private void AddSetToExercise(StackLayout exerciseLayout, Set set)
        {
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
        {
            new ColumnDefinition { Width = GridLength.Auto }, // Icon Button
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, // Weight Entry
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, // Reps Entry
        },
                Margin = new Thickness(0, 5, 0, 5)
            };

            // Create the action button for set options
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
                    Placeholder = "Weight", // Always show "Weight" as the placeholder
                    Text = string.Empty, // Leave the text field empty
                    Keyboard = Keyboard.Numeric,
                    TextColor = Colors.White,
                    BackgroundColor = Color.FromArgb("#2A2A2A"),
                    WidthRequest = 100,
                    HeightRequest = 35,
                    Margin = new Thickness(5, 5, 0, 0),
                }
            };

            // Entry for reps with frame
            var repsEntryFrame = new Frame
            {
                Padding = new Thickness(1),
                BackgroundColor = Color.FromArgb("#2A2A2A"),
                WidthRequest = 75,
                HeightRequest = 35,
                BorderColor = Color.FromRgb(140, 140, 140),
                Content = new Entry
                {
                    Placeholder = "Reps", // Always show "Reps" as the placeholder
                    Text = string.Empty, // Leave the text field empty
                    Keyboard = Keyboard.Numeric,
                    TextColor = Colors.White,
                    BackgroundColor = Color.FromArgb("#2A2A2A"),
                    WidthRequest = 75,
                    HeightRequest = 35,
                    Margin = new Thickness(5, 5, 0, 0),
                }
            };


            // Add the Button (icon), Weight, and Reps Entry to the grid
            grid.Children.Add(actionButton);
            Grid.SetColumn(actionButton, 0);

            grid.Children.Add(weightEntryFrame);
            Grid.SetColumn(weightEntryFrame, 1);

            grid.Children.Add(repsEntryFrame);
            Grid.SetColumn(repsEntryFrame, 2);

            // Add the new grid to the exercise layout
            exerciseLayout.Children.Add(grid);
        }

        private async void OnAddExerciseClicked(object sender, EventArgs e)
        {
            var addButton = sender as Button;

            // Perform the scale down animation
            await addButton.ScaleTo(0.9, 100, Easing.CubicIn);

            // Perform the scale back to original size
            await addButton.ScaleTo(1.0, 100, Easing.CubicOut);

            // Navigate to the SelectExercisePage
            await Navigation.PushAsync(new SelectExercisePage());
        }


        private Frame CreateExerciseFrame(Exercise exercise)
        {

            Console.WriteLine($"Creating Exercise Frame for: {exercise.Name}");

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
                BackgroundColor = Color.FromRgba(0, 0, 0, 0),
                WidthRequest = 60,
                HeightRequest = 60,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.End
            };

            // Add the provided menu to each newly created exercise
            menuButton.Clicked += async (s, e) =>
            {
                var currentIndex = _exercises.IndexOf(exercise);
                var menuOptions = new List<string> { "Delete Exercise", "Rename Exercise", "Add Set" };

                if (currentIndex > 0)
                {
                    menuOptions.Add("Move Exercise Up");
                }

                if (currentIndex < _exercises.Count - 1)
                {
                    menuOptions.Add("Move Exercise Down");
                }

                string action = await DisplayActionSheet("Options", "Cancel", null, menuOptions.ToArray());

                if (action == "Delete Exercise")
                {
                    _exercises.Remove(exercise);
                    CustomExerciseLayout.Children.Remove(exerciseFrame);
                }
                else if (action == "Rename Exercise")
                {
                    string newName = await DisplayPromptAsync("Rename Exercise", "Enter the new name for the exercise:", "OK", "Cancel", "Exercise Name");
                    if (!string.IsNullOrEmpty(newName))
                    {
                        exercise.Name = newName;
                        var exerciseNameLabel = exerciseLayout.Children.OfType<Label>().FirstOrDefault();
                        if (exerciseNameLabel != null)
                        {
                            exerciseNameLabel.Text = newName;
                        }
                    }
                }
                else if (action == "Add Set")
                {
                    AddSetToExercise(exerciseLayout, new Set { SetNumber = exercise.Sets.Count + 1 });
                }
                else if (action == "Move Exercise Up")
                {
                    // Swap the current exercise with the one above it
                    _exercises.RemoveAt(currentIndex);
                    _exercises.Insert(currentIndex - 1, exercise);

                    // Update the UI to reflect the change
                    CustomExerciseLayout.Children.RemoveAt(currentIndex);
                    CustomExerciseLayout.Children.Insert(currentIndex - 1, exerciseFrame);
                }
                else if (action == "Move Exercise Down")
                {
                    // Swap the current exercise with the one below it
                    _exercises.RemoveAt(currentIndex);
                    _exercises.Insert(currentIndex + 1, exercise);

                    // Update the UI to reflect the change
                    CustomExerciseLayout.Children.RemoveAt(currentIndex);
                    CustomExerciseLayout.Children.Insert(currentIndex + 1, exerciseFrame);
                }
            };

            muscleGroupGrid.Children.Add(menuButton);
            Grid.SetColumn(menuButton, 1);

            exerciseLayout.Children.Add(muscleGroupGrid);

            var exerciseNameLabel = new Label
            {
                Text = exercise.Name,
                FontAttributes = FontAttributes.Bold,
                FontSize = 18,
                TextColor = Color.FromArgb("#ffffff")
            };

            exerciseLayout.Children.Add(exerciseNameLabel);

            // Add the exercise layout to the frame
            exerciseFrame.Content = exerciseLayout;

            return exerciseFrame;
        }

        private void AddSetToExercise(StackLayout exerciseLayout)
        {
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Auto }, // Icon Button
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, // Weight Entry
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, // Reps Entry
                },
                Margin = new Thickness(0, 5, 0, 5)
            };

            // Create the action button for set options
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
                    Placeholder = "Weight",
                    Keyboard = Keyboard.Numeric,
                    TextColor = Colors.White,
                    BackgroundColor = Color.FromArgb("#2A2A2A"),
                    WidthRequest = 100,
                    HeightRequest = 35,
                    Margin = new Thickness(5, 5, 0, 0),
                    IsEnabled = false,
                }
            };

            // Entry for reps with frame
            var repsEntryFrame = new Frame
            {
                Padding = new Thickness(1),
                BackgroundColor = Color.FromArgb("#2A2A2A"),
                WidthRequest = 75,
                HeightRequest = 35,
                BorderColor = Color.FromRgb(140, 140, 140),
                Content = new Entry
                {
                    Placeholder = "Reps",
                    Keyboard = Keyboard.Numeric,
                    TextColor = Colors.White,
                    BackgroundColor = Color.FromArgb("#2A2A2A"),
                    WidthRequest = 75,
                    HeightRequest = 35,
                    Margin = new Thickness(5, 5, 0, 0),
                    IsEnabled = false,
                }
            };

            // Add the Button (icon), Weight, and Reps Entry to the grid
            grid.Children.Add(actionButton);
            Grid.SetColumn(actionButton, 0);

            grid.Children.Add(weightEntryFrame);
            Grid.SetColumn(weightEntryFrame, 1);

            grid.Children.Add(repsEntryFrame);
            Grid.SetColumn(repsEntryFrame, 2);

            // Add the new grid to the exercise layout
            exerciseLayout.Children.Add(grid);
        }

        private void AddSetBelow(Grid gridAbove, StackLayout exerciseLayout)
        {
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Auto }, // Icon Button
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, // Weight Entry
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) } // Reps Entry
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

            // Entry for weight with frame
            var weightEntryFrame = new Frame
            {
                Padding = new Thickness(1),
                BackgroundColor = Color.FromArgb("#2A2A2A"),
                WidthRequest = 100,
                HeightRequest = 35,
                BorderColor = Color.FromRgb(140, 140, 140),
                Content = new Entry
                {
                    Placeholder = "Weight",
                    Keyboard = Keyboard.Numeric,
                    TextColor = Colors.White,
                    BackgroundColor = Color.FromArgb("#2A2A2A"),
                    WidthRequest = 100,
                    HeightRequest = 35,
                    Margin = new Thickness(5, 5, 0, 0)
                }
            };

            // Entry for reps with frame
            var repsEntryFrame = new Frame
            {
                Padding = new Thickness(1),
                BackgroundColor = Color.FromArgb("#2A2A2A"),
                WidthRequest = 75,
                HeightRequest = 35,
                BorderColor = Color.FromRgb(140, 140, 140),
                Content = new Entry
                {
                    Placeholder = "Reps",
                    Keyboard = Keyboard.Numeric,
                    TextColor = Colors.White,
                    BackgroundColor = Color.FromArgb("#2A2A2A"),
                    WidthRequest = 75,
                    HeightRequest = 35,
                    Margin = new Thickness(5, 5, 0, 0)
                }
            };

            // Add the Button (icon), Weight, and Reps Entry to the grid
            grid.Children.Add(actionButton);
            Grid.SetColumn(actionButton, 0);

            grid.Children.Add(weightEntryFrame);
            Grid.SetColumn(weightEntryFrame, 1);

            grid.Children.Add(repsEntryFrame);
            Grid.SetColumn(repsEntryFrame, 2);

            // Add the grid below the existing grid
            int index = exerciseLayout.Children.IndexOf(gridAbove);
            exerciseLayout.Children.Insert(index + 1, grid);
        }

        private async void OnCloseButtonTapped(object sender, EventArgs e)
        {
            var closeButton = sender as Button;

            // Perform the scale down animation
            await closeButton.ScaleTo(0.8, 100, Easing.CubicIn);

            // Perform the scale back to original size
            await closeButton.ScaleTo(1.0, 100, Easing.CubicOut);

            bool confirm = await DisplayAlert("Confirm", "Are you sure you want to cancel? This action cannot be undone.", "Yes", "No");
            if (confirm)
            {
                await Navigation.PopAsync(); // Navigate back to the previous page
            }
        }

        private async void OnSaveButtonTapped(object sender, EventArgs e)
        {
            // Prompt the user to name the workout
            string workoutTitle;

            if (_workoutSession != null)
            {
                // Editing mode: Pre-fill the dialog with the existing workout title
                workoutTitle = await DisplayPromptAsync("Save Workout", "Enter the name for your workout:", initialValue: _workoutSession.Title);
            }
            else
            {
                // Creation mode: Prompt for a new workout name
                workoutTitle = await DisplayPromptAsync("Save Workout", "Enter the name for your workout:", "OK", "Cancel", "Workout Name");
            }

            if (string.IsNullOrWhiteSpace(workoutTitle))
            {
                await DisplayAlert("Error", "Workout name cannot be empty.", "OK");
                return; // Exit if the user cancels or enters an empty name
            }

            // Determine if we're editing an existing workout or creating a new one
            string workoutId;
            if (_workoutSession != null)
            {
                // Editing mode: Keep the original ID
                workoutId = _workoutSession.Id;
                _workoutSession.Title = workoutTitle;
                _workoutSession.Date = DateTime.Today.ToString("yyyy-MM-dd");

                // Update each exercise rather than clearing them out
                foreach (var child in CustomExerciseLayout.Children)
                {
                    if (child is Frame exerciseFrame && exerciseFrame.Content is StackLayout exerciseLayout)
                    {
                        var exerciseNameLabel = exerciseLayout.Children.OfType<Label>().FirstOrDefault();
                        var muscleGroupLabel = exerciseLayout.Children.OfType<Grid>().FirstOrDefault()?.Children.OfType<Frame>().FirstOrDefault()?.Content as Label;

                        if (exerciseNameLabel != null && muscleGroupLabel != null)
                        {
                            var existingExercise = _workoutSession.Exercises.FirstOrDefault(ex => ex.Name == exerciseNameLabel.Text);

                            if (existingExercise == null)
                            {
                                existingExercise = new Exercise
                                {
                                    Name = exerciseNameLabel.Text,
                                    MuscleGroup = muscleGroupLabel.Text,
                                    Sets = new List<Set>()
                                };
                                _workoutSession.Exercises.Add(existingExercise);
                            }
                            else
                            {
                                existingExercise.MuscleGroup = muscleGroupLabel.Text;
                            }

                            var existingSets = existingExercise.Sets.ToList();
                            existingExercise.Sets.Clear();

                            int setIndex = 0;
                            foreach (var setGrid in exerciseLayout.Children.OfType<Grid>())
                            {
                                if (setGrid.Children.Count > 2)
                                {
                                    var weightFrame = setGrid.Children[1] as Frame;
                                    var repsFrame = setGrid.Children[2] as Frame;

                                    int weight = 0;
                                    int reps = 0;

                                    if (weightFrame?.Content is Entry weightEntry && !string.IsNullOrEmpty(weightEntry.Text))
                                    {
                                        int.TryParse(weightEntry.Text, out weight);
                                    }

                                    if (repsFrame?.Content is Entry repsEntry && !string.IsNullOrEmpty(repsEntry.Text))
                                    {
                                        int.TryParse(repsEntry.Text, out reps);
                                    }

                                    var set = new Set
                                    {
                                        SetNumber = setIndex + 1,
                                        Weight = weight,
                                        Reps = reps,
                                        PreviousWeight = existingSets.Count > setIndex ? existingSets[setIndex].PreviousWeight : 0,
                                        PreviousReps = existingSets.Count > setIndex ? existingSets[setIndex].PreviousReps : 0,
                                        Completed = false
                                    };
                                    existingExercise.Sets.Add(set);
                                    setIndex++;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // Creation mode: Generate a new ID
                workoutId = $"CustomTemplate_{workoutTitle.Replace(" ", "_").ToLower()}";
                _workoutSession = new WorkoutSession
                {
                    Id = workoutId,
                    Title = workoutTitle,
                    Date = DateTime.Today.ToString("yyyy-MM-dd"),
                    LastPerformed = string.Empty,
                    Exercises = new List<Exercise>(),
                    CompletedExercises = new List<CompletedExercise>()
                };

                // Collect the exercises from the CustomExerciseLayout
                foreach (var child in CustomExerciseLayout.Children)
                {
                    if (child is Frame exerciseFrame && exerciseFrame.Content is StackLayout exerciseLayout)
                    {
                        var exerciseNameLabel = exerciseLayout.Children.OfType<Label>().FirstOrDefault();
                        var muscleGroupLabel = exerciseLayout.Children.OfType<Grid>().FirstOrDefault()?.Children.OfType<Frame>().FirstOrDefault()?.Content as Label;

                        if (exerciseNameLabel != null && muscleGroupLabel != null)
                        {
                            var exercise = new Exercise
                            {
                                Name = exerciseNameLabel.Text,
                                MuscleGroup = muscleGroupLabel.Text,
                                Sets = new List<Set>()
                            };

                            foreach (var setGrid in exerciseLayout.Children.OfType<Grid>())
                            {
                                if (setGrid.Children.Count > 2)
                                {
                                    var weightFrame = setGrid.Children[1] as Frame;
                                    var repsFrame = setGrid.Children[2] as Frame;

                                    int weight = 0;
                                    int reps = 0;

                                    if (weightFrame?.Content is Entry weightEntry && !string.IsNullOrEmpty(weightEntry.Text))
                                    {
                                        int.TryParse(weightEntry.Text, out weight);
                                    }

                                    if (repsFrame?.Content is Entry repsEntry && !string.IsNullOrEmpty(repsEntry.Text))
                                    {
                                        int.TryParse(repsEntry.Text, out reps);
                                    }

                                    var set = new Set
                                    {
                                        SetNumber = exercise.Sets.Count + 1,
                                        Weight = weight,
                                        Reps = reps,
                                        PreviousWeight = 0,
                                        PreviousReps = 0,
                                        Completed = false
                                    };
                                    exercise.Sets.Add(set);
                                }
                            }

                            _workoutSession.Exercises.Add(exercise);
                        }
                    }
                }
            }

            // Serialize the workout to JSON
            string json = JsonConvert.SerializeObject(_workoutSession, Formatting.Indented);

            // Save the JSON file
#if ANDROID
    var path = Android.App.Application.Context.GetExternalFilesDir(Android.OS.Environment.DirectoryDocuments).AbsolutePath;

    if (path == null)
    {
        await DisplayAlert("Error", "Unable to save workout. Path is null.", "OK");
        return;
    }

    string filePath = Path.Combine(path, $"{workoutId}.json");

    if (filePath == null)
    {
        await DisplayAlert("Error", "Unable to save workout. File path is null.", "OK");
        return;
    }

    File.WriteAllText(filePath, json);

    await DisplayAlert("Success", "Workout saved successfully!", "OK");

#else
            await DisplayAlert("Error", "Saving workouts is not supported on this platform.", "OK");
#endif

            // Navigate back to the previous page or refresh the current view
            await Navigation.PopAsync();
        }





    }
}
