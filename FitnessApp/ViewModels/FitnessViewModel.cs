using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using FitnessApp.Services;

namespace FitnessApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly FitnessApiService _apiService;

        private string _inputText;
        public string InputText
        {
            get => _inputText;
            set { _inputText = value; OnPropertyChanged(); }
        }

        private string _prediction;
        public string Prediction
        {
            get => _prediction;
            set { _prediction = value; OnPropertyChanged(); }
        }

        private string _instructions;
        public string Instructions
        {
            get => _instructions;
            set { _instructions = value; OnPropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private string _placeholderText;
        public string PlaceholderText
        {
            get => _placeholderText;
            set { _placeholderText = value; OnPropertyChanged(); }
        }

        private bool _isPlaceholderVisible = true;
        public bool IsPlaceholderVisible
        {
            get => _isPlaceholderVisible;
            set { _isPlaceholderVisible = value; OnPropertyChanged(); }
        }

        public ICommand PredictCommand { get; }
        public ICommand PickImageCommand { get; }
        public ICommand TakePhotoCommand { get; }

        public MainViewModel()
        {
            _apiService = new FitnessApiService();
            PlaceholderText = "Take or select a picture to identify gym equipment";

            PredictCommand = new Command(async () => await PredictAsync());
            PickImageCommand = new Command(async () => await PickImageAsync());
            TakePhotoCommand = new Command(async () => await TakePhotoAsync());
        }

        private async Task PredictAsync()
        {
            if (string.IsNullOrWhiteSpace(InputText))
            {
                Prediction = "Please enter some text.";
                Instructions = string.Empty;
                return;
            }

            IsLoading = true;
            Prediction = "Loading...";
            Instructions = string.Empty;

            try
            {
                var result = await _apiService.GetPredictionAsync(InputText);
                Prediction = result; // Adjust based on actual API response
                Instructions = string.Empty;
            }
            catch (HttpRequestException ex)
            {
                Prediction = "Error: Could not connect to the server.";
                Instructions = $"Details: {ex.Message}";
                Console.WriteLine($"HTTP Error in PredictAsync: {ex.Message}, Inner: {ex.InnerException?.Message}");
            }
            catch (Exception ex)
            {
                Prediction = "Error: Prediction failed.";
                Instructions = $"Details: {ex.Message}";
                Console.WriteLine($"General Error in PredictAsync: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task PickImageAsync()
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select an image"
                });

                if (result != null)
                {
                    IsLoading = true;
                    Prediction = "Uploading image...";
                    Instructions = string.Empty;

                    var response = await _apiService.GetImagePredictionAsync(result.FullPath);
                    if (!string.IsNullOrEmpty(response.Prediction))
                    {
                        Prediction = $"Equipment: {response.Prediction}";
                        Instructions = string.IsNullOrEmpty(response.Instructions) ? "No instructions available." : $"Instructions: {response.Instructions}";
                        IsPlaceholderVisible = false; // Hide placeholder on successful result
                    }
                    else
                    {
                        Prediction = "Unknown";
                        Instructions = "No instructions available.";
                        IsPlaceholderVisible = true; // Show placeholder if no valid result
                    }
                }
                else
                {
                    IsPlaceholderVisible = true; // Show placeholder if no image selected
                }
            }
            catch (HttpRequestException ex)
            {
                Prediction = "Error: Could not connect to the server.";
                Instructions = $"Details: {ex.Message}";
                IsPlaceholderVisible = true; // Show placeholder on error
                Console.WriteLine($"HTTP Error in PickImageAsync: {ex.Message}, Inner: {ex.InnerException?.Message}");
            }
            catch (Exception ex)
            {
                Prediction = "Error: Prediction failed.";
                Instructions = $"Details: {ex.Message}";
                IsPlaceholderVisible = true; // Show placeholder on error
                Console.WriteLine($"General Error in PickImageAsync: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task TakePhotoAsync()
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                Prediction = "Camera not supported.";
                Instructions = string.Empty;
                IsPlaceholderVisible = true; // Show placeholder if camera not supported
                return;
            }

            try
            {
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo != null)
                {
                    IsLoading = true;
                    Prediction = "Uploading photo...";
                    Instructions = string.Empty;

                    using var stream = await photo.OpenReadAsync();
                    var response = await _apiService.GetImagePredictionAsync(stream, photo.FileName);
                    if (!string.IsNullOrEmpty(response.Prediction))
                    {
                        Prediction = $"Equipment: {response.Prediction}";
                        Instructions = string.IsNullOrEmpty(response.Instructions) ? "No instructions available." : $"Instructions: {response.Instructions}";
                        IsPlaceholderVisible = false; // Hide placeholder on successful result
                    }
                    else
                    {
                        Prediction = "Unknown";
                        Instructions = "No instructions available.";
                        IsPlaceholderVisible = true; // Show placeholder if no valid result
                    }
                }
                else
                {
                    IsPlaceholderVisible = true; // Show placeholder if no photo taken
                }
            }
            catch (HttpRequestException ex)
            {
                Prediction = "Error: Could not connect to the server.";
                Instructions = $"Details: {ex.Message}";
                IsPlaceholderVisible = true; // Show placeholder on error
                Console.WriteLine($"HTTP Error in TakePhotoAsync: {ex.Message}, Inner: {ex.InnerException?.Message}");
            }
            catch (Exception ex)
            {
                Prediction = "Error: Prediction failed.";
                Instructions = $"Details: {ex.Message}";
                IsPlaceholderVisible = true; // Show placeholder on error
                Console.WriteLine($"General Error in TakePhotoAsync: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}