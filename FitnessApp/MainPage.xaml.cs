using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Microsoft.Maui.Controls;
using Newtonsoft.Json;
using FitnessApp.Models;

namespace FitnessApp
{
    [QueryProperty(nameof(SelectedExercise), "SelectedExercise")]
    [QueryProperty(nameof(SelectedWorkout), "SelectedWorkout")]

    public partial class MainPage : ContentPage
    {
        // One exercise card on screen. TemplateName is the name the exercise had in the
        // template when the workout started, so Finish can match it back to the template exercise
        // even after a rename. It is null for exercises added during the workout.
        private class ExerciseCard
        {
            public Frame Frame;
            public Label NameLabel;
            public StackLayout SetsLayout;
            public string TemplateName;
            public string MuscleGroup;
            public string ImagePath;
            public List<SetRow> Sets = new List<SetRow>();
        }

        private class SetRow
        {
            public Grid Grid;
            public Label NumberLabel;
            public Entry WeightEntry;
            public Entry RepsEntry;
            public Button CheckButton;
            public bool IsCompleted;
            public Set TemplateSet; // The template set this row came from; null for sets added mid-workout
        }

        private int workoutCount = 0; // Store the workout count
        private Label _timerLabel;
        private WorkoutSession _currentWorkoutSession;
        private readonly Dictionary<Frame, ExerciseCard> _cards = new Dictionary<Frame, ExerciseCard>();
        private Frame _insertAfterFrame; // Where "Add Exercise Below" should put the picked exercise
        private Button _finishButton;
        private bool _isSaving;

        private System.Timers.Timer _timer;
        private TimeSpan _elapsedTime;
        private readonly object _lock = new object(); // Prevents thread race conditions

        public Exercise SelectedExercise
        {
            get => null; // No need to return anything
            set
            {
                if (value == null || _currentWorkoutSession == null)
                {
                    return;
                }

                // Copy the picked exercise so the exercise list's own object is never changed
                var exercise = new Exercise
                {
                    Name = value.Name,
                    ImagePath = value.ImagePath,
                    MuscleGroup = value.MuscleGroup,
                    Sets = new List<Set>
                    {
                        new Set { SetNumber = 1 },
                        new Set { SetNumber = 2 }
                    }
                };

                // Insert the new card without rebuilding the page, so typed sets and the timer survive
                var frame = CreateExerciseFrame(exercise, templateName: null);
                int index = _insertAfterFrame != null ? PlanContent.Children.IndexOf(_insertAfterFrame) : -1;
                if (index >= 0)
                {
                    PlanContent.Children.Insert(index + 1, frame);
                }
                else
                {
                    int finishIndex = _finishButton != null ? PlanContent.Children.IndexOf(_finishButton) : -1;
                    if (finishIndex >= 0)
                    {
                        PlanContent.Children.Insert(finishIndex, frame);
                    }
                    else
                    {
                        PlanContent.Children.Add(frame);
                    }
                }

                _insertAfterFrame = null;
            }
        }

        private WorkoutSession _selectedWorkout;

        public WorkoutSession SelectedWorkout
        {
            get => _selectedWorkout;
            set
            {
                if (value == null || ReferenceEquals(value, _selectedWorkout))
                {
                    return;
                }

                _selectedWorkout = value;
                DisplayWorkoutPlan(_selectedWorkout);
            }
        }

        public MainPage()
        {
            InitializeComponent();

            LoadWorkoutCount();
            DisplayNoWorkoutMessage();
        }

