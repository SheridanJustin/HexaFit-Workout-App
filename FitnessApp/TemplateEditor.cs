using FitnessApp.Models;

namespace FitnessApp
{
    // Shared exercise/set editor for the Create and Edit template screens.
    // Each card keeps its own set rows, so adding an exercise never rebuilds (and wipes) the others.
    // Weights and reps aren't edited here; each set keeps the "previous" values its last workout saved.
    public class TemplateEditor
    {
        private class ExerciseCard
        {
            public Frame Frame;
            public Label NameLabel;
            public string MuscleGroup;
            public string ImagePath;
            public View Body; // Hidden while dragging so every card fits on screen
            public StackLayout SetsLayout;
            public List<SetRow> Rows = new List<SetRow>();
        }

        private class SetRow
        {
            public Grid Grid;
            public Label NumberLabel;
            public Set TemplateSet; // The saved set this row came from; null for new sets
        }

        private readonly ContentPage _page;
        private readonly StackLayout _layout;
        private readonly List<ExerciseCard> _cards = new List<ExerciseCard>();
        private readonly CardReorder _reorder;

        public TemplateEditor(ContentPage page, StackLayout layout)
        {
            _page = page;
            _layout = layout;
            _reorder = new CardReorder(layout, SetCompact, SyncOrderFromLayout);
        }

        public int ExerciseCount => _cards.Count;

        // Adds a card for the exercise. Exercises picked from the list get two sets.
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
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            var handle = CardReorder.CreateHandle();
            headerGrid.Children.Add(handle);
            Grid.SetColumn(handle, 0);

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
            Grid.SetColumn(muscleGroupFrame, 1);

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
            Grid.SetColumn(menuButton, 2);

            exerciseLayout.Children.Add(headerGrid);

            card.NameLabel = new Label
            {
                Text = exercise.Name,
                FontAttributes = FontAttributes.Bold,
                FontSize = 18,
                TextColor = Theme.TextPrimary
            };
            exerciseLayout.Children.Add(card.NameLabel);

            card.Body = card.SetsLayout;
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
            _reorder.Attach(card.Frame, handle, () => card.NameLabel.Text);
        }

        private void SetCompact(bool compact)
        {
            foreach (var card in _cards)
            {
                card.Body.IsVisible = !compact;
            }
        }

        // Keeps _cards in the same order as the cards on screen after a drag
        private void SyncOrderFromLayout()
        {
            var ordered = _layout.Children
                .Select(child => _cards.FirstOrDefault(c => c.Frame == child))
                .Where(c => c != null)
                .ToList();
            _cards.Clear();
            _cards.AddRange(ordered);
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

        private void AddSetRow(ExerciseCard card, Set set, int index)
        {
            var row = new SetRow
            {
                TemplateSet = set,
                Grid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(40) },  // Set menu
                        new ColumnDefinition { Width = GridLength.Auto },     // "Set N"
                        new ColumnDefinition { Width = GridLength.Star }      // Last logged
                    },
                    ColumnSpacing = 8,
                    Margin = new Thickness(0, 2)
                }
            };

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
                VerticalOptions = LayoutOptions.Center
            };

            // Read-only reminder of what was logged last time
            double weight = set?.PreviousWeight ?? 0;
            int reps = set?.PreviousReps ?? 0;
            var lastLabel = new Label
            {
                Text = weight > 0 || reps > 0 ? $"Last: {weight} lbs × {reps} reps" : string.Empty,
                FontSize = 14,
                TextColor = Theme.TextMuted,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center
            };

            row.Grid.Children.Add(actionButton);
            Grid.SetColumn(actionButton, 0);
            row.Grid.Children.Add(row.NumberLabel);
            Grid.SetColumn(row.NumberLabel, 1);
            row.Grid.Children.Add(lastLabel);
            Grid.SetColumn(lastLabel, 2);

            index = Math.Clamp(index, 0, card.Rows.Count);
            card.Rows.Insert(index, row);
            card.SetsLayout.Children.Insert(index, row.Grid);
            RenumberSets(card);
        }

        private static void RenumberSets(ExerciseCard card)
        {
            for (int i = 0; i < card.Rows.Count; i++)
            {
                card.Rows[i].NumberLabel.Text = $"Set {i + 1}";
            }
        }

        // Reads the cards back into exercises, in on-screen order. Sets keep their saved
        // "previous" values; new sets start empty.
        public List<Exercise> BuildExercises()
        {
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
                    var saved = card.Rows[i].TemplateSet;
                    exercise.Sets.Add(new Set
                    {
                        SetNumber = i + 1,
                        Weight = saved?.Weight ?? 0,
                        Reps = saved?.Reps ?? 0,
                        PreviousWeight = saved?.PreviousWeight ?? 0,
                        PreviousReps = saved?.PreviousReps ?? 0
                    });
                }

                exercises.Add(exercise);
            }

            return exercises;
        }
    }
}
