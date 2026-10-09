using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using FitnessApp.Services;

namespace FitnessApp.ViewModels
{
    // Drives the Scan page: take or pick a photo, send it to the scanner server,
    // then show what machine it is, how to use it, and a link to a how-to video.
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly FitnessApiService _apiService;

        private ImageSource _previewImage;
        public ImageSource PreviewImage
        {
            get => _previewImage;
            set { _previewImage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasImage)); OnPropertyChanged(nameof(IsIdle)); }
        }

        public bool HasImage => PreviewImage != null;

        private string _prediction;
        public string Prediction
        {
            get => _prediction;
            set { _prediction = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasResult)); }
        }

        private string _instructions;
        public string Instructions
        {
            get => _instructions;
            set { _instructions = value; OnPropertyChanged(); }
        }

        public bool HasResult => !string.IsNullOrWhiteSpace(Prediction);

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotLoading)); }
        }

        public bool IsNotLoading => !IsLoading;

        // Nothing picked yet: show the "take a photo" prompt
        public bool IsIdle => PreviewImage == null;

        public ICommand PickImageCommand { get; }
        public ICommand TakePhotoCommand { get; }
        public ICommand WatchVideoCommand { get; }

        public MainViewModel()
        {
            _apiService = new FitnessApiService();

            PickImageCommand = new Command(async () => await PickImageAsync());
            TakePhotoCommand = new Command(async () => await TakePhotoAsync());
            WatchVideoCommand = new Command(async () => await WatchVideoAsync());
        }

        private async Task PickImageAsync()
        {
            if (IsLoading)
            {
                return;
            }

            try
            {
                var photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions { Title = "Select a gym machine photo" });
                await AnalyzeAsync(photo);
            }
            catch (Exception ex)
            {
                ShowError("Couldn't open your photos.", ex);
            }
        }

        private async Task TakePhotoAsync()
        {
            if (IsLoading)
            {
                return;
            }

            if (!MediaPicker.Default.IsCaptureSupported)
            {
                ErrorMessage = "This device doesn't have a camera you can use. Try choosing a photo instead.";
                return;
            }

            try
            {
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                await AnalyzeAsync(photo);
            }
            catch (PermissionException ex)
            {
                ShowError("HexaFit needs camera permission to take a photo. You can allow it in your phone's Settings.", ex);
            }
            catch (Exception ex)
            {
                ShowError("Couldn't open the camera.", ex);
            }
        }

        private async Task AnalyzeAsync(FileResult photo)
        {
            if (photo == null)
            {
                return; // Cancelled
            }

            PreviewImage = ImageSource.FromFile(photo.FullPath);
            Prediction = null;
            Instructions = null;
            ErrorMessage = null;
            IsLoading = true;

            try
            {
                using var stream = await photo.OpenReadAsync();
                var response = await _apiService.GetImagePredictionAsync(stream, photo.FileName);

                if (string.IsNullOrWhiteSpace(response?.Prediction))
                {
                    ErrorMessage = "We couldn't recognise that machine. Try a clearer photo with the whole machine in view.";
                    return;
                }

                Prediction = response.Prediction.Trim();
                Instructions = string.IsNullOrWhiteSpace(response.Instructions)
                    ? "No instructions available yet. Watch the video below to see it in action."
                    : response.Instructions.Trim();
            }
            catch (HttpRequestException ex)
            {
                ShowError("Couldn't reach the HexaFit scanner server. Make sure it's running and try again.", ex);
            }
            catch (TaskCanceledException ex)
            {
                ShowError("The scanner server took too long to answer. Try again.", ex);
            }
            catch (Exception ex)
            {
                ShowError("Something went wrong while scanning. Try again.", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Opens a YouTube search for a how-to video on the identified machine
        private async Task WatchVideoAsync()
        {
            if (!HasResult)
            {
                return;
            }

            var query = Uri.EscapeDataString($"how to use {Prediction} gym machine");
            await Launcher.Default.OpenAsync(new Uri($"https://www.youtube.com/results?search_query={query}"));
        }

        private void ShowError(string message, Exception ex)
        {
            Console.WriteLine($"Scan error: {ex}");
            Prediction = null;
            Instructions = null;
            ErrorMessage = message;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
