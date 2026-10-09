using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Maui.Controls;
using FitnessApp.Models;

namespace FitnessApp
{
    public partial class HistoryPage : ContentPage
    {
        // How many workouts to read and draw at a time
        private const int PageSize = 10;

        // Start loading the next page when the user is this close to the bottom
        private const double LoadMoreThreshold = 400;

        private List<string> _files = new List<string>();
        private int _nextIndex;
        private bool _isLoading;
        private int _generation; // Bumped on every reload so a stale page load is dropped

        public HistoryPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Refresh the workout history every time the page appears
            LoadWorkoutHistory();
        }

        private void DisplayNoWorkoutMessage()
        {
            HistoryContent.Children.Clear();

            var messageLabel = new Label
            {
                Text = "No workouts saved.",
                TextColor = Theme.TextPrimary,
                HorizontalOptions = LayoutOptions.Center,
                FontSize = 18,
                Margin = new Thickness(0, 20, 0, 10)
            };

            HistoryContent.Children.Add(messageLabel);
            UpdateLoadMoreButton();
        }

        private async void LoadWorkoutHistory()
        {
            _generation++;
            _isLoading = false;
            HistoryContent.Children.Clear();
            _nextIndex = 0;

            string directoryPath = FileSystem.AppDataDirectory;

            // Only list the files here; their contents are read a page at a time.
            // File names start with History_yyyy-MM-dd_HH-mm-ss, so sorting by name puts the newest first.
            _files = Directory.Exists(directoryPath)
                ? Directory.GetFiles(directoryPath, "History_*.json")
                    .OrderByDescending(f => Path.GetFileName(f), StringComparer.Ordinal)
                    .ToList()
                : new List<string>();

            if (_files.Count == 0)
            {
                DisplayNoWorkoutMessage();
                return;
            }

            await LoadNextPageAsync();
        }

        private async Task LoadNextPageAsync()
        {
            if (_isLoading || _nextIndex >= _files.Count)
            {
                return;
            }

            _isLoading = true;
            int generation = _generation;
            LoadingMoreIndicator.IsVisible = true;
            LoadingMoreIndicator.IsRunning = true;
            LoadMoreButton.IsVisible = false;

            var pageFiles = _files.Skip(_nextIndex).Take(PageSize).ToList();

            // Read and parse off the UI thread so scrolling stays smooth
            var sessions = await Task.Run(() =>
            {
                var result = new List<(string File, WorkoutSession Session)>();
                foreach (var file in pageFiles)
                {
                    try
                    {
                        var session = JsonConvert.DeserializeObject<WorkoutSession>(File.ReadAllText(file));
                        if (session != null)
                        {
                            result.Add((file, session));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading file {file}: {ex.Message}");
                    }
                }
                return result;
            });

            if (generation != _generation)
            {
                return; // The page was reloaded while this one was reading
            }

            foreach (var (file, session) in sessions)
            {
                HistoryContent.Children.Add(CreateHistoryCard(file, session));
            }

            _nextIndex += pageFiles.Count;
            _isLoading = false;
            LoadingMoreIndicator.IsRunning = false;
            LoadingMoreIndicator.IsVisible = false;
            UpdateLoadMoreButton();
        }

        private void UpdateLoadMoreButton()
        {
            LoadMoreButton.IsVisible = !_isLoading && _nextIndex < _files.Count;
        }

        private async void OnHistoryScrolled(object sender, ScrolledEventArgs e)
        {
            double distanceToBottom = HistoryScroll.ContentSize.Height - (e.ScrollY + HistoryScroll.Height);
            if (distanceToBottom < LoadMoreThreshold)
            {
                await LoadNextPageAsync();
            }
        }

        private async void OnLoadMoreClicked(object sender, EventArgs e)
        {
            await LoadNextPageAsync();
        }

        private View CreateHistoryCard(string filePath, WorkoutSession session)
        {
            var container = new StackLayout { Spacing = 0 };

            // Add the workout date and title at the top
            var workoutTitleGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            var workoutTitleLabel = new Label
            {
                Text = $"{session.Title} - {session.Date}",
                FontAttributes = FontAttributes.Bold,
                FontSize = 20,
                TextColor = Theme.TextPrimary,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(2, 0, 0, 0)
            };

            var menuButton = new Button
            {
                ImageSource = "menu.png",
                BackgroundColor = Colors.Transparent,
                WidthRequest = 48,
                HeightRequest = 48,
                VerticalOptions = LayoutOptions.Center
            };

            menuButton.Clicked += async (s, e) =>
            {
                string action = await DisplayActionSheet("Options", "Cancel", null, "Delete Workout");

                if (action == "Delete Workout")
                {
                    bool confirm = await DisplayAlert("Delete Confirmation", "Are you sure you want to delete this workout history?", "Yes", "No");
                    if (confirm)
                    {
                        DeleteWorkoutHistory(filePath, container);
                    }
                }
            };

            workoutTitleGrid.Children.Add(workoutTitleLabel);
            Grid.SetColumn(workoutTitleLabel, 0);
            workoutTitleGrid.Children.Add(menuButton);
            Grid.SetColumn(menuButton, 1);

            container.Children.Add(workoutTitleGrid);

            // Create a frame to hold all exercises of the workout
            var workoutFrame = new Frame
            {
                BorderColor = Theme.Border,
                BackgroundColor = Theme.CardBackground,
                CornerRadius = 10,
                Padding = new Thickness(10),
                HasShadow = true
            };

            var workoutLayout = new StackLayout { Spacing = 5 };

            foreach (var exercise in session.CompletedExercises ?? new List<CompletedExercise>())
            {
                var exerciseTag = new Frame
                {
                    BorderColor = Theme.TagBorder,
                    BackgroundColor = Theme.TagBackground,
                    CornerRadius = 5,
                    Padding = new Thickness(5),
                    HasShadow = false,
                    Margin = new Thickness(0, 5, 0, 7),
                    HorizontalOptions = LayoutOptions.Start,
                    Content = new Label
                    {
                        Text = (exercise.Name ?? string.Empty).ToUpper(),
                        TextColor = Theme.TextPrimary,
                        FontAttributes = FontAttributes.Bold
                    }
                };

                workoutLayout.Children.Add(exerciseTag);

                int setNumber = 1;
                foreach (var set in exercise.Sets ?? new List<CompletedSet>())
                {
                    workoutLayout.Children.Add(new Label
                    {
                        Text = $"Set {setNumber++}:  {set.Weight} lbs × {set.Reps} reps",
                        FontSize = 14,
                        TextColor = Theme.TextPrimary,
                        Margin = new Thickness(2, 2, 0, 2)
                    });
                }
            }

            workoutFrame.Content = workoutLayout;
            container.Children.Add(workoutFrame);

            return container;
        }

        private void DeleteWorkoutHistory(string filePath, View card)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            // Remove just this card so the list keeps its scroll position
            HistoryContent.Children.Remove(card);

            int index = _files.IndexOf(filePath);
            if (index >= 0)
            {
                _files.RemoveAt(index);
                if (index < _nextIndex)
                {
                    _nextIndex--;
                }
            }

            if (_files.Count == 0)
            {
                DisplayNoWorkoutMessage();
            }
        }
    }
}
