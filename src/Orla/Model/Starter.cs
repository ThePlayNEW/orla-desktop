using System;
using System.IO;
using System.Linq;

namespace Orla
{
    // Starting without the organizer, from the presets people pick on the welcome screen.
    public static class Starter
    {
        // Keeps the Windows icons as they are; the panels come from the presets people pick.
        public static Layout alongside(Layout settings)
        {
            Layout layout = copySettings(settings);
            layout.CleanDesktop = false;
            return layout;
        }

        // A new layout with no panels and the same settings.
        public static Layout copySettings(Layout s)
        {
            return new Layout {
                StartupConfigured = s.StartupConfigured, StartupEnabled = s.StartupEnabled, OverlayHotkey = s.OverlayHotkey,
                OverlayShortcut = s.OverlayShortcut, Welcomed = s.Welcomed, Animations = s.Animations, Opacity = s.Opacity, Theme = s.Theme, Language = s.Language, IconSize = s.IconSize,
                LockLayout = s.LockLayout, AutoUpdate = s.AutoUpdate, LastUpdateCheck = s.LastUpdateCheck
            };
        }
    }
}
