using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace Orla
{
    // The only motion in Orla: short, eased fades when something appears. Nothing runs continuously.
    public static class Motion
    {
        public static void reveal(UIElement element)
        {
            var fade = new DoubleAnimation(0.6, 1, TimeSpan.FromMilliseconds(160)) {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.Stop
            };
            element.BeginAnimation(UIElement.OpacityProperty, fade);
        }
    }
}
