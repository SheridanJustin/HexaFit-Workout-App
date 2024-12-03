using Android.Graphics;
using Android.Widget;
using WorkoutApplication.Effects;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Compatibility;

[assembly: ResolutionGroupName("WorkoutApplication")]
[assembly: ExportEffect(typeof(TintEffect), "TintEffect")]
namespace WorkoutApplication.Droid.Effects
{
    public class TintEffect : PlatformEffect
    {
        protected override void OnAttached()
        {
            if (Control is ImageView imageView && Element is Image imageElement)
            {
                var tintColor = ((TintEffect)Element.Effects.FirstOrDefault(e => e is TintEffect)).TintColor;
                imageView.SetColorFilter(tintColor.ToAndroid(), PorterDuff.Mode.SrcIn);
            }
        }

        protected override void OnDetached()
        {
        }
    }
}
