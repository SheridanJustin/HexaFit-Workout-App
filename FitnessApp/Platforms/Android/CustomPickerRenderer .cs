using Android.Content;
using Android.Graphics.Drawables;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Platform;
using WorkoutApplication;
using Application = Android.App.Application;

[assembly: ExportRenderer(typeof(Picker), typeof(CustomPickerRenderer))]
namespace WorkoutApplication.Platforms.Android
{
    public class CustomPickerRenderer : PickerRenderer
    {
        public CustomPickerRenderer(Context context) : base(context)
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Picker> e)
        {
            base.OnElementChanged(e);

            if (Control != null)
            {
                // Change the text color of the dropdown
                Control.SetTextColor(Android.Graphics.Color.ParseColor("#32D74B")); // Accent Green

                // Change the background color of the dropdown
                var gradientDrawable = new GradientDrawable();
                gradientDrawable.SetColor(Android.Graphics.Color.ParseColor("#121212")); // Dark Background
                Control.SetBackground(gradientDrawable);

                // Change the dropdown items background color and text color
                Control.SetBackgroundColor(Android.Graphics.Color.ParseColor("#121212")); // Dark Background
                Control.SetPopupBackgroundDrawable(new ColorDrawable(Android.Graphics.Color.ParseColor("#121212"))); // Dropdown background color
                Control.SetTextColor(Android.Graphics.Color.ParseColor("#32D74B")); // Accent Green
            }
        }
    }
}