        private void StartTimer()
        {
            StopTimer();

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
                var text = _elapsedTime.ToString(@"hh\:mm\:ss");

                // Update the timer label on the UI thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (_timerLabel != null)
                    {
                        _timerLabel.Text = text;
                    }
                });
            }
        }

        private void StopTimer()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Elapsed -= OnTimedEvent;
                _timer.Dispose();
                _timer = null;
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
                TextColor = Theme.TextPrimary,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Start,
                FontSize = 16,
                Margin = new Thickness(0, -20, 0, 30)
            };

            var selectButton = new Button
            {
                Text = "Go to Templates",
                BackgroundColor = Theme.Accent,
                TextColor = Theme.TextPrimary,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 10,
                HeightRequest = 50,
                Padding = new Thickness(24, 0),
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Start,
            };

            selectButton.Clicked += async (s, e) =>
            {
                await selectButton.ScaleTo(0.9, 100, Easing.CubicIn);
                await selectButton.ScaleTo(1, 100, Easing.CubicOut);

                await Shell.Current.GoToAsync("//Templates");
            };

            PlanContent.Children.Add(image);
            PlanContent.Children.Add(messageLabel);
            PlanContent.Children.Add(selectButton);
        }

        public void DisplayWorkoutPlan(WorkoutSession session)
        {
            if (session == null)
            {
                DisplayNoWorkoutMessage();
                return;
            }

            _currentWorkoutSession = session;
            _cards.Clear();
            _insertAfterFrame = null;
            PlanContent.Children.Clear();

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

            var headerFrame = new Frame
            {
                BorderColor = Theme.Border,
                BackgroundColor = Theme.CardBackground,
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
                TextColor = Theme.TextSecondary,
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
                TextColor = Theme.TextPrimary,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                BackgroundColor = Theme.Danger,
                TextColor = Theme.TextPrimary,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 10,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.End,
                Padding = new Thickness(16, 10)
            };

            cancelButton.Clicked += OnCancelButtonClicked;

            titleGrid.Children.Add(titleLabel);
            Grid.SetRow(titleLabel, 0);
            Grid.SetColumn(titleLabel, 0);

            titleGrid.Children.Add(cancelButton);
            Grid.SetRow(cancelButton, 0);
            Grid.SetColumn(cancelButton, 1);
            Grid.SetRowSpan(cancelButton, 2);

            titleGrid.Children.Add(_timerLabel);
            Grid.SetRow(_timerLabel, 1);
            Grid.SetColumn(_timerLabel, 0);

            headerFrame.Content = titleGrid;
            PlanContent.Children.Add(headerFrame);

            foreach (var exercise in session.Exercises ?? new List<Exercise>())
            {
                PlanContent.Children.Add(CreateExerciseFrame(exercise, exercise.Name));
            }

            _finishButton = new Button
            {
                Text = "Finish Workout",
                BackgroundColor = Theme.Accent,
                TextColor = Theme.TextPrimary,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 10,
                HeightRequest = 50,
                Margin = new Thickness(0, 20, 0, 20),
                HorizontalOptions = LayoutOptions.Fill
            };

            _finishButton.Clicked += OnFinishButtonClicked;
            PlanContent.Children.Add(_finishButton);

            StartTimer();
        }

        // Builds one exercise card: muscle group tag, menu, name, column headers and set rows
        private Frame CreateExerciseFrame(Exercise exercise, string templateName)
        {
            var exerciseFrame = new Frame
            {
                BorderColor = Theme.Border,
                BackgroundColor = Theme.CardBackground,
                CornerRadius = 10,
                Padding = new Thickness(10, 0, 10, 10),
                Margin = new Thickness(0, 5, 0, 10),
                HasShadow = true
            };

            var exerciseLayout = new StackLayout
            {
                Spacing = 5,
                Margin = new Thickness(0, 5, 0, 7)
            };

            var card = new ExerciseCard
            {
                Frame = exerciseFrame,
                TemplateName = templateName,
                MuscleGroup = exercise.MuscleGroup ?? string.Empty,
                ImagePath = exercise.ImagePath ?? string.Empty,
                SetsLayout = new StackLayout { Spacing = 0 }
            };

            var muscleGroupGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            var muscleGroup = exercise.MuscleGroup ?? string.Empty;
            var muscleGroupFrame = new Frame
            {
                BorderColor = Theme.TagBorder,
                BackgroundColor = Theme.TagBackground,
                CornerRadius = 5,
                Padding = new Thickness(5),
                HasShadow = false,
                Margin = new Thickness(0, 5, 0, 7),
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Center,
                IsVisible = !string.IsNullOrWhiteSpace(muscleGroup),
                Content = new Label
                {
                    Text = muscleGroup.ToUpper(),
                    TextColor = Theme.TextPrimary,
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center
                }
            };

            muscleGroupGrid.Children.Add(muscleGroupFrame);
            Grid.SetColumn(muscleGroupFrame, 0);

            var menuButton = new Button
            {
                ImageSource = "menu.png",
                BackgroundColor = Colors.Transparent,
                WidthRequest = 48,
                HeightRequest = 48,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.End
            };

            menuButton.Clicked += async (s, e) =>
            {
                string action = await DisplayActionSheet("Options", "Cancel", null, "Add Set", "Rename Exercise", "Add Exercise Below", "Delete Exercise");

                if (action == "Delete Exercise")
                {
                    bool confirm = await DisplayAlert("Delete Exercise", $"Remove {card.NameLabel.Text} from this workout?", "Delete", "Keep");
                    if (confirm)
                    {
                        PlanContent.Children.Remove(exerciseFrame);
                        _cards.Remove(exerciseFrame);
                    }
                }
                else if (action == "Rename Exercise")
                {
                    string newName = await DisplayPromptAsync("Rename Exercise", "Enter the new name for the exercise:", "OK", "Cancel", "Exercise Name", initialValue: card.NameLabel.Text);

                    if (!string.IsNullOrWhiteSpace(newName))
                    {
                        card.NameLabel.Text = newName.Trim();
                    }
                }
                else if (action == "Add Set")
                {
                    AddSetRow(card, null, card.Sets.Count);
                }
                else if (action == "Add Exercise Below")
                {
                    _insertAfterFrame = exerciseFrame;
                    await Navigation.PushAsync(new SelectExercisePage());
                }
            };

            muscleGroupGrid.Children.Add(menuButton);
            Grid.SetColumn(menuButton, 1);

            exerciseLayout.Children.Add(muscleGroupGrid);

            card.NameLabel = new Label
            {
                Text = exercise.Name,
                FontAttributes = FontAttributes.Bold,
                FontSize = 18,
                TextColor = Theme.TextPrimary
            };
            exerciseLayout.Children.Add(card.NameLabel);

            var headerGrid = CreateSetGrid();
            headerGrid.Margin = new Thickness(0, 5, 0, 0);
            AddHeaderLabel(headerGrid, "Set", 1);
            AddHeaderLabel(headerGrid, "Weight", 2);
            AddHeaderLabel(headerGrid, "Reps", 3);
            exerciseLayout.Children.Add(headerGrid);

            exerciseLayout.Children.Add(card.SetsLayout);

            var sets = exercise.Sets ?? new List<Set>();
            if (sets.Count == 0)
            {
                sets = new List<Set> { new Set { SetNumber = 1 } };
            }

            foreach (var set in sets)
            {
                AddSetRow(card, set, card.Sets.Count);
            }

            exerciseFrame.Content = exerciseLayout;
            _cards[exerciseFrame] = card;
            return exerciseFrame;
        }

        // Every set row and the column header share these columns so they line up
        private static Grid CreateSetGrid()
        {
            return new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(40) },  // Set menu
                    new ColumnDefinition { Width = new GridLength(32) },  // Set number
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, // Weight
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, // Reps
                    new ColumnDefinition { Width = new GridLength(48) }   // Checkmark
                },
                ColumnSpacing = 8
            };
        }

        private static void AddHeaderLabel(Grid grid, string text, int column)
        {
            var label = new Label
            {
                Text = text,
                FontAttributes = FontAttributes.Bold,
                FontSize = 14,
                TextColor = Theme.TextSecondary,
                HorizontalOptions = LayoutOptions.Center
            };
            grid.Children.Add(label);
            Grid.SetColumn(label, column);
        }

        private static Frame WrapEntry(Entry entry)
        {
            return new Frame
            {
                Padding = new Thickness(6, 0),
                BackgroundColor = Theme.InputBackground,
                HeightRequest = 40,
                CornerRadius = 6,
                BorderColor = Theme.Border,
                HasShadow = false,
                Content = entry
            };
        }

        // Adds a set row at the given position in the card. previous holds the last-session values, if any.
        private void AddSetRow(ExerciseCard card, Set previous, int index)
        {
            var row = new SetRow { Grid = CreateSetGrid(), TemplateSet = previous };
            row.Grid.Margin = new Thickness(0, 4);

            var actionButton = new Button
            {
                ImageSource = "menu.png",
                BackgroundColor = Colors.Transparent,
                WidthRequest = 40,
                HeightRequest = 40,
                Padding = new Thickness(5),
                VerticalOptions = LayoutOptions.Center
            };

            actionButton.Clicked += async (s, e) =>
            {
                string action = await DisplayActionSheet("Options", "Cancel", null, "Add Set Below", "Remove Set");

                if (action == "Remove Set")
                {
                    card.SetsLayout.Children.Remove(row.Grid);
                    card.Sets.Remove(row);
                    RenumberSets(card);
                }
                else if (action == "Add Set Below")
                {
                    AddSetRow(card, null, card.Sets.IndexOf(row) + 1);
                }
            };

            row.NumberLabel = new Label
            {
                FontAttributes = FontAttributes.Bold,
                FontSize = 16,
                TextColor = Theme.TextSecondary,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            double previousWeight = previous?.PreviousWeight ?? 0;
            int previousReps = previous?.PreviousReps ?? 0;

            row.WeightEntry = new Entry
            {
                Placeholder = previousWeight > 0 ? $"{previousWeight} lbs" : "lbs",
                PlaceholderColor = Theme.TextMuted,
                Keyboard = Keyboard.Numeric,
                TextColor = Theme.TextPrimary,
                BackgroundColor = Theme.InputBackground,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalOptions = LayoutOptions.Center
            };

            row.RepsEntry = new Entry
            {
                Placeholder = previousReps > 0 ? $"{previousReps} reps" : "reps",
                PlaceholderColor = Theme.TextMuted,
                Keyboard = Keyboard.Numeric,
                TextColor = Theme.TextPrimary,
                BackgroundColor = Theme.InputBackground,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalOptions = LayoutOptions.Center
            };

            row.CheckButton = new Button
            {
                Text = "✔",
                FontSize = 16,
                WidthRequest = 40,
                HeightRequest = 40,
                CornerRadius = 6,
                BorderWidth = 2,
                Padding = new Thickness(0),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            row.WeightEntry.TextChanged += (s, e) => UpdateCheckButton(row);
            row.RepsEntry.TextChanged += (s, e) => UpdateCheckButton(row);

            row.CheckButton.Clicked += async (s, e) =>
            {
                if (!HasBothValues(row))
                {
                    return;
                }

                await row.CheckButton.ScaleTo(0.9, 100, Easing.CubicIn);
                await row.CheckButton.ScaleTo(1, 100, Easing.CubicOut);

                // Tapping again un-marks the set
                row.IsCompleted = !row.IsCompleted;
                UpdateCheckButton(row);
            };

            UpdateCheckButton(row);

            row.Grid.Children.Add(actionButton);
            Grid.SetColumn(actionButton, 0);

            row.Grid.Children.Add(row.NumberLabel);
            Grid.SetColumn(row.NumberLabel, 1);

            var weightFrame = WrapEntry(row.WeightEntry);
            row.Grid.Children.Add(weightFrame);
            Grid.SetColumn(weightFrame, 2);

            var repsFrame = WrapEntry(row.RepsEntry);
            row.Grid.Children.Add(repsFrame);
            Grid.SetColumn(repsFrame, 3);

            row.Grid.Children.Add(row.CheckButton);
            Grid.SetColumn(row.CheckButton, 4);

            index = Math.Clamp(index, 0, card.Sets.Count);
            card.Sets.Insert(index, row);
            card.SetsLayout.Children.Insert(index, row.Grid);
            RenumberSets(card);
        }

        private static void RenumberSets(ExerciseCard card)
        {
            for (int i = 0; i < card.Sets.Count; i++)
            {
                card.Sets[i].NumberLabel.Text = (i + 1).ToString();
            }
        }

        private static bool HasBothValues(SetRow row)
        {
            return !string.IsNullOrWhiteSpace(row.WeightEntry.Text) && !string.IsNullOrWhiteSpace(row.RepsEntry.Text);
        }

        // Idle: dim outline. Ready (both values typed): bright outline. Done: filled green.
        private static void UpdateCheckButton(SetRow row)
        {
            bool ready = HasBothValues(row);
            if (!ready)
            {
                row.IsCompleted = false;
            }

            if (row.IsCompleted)
            {
                row.CheckButton.BackgroundColor = Theme.Accent;
                row.CheckButton.BorderColor = Theme.Accent;
                row.CheckButton.TextColor = Theme.TextPrimary;
            }
            else if (ready)
            {
                row.CheckButton.BackgroundColor = Colors.Transparent;
                row.CheckButton.BorderColor = Theme.CheckReady;
                row.CheckButton.TextColor = Theme.TextPrimary;
            }
            else
            {
                row.CheckButton.BackgroundColor = Colors.Transparent;
                row.CheckButton.BorderColor = Theme.CheckIdle;
                row.CheckButton.TextColor = Theme.CheckIdle;
            }
        }

        // Accepts "12.5" and "12,5"
        private static bool TryParseWeight(string text, out double weight)
        {
            return double.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out weight)
                && weight >= 0 && !double.IsInfinity(weight);
        }

        private static bool TryParseReps(string text, out int reps)
        {
            return int.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out reps);
        }

        // Reads the sets currently on screen. Never changes the workout session, so it is safe
        // to call from Cancel and then Finish without logging anything twice.
        private List<CompletedExercise> CollectWorkoutData(out bool hasEmptySets, out string invalidMessage)
        {
            hasEmptySets = false;
            invalidMessage = null;
            var logged = new List<CompletedExercise>();

            foreach (var child in PlanContent.Children)
            {
                if (child is not Frame frame || !_cards.TryGetValue(frame, out var card))
                {
                    continue;
                }

                string exerciseName = card.NameLabel.Text;
                var completedExercise = new CompletedExercise
                {
                    Name = exerciseName,
                    Sets = new List<CompletedSet>()
                };

                for (int i = 0; i < card.Sets.Count; i++)
                {
                    var row = card.Sets[i];
                    if (!HasBothValues(row))
                    {
                        hasEmptySets = true;
                        continue;
                    }

                    if (!TryParseWeight(row.WeightEntry.Text, out double weight))
                    {
                        invalidMessage ??= $"Set {i + 1} of {exerciseName} has a weight that isn't a number.";
                        continue;
                    }

                    if (!TryParseReps(row.RepsEntry.Text, out int reps))
                    {
                        invalidMessage ??= $"Set {i + 1} of {exerciseName} needs a whole number of reps.";
                        continue;
                    }

                    completedExercise.Sets.Add(new CompletedSet
                    {
                        Weight = weight,
                        Reps = reps,
                        Completed = true
                    });
                }

                if (completedExercise.Sets.Count > 0)
                {
                    logged.Add(completedExercise);
                }
            }

            return logged;
        }

        private async void OnFinishButtonClicked(object sender, EventArgs e)
        {
            var session = _currentWorkoutSession;
            if (_isSaving || session == null)
            {
                return;
            }

            _isSaving = true;
            _finishButton.IsEnabled = false;

            try
            {
                var logged = CollectWorkoutData(out bool hasEmptySets, out string invalidMessage);

                if (invalidMessage != null)
                {
                    await DisplayAlert("Check your sets", invalidMessage, "OK");
                    return;
                }

                if (logged.Count == 0)
                {
                    await DisplayAlert("Warning", "Please input at least one set!", "Ok");
                    return;
                }

                if (hasEmptySets)
                {
                    bool proceed = await DisplayAlert("Warning", "Some exercises have empty sets and will not be saved. Do you want to continue?", "Yes", "No");
                    if (!proceed)
                    {
                        return;
                    }
                }

#if ANDROID
                var path = FileSystem.AppDataDirectory;

                var historySession = new WorkoutSession
                {
                    Id = session.Id,
                    Title = session.Title,
                    Date = DateTime.Today.ToString("yyyy-MM-dd"),
                    LastPerformed = session.LastPerformed,
                    Exercises = session.Exercises,
                    CompletedExercises = logged
                };

                string historyFileName = $"History_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_Workout.json";
                string json = JsonConvert.SerializeObject(historySession, Formatting.Indented);
                File.WriteAllText(Path.Combine(path, historyFileName), json);

                // The workout is saved. Updating the template must never undo that.
                try
                {
                    SaveChangesToTemplate(session.Id, path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not update template {session.Id}: {ex}");
                }

                LoadWorkoutCount(); // Settings can reset the counter while this page is alive
                workoutCount++;
                SaveWorkoutCount();

                EndWorkout();

                await DisplayAlert("Workout Finished", $"You have completed {workoutCount} workouts. View previous workouts under the history tab.", "OK");
#else
                await DisplayAlert("Not Supported", "Workout saving is only supported on Android for now.", "OK");
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex}");
                await DisplayAlert("Error", "An unexpected error occurred.", "OK");
            }
            finally
            {
                _isSaving = false;
                if (_finishButton != null)
                {
                    _finishButton.IsEnabled = true;
                }
            }
        }

        // Writes the workout as it ended back into its template: exercise order, names, added or
        // removed exercises and sets, plus this session's weights and reps as the new "previous" values.
        private void SaveChangesToTemplate(string workoutId, string directory)
        {
            if (string.IsNullOrEmpty(workoutId))
            {
                return;
            }

            string templatePath = Path.Combine(directory, $"{workoutId}.json");

            if (!File.Exists(templatePath))
            {
                Console.WriteLine($"Error: Original workout file with Id {workoutId} not found.");
                return;
            }

            var template = JsonConvert.DeserializeObject<WorkoutSession>(File.ReadAllText(templatePath));
            if (template == null)
            {
                return;
            }

            var remaining = new List<Exercise>(template.Exercises ?? new List<Exercise>());
            var updatedExercises = new List<Exercise>();

            foreach (var child in PlanContent.Children)
            {
                if (child is not Frame frame || !_cards.TryGetValue(frame, out var card))
                {
                    continue;
                }

                // Find the template exercise this card started from (none for exercises added mid-workout)
                Exercise original = null;
                if (card.TemplateName != null)
                {
                    original = remaining.FirstOrDefault(e => e.Name == card.TemplateName);
                    remaining.Remove(original);
                }

                var exercise = new Exercise
                {
                    Name = card.NameLabel.Text,
                    MuscleGroup = card.MuscleGroup,
                    ImagePath = string.IsNullOrEmpty(card.ImagePath) ? original?.ImagePath ?? string.Empty : card.ImagePath,
                    Sets = new List<Set>()
                };

                for (int i = 0; i < card.Sets.Count; i++)
                {
                    var row = card.Sets[i];
                    var originalSet = row.TemplateSet;

                    var set = new Set
                    {
                        SetNumber = i + 1,
                        Weight = originalSet?.Weight ?? 0,
                        Reps = originalSet?.Reps ?? 0,
                        PreviousWeight = originalSet?.PreviousWeight ?? 0,
                        PreviousReps = originalSet?.PreviousReps ?? 0
                    };

                    // Sets you filled in become the new hints; blank sets keep the old ones
                    if (HasBothValues(row)
                        && TryParseWeight(row.WeightEntry.Text, out double weight)
                        && TryParseReps(row.RepsEntry.Text, out int reps))
                    {
                        set.PreviousWeight = weight;
                        set.PreviousReps = reps;
                    }

                    exercise.Sets.Add(set);
                }

                updatedExercises.Add(exercise);
            }

            // A template with no exercises disappears from the Templates page, so never save one
            if (updatedExercises.Count > 0)
            {
                template.Exercises = updatedExercises;
            }

            template.LastPerformed = DateTime.Now.ToString("yyyy-MM-dd");

            File.WriteAllText(templatePath, JsonConvert.SerializeObject(template, Formatting.Indented));
        }

        private async void OnCancelButtonClicked(object sender, EventArgs e)
        {
            try
            {
                var logged = CollectWorkoutData(out _, out string invalidMessage);
                if (logged.Count > 0 || invalidMessage != null)
                {
                    bool proceed = await DisplayAlert("Warning", "Some sets have data that will be lost. Do you want to continue?", "Yes", "No");
                    if (!proceed)
                    {
                        return;
                    }
                }

                EndWorkout();

                await DisplayAlert("Workout Canceled", "Your workout has been canceled.", "OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex}");
                await DisplayAlert("Error", "An unexpected error occurred.", "OK");
            }
        }

        private void EndWorkout()
        {
            StopTimer();
            _cards.Clear();
            _currentWorkoutSession = null;
            _selectedWorkout = null;
            _insertAfterFrame = null;
            _finishButton = null;
            _timerLabel = null;
            App.IsWorkoutActive = false;
            DisplayNoWorkoutMessage();
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
