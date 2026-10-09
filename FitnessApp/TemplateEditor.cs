using System.Globalization;
using FitnessApp.Models;

namespace FitnessApp
{
    // Shared exercise/set editor for the Create and Edit template screens.
    // Each card keeps its own set rows, so adding an exercise never rebuilds (and wipes) the others.
    public class TemplateEditor
    {
        private class ExerciseCard
        {
            public Frame Frame;
            public Label NameLabel;
            public string MuscleGroup;
            public string ImagePath;
            public StackLayout SetsLayout;
            public List<SetRow> Rows = new List<SetRow>();
        }

        private class SetRow
        {
            public Grid Grid;
            public Label NumberLabel;
            public Entry WeightEntry;
            public Entry RepsEntry;
        }

        private readonly ContentPage _page;
        private readonly StackLayout _layout;
        private readonly List<ExerciseCard> _cards = new List<ExerciseCard>();

        public TemplateEditor(ContentPage page, StackLayout layout)
        {
            _page = page;
            _layout = layout;
        }

        public int ExerciseCount => _cards.Count;

        // Adds a card for the exercise. Exercises picked from the list get two empty sets.
        public void AddExercise(Exercise exercise, bool withDefaultSets)
        {
            var card = new ExerciseCard
            {
                Frame = new Frame
                {
                    BorderColor = Theme.Border,
                    BackgroundColor = Theme.CardBackground,
                    CornerRadius = 10,
                    Padding = new Thickness(10, 0, 10, 10),
                    Margin = new Thickness(0, 5, 0, 10),
                    HasShadow = true
                },
                MuscleGroup = exercise.MuscleGroup ?? string.Empty,
                ImagePath = exercise.ImagePath ?? string.Empty,
                SetsLayout = new StackLayout { Spacing = 0 }
            };

            var exerciseLayout = new StackLayout
            {
                Spacing = 5,
                Margin = new Thickness(0, 5, 0, 7)
            };

            var headerGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

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
                IsVisible = !string.IsNullOrWhiteSpace(card.MuscleGroup),
                Content = new Label
                {
                    Text = card.MuscleGroup.ToUpper(),
                    TextColor = Theme.TextPrimary,
                    FontAttributes = FontAttributes.Bold
                }
            };
            headerGrid.Children.Add(muscleGroupFrame);
            Grid.SetColumn(muscleGroupFrame, 0);

            var menuButton = new Button
            {
                ImageSource = "menu.png",
                BackgroundColor = Colors.Transparent,
                WidthRequest = 48,
                HeightRequest = 48,
                VerticalOptions = LayoutOptions.Center
            };
            menuButton.Clicked += async (s, e) => await ShowExerciseMenu(card);
            headerGrid.Children.Add(menuButton);
            Grid.SetColumn(menuButton, 1);

            exerciseLayout.Children.Add(headerGrid);

            card.NameLabel = new Label
            {
                Text = exercise.Name,
                FontAttributes = FontAttributes.Bold,
                FontSize = 18,
                TextColor = Theme.TextPrimary
            };
            exerciseLayout.Children.Add(card.NameLabel);

            var columnHeaders = CreateSetGrid();
            columnHeaders.Margin = new Thickness(0, 5, 0, 0);
            AddHeaderLabel(columnHeaders, "Set", 1);
            AddHeaderLabel(columnHeaders, "Weight", 2);
            AddHeaderLabel(columnHeaders, "Reps", 3);
            exerciseLayout.Children.Add(columnHeaders);
            exerciseLayout.Children.Add(card.SetsLayout);

            card.Frame.Content = exerciseLayout;

            if (withDefaultSets || exercise.Sets == null || exercise.Sets.Count == 0)
            {
                AddSetRow(card, null, 0);
                AddSetRow(card, null, 1);
            }
            else
            {
                foreach (var set in exercise.Sets)
                {
                    AddSetRow(card, set, card.Rows.Count);
                }
            }

            _cards.Add(card);
            _layout.Children.Add(card.Frame);
        }

        private async Task ShowExerciseMenu(ExerciseCard card)
        {
            int index = _cards.IndexOf(card);
            var options = new List<string> { "Add Set", "Rename Exercise" };
            if (index > 0)
            {
                options.Add("Move Exercise Up");
            }
            if (index < _cards.Count - 1)
            {
                options.Add("Move Exercise Down");
            }
            options.Add("Delete Exercise");

            string action = await _page.DisplayActionSheet("Options", "Cancel", null, options.ToArray());

            switch (action)
            {
                case "Add Set":
                    AddSetRow(card, null, card.Rows.Count);
                    break;

                case "Rename Exercise":
                    string newName = await _page.DisplayPromptAsync("Rename Exercise", "Enter the new name for the exercise:", "OK", "Cancel", "Exercise Name", initialValue: card.NameLabel.Text);
                    if (!string.IsNullOrWhiteSpace(newName))
                    {
                        card.NameLabel.Text = newName.Trim();
                    }
                    break;

                case "Move Exercise Up":
                    MoveCard(card, index - 1);
                    break;

                case "Move Exercise Down":
                    MoveCard(card, index + 1);
                    break;

                case "Delete Exercise":
                    _cards.Remove(card);
                    _layout.Children.Remove(card.Frame);
                    break;
            }
        }

        private void MoveCard(ExerciseCard card, int newIndex)
        {
            _cards.Remove(card);
            _cards.Insert(newIndex, card);
            _layout.Children.Remove(card.Frame);
            _layout.Children.Insert(newIndex, card.Frame);
        }

        private static Grid CreateSetGrid()
        {
            return new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(40) },  // Set menu
                    new ColumnDefinition { Width = new GridLength(32) },  // Set number
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, // Weight
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }  // Reps
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

        private static Entry CreateEntry(string placeholder, string text)
        {
            return new Entry
            {
                Placeholder = placeholder,
                PlaceholderColor = Theme.TextMuted,
                Text = text,
                Keyboard = Keyboard.Numeric,
                TextColor = Theme.TextPrimary,
                BackgroundColor = Theme.InputBackground,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalOptions = LayoutOptions.Center
            };
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

        // The weight/reps boxes hold the set's "previous" values, which the workout screen shows as hints
        private void AddSetRow(ExerciseCard card, Set set, int index)
        {
            var row = new SetRow { Grid = CreateSetGrid() };
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
                string action = await _page.DisplayActionSheet("Options", "Cancel", null, "Add Set Below", "Remove Set");

                if (action == "Remove Set")
                {
                    card.Rows.Remove(row);
                    card.SetsLayout.Children.Remove(row.Grid);
                    RenumberSets(card);
                }
                else if (action == "Add Set Below")
                {
                    AddSetRow(card, null, card.Rows.IndexOf(row) + 1);
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

            double weight = set?.PreviousWeight ?? 0;
            int reps = set?.PreviousReps ?? 0;
            row.WeightEntry = CreateEntry("lbs", weight > 0 ? weight.ToString(CultureInfo.InvariantCulture) : null);
            row.RepsEntry = CreateEntry("reps", reps > 0 ? reps.ToString(CultureInfo.InvariantCulture) : null);

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

            index = Math.Clamp(index, 0, card.Rows.Count);
            card.Rows.Insert(index, row);
            card.SetsLayout.Children.Insert(index, row.Grid);
            RenumberSets(card);
        }

        private static void RenumberSets(ExerciseCard card)
        {
            for (int i = 0; i < card.Rows.Count; i++)
            {
                card.Rows[i].NumberLabel.Text = (i + 1).ToString();
            }
        }

        // Reads the cards back into exercises, in on-screen order. Empty boxes save as 0.
        public List<Exercise> BuildExercises(out string invalidMessage)
        {
            invalidMessage = null;
            var exercises = new List<Exercise>();

            foreach (var card in _cards)
            {
                var exercise = new Exercise
                {
                    Name = card.NameLabel.Text,
                    MuscleGroup = card.MuscleGroup,
                    ImagePath = card.ImagePath,
                    Sets = new List<Set>()
                };

                for (int i = 0; i < card.Rows.Count; i++)
                {
                    var row = card.Rows[i];
                    double weight = 0;
                    int reps = 0;

                    string weightText = row.WeightEntry.Text?.Trim();
                    if (!string.IsNullOrEmpty(weightText)
                        && (!double.TryParse(weightText.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out weight) || weight < 0))
                    {
                        invalidMessage ??= $"Set {i + 1} of {exercise.Name} has a weight that isn't a number.";
                    }

                    string repsText = row.RepsEntry.Text?.Trim();
                    if (!string.IsNullOrEmpty(repsText)
                        && !int.TryParse(repsText, NumberStyles.None, CultureInfo.InvariantCulture, out reps))
                    {
                        invalidMessage ??= $"Set {i + 1} of {exercise.Name} needs a whole number of reps.";
                    }

                    exercise.Sets.Add(new Set
                    {
                        SetNumber = i + 1,
                        PreviousWeight = weight,
                        PreviousReps = reps
                    });
                }

                exercises.Add(exercise);
            }

            return exercises;
        }
    }
}
