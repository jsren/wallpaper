/* Slideshow.cs - (c) James S Renwick 2014
 * ---------------------------------------
 * Version 1.1.0
 */
using System;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

using Win32.Desktop;
using Microsoft.Win32.TaskScheduler;

namespace Wallpaper
{
    /// <summary>
    /// Class providing methods for managing slideshows implemented
    /// with Windows' sheduled task system.
    /// </summary>
    public static class SlideshowHelper
    {
        // List of valid background image extensions
        private static readonly string[] imgExts = new string[] {
            ".jpg", ".jpeg", ".png", ".bmp", ".tiff"
        };

        // Gets the path to the companion wallpaperw executable.
        private static string GetWallpaperwExe()
        {
            string path = Path.GetFullPath(Environment.GetCommandLineArgs()[0]);
            return Path.Combine(Path.GetDirectoryName(path), "wallpaperw.exe");
        }

        /// <summary>
        /// Gets a list of valid background images contained within the
        /// given directory.
        /// </summary>
        /// <param name="path">The path in which to search for backgrounds.</param>
        /// <returns>A list of valid background images.</returns>
        public static string[] GetBackgroundsInFolder(string path)
        {
            // Filter files in directory by images
            return Directory.EnumerateFiles(path).Where((f) =>
            {
                foreach (string ext in imgExts) {
                    if (Path.GetExtension(f) == ext) return true;
                }
                return false;
            }).ToArray();
        }

        /// <summary>
        /// Cancels the current background slideshow.
        /// </summary>
        /// <returns>True when an active slideshow was cancelled.</returns>
        public static bool CancelSlideshow()
        {
            using (TaskService service = new TaskService())
            {
                // First look for any other slideshow entries
                Task[] tasks = service.FindAllTasks(new Regex("wwu_slideshow"));

                // Remove any previous
                foreach (Task t in tasks)
                {
                    t.Stop();
                    service.RootFolder.DeleteTask(t.Name, false);
                }
                return tasks.Length != 0;
            }
        }

        public static void NextBackground()
        {
            int    index;
            int    interval;
            string path;

            // Read registry values
            using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
            {
                index    = (int)key.GetValue("WWU CurrentIndex", -1);
                interval = (int)key.GetValue("WWU CurrentInterval", 0);
                path     = (string)key.GetValue("WWU CurrentDir", null);
            }

            if (path == null || interval == 0) 
                throw new Exception("No slideshow currently in progress");

            // Get the current images in the given directory
            string[] images = GetBackgroundsInFolder(path);
            // Wrap index
            if (++index >= images.Length) index = 0;

            // Set blank if no images in folder
            if (images.Length == 0) {
                WallpaperInterop.SetWallpaper("", PicturePosition.Center, false);
            }
            // Otherwise set background to the next image
            else
            {
                var pos = WallpaperInterop.GetWallpaperPosition();
                WallpaperInterop.SetWallpaper(images[index], pos, false);
            }
            // Save registry values
            using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
            {
                key.SetValue("WWU CurrentIndex",    index);
                key.SetValue("WWU CurrentInterval", interval);
                key.SetValue("WWU CurrentDir",      path);
            }
        }

        /// <summary>
        /// Begins a new slideshow, cancelling any active ones and setting the initial
        /// background.
        /// </summary>
        /// <param name="path">The path to the background directory.</param>
        /// <param name="position">The position with which the background will be displayed.</param>
        /// <param name="interval">The interval before which the next background will be set.</param>
        /// <param name="index">The file index within the directory at which to begin.</param>
        public static void BeginSlideshow(string path, PicturePosition position, 
            int interval, int index)
        {
            if (interval < 0) throw new ArgumentOutOfRangeException("showInterval");
            if (index < 0)    throw new ArgumentOutOfRangeException("showIndex");

            // Get the absolute path to the directory
            path = Path.GetFullPath(path);

            // Get the current images in the given directory
            string[] images = GetBackgroundsInFolder(path);

            // Wrap index
            if (index >= images.Length) index = 0;

            // Clamp interval
            interval = Math.Min(interval, 44640);

            // Save registry values
            using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
            {
                key.SetValue("WWU CurrentIndex",    index);
                key.SetValue("WWU CurrentInterval", interval);
                key.SetValue("WWU CurrentDir",      path);
            }

            // Remove any current slideshows
            CancelSlideshow();

            // Schedule slideshow
            using (TaskService service = new TaskService())
            {
                // Create new task
                TaskDefinition task = service.NewTask();
                
                // Create time trigger
                var trigger = new TimeTrigger();
                trigger.SetRepetition(TimeSpan.FromMinutes(interval), TimeSpan.Zero);
                
                task.Triggers.Add(trigger);
                task.Settings.StartWhenAvailable = true;
                task.Settings.WakeToRun          = false;

                // Add re-execution to set next background
                task.Actions.Add(new ExecAction(GetWallpaperwExe() + " /next"));

                // Register task
                service.RootFolder.RegisterTaskDefinition("wwu_slideshow", task);
            }

            // Now actually set the first wallpaper

            // Set blank if no images in folder
            if (images.Length == 0) {
                WallpaperInterop.SetWallpaper("", PicturePosition.Center, false);
            }
            // Otherwise set background to the next image
            else {
                WallpaperInterop.SetWallpaper(images[index], position, false);
            }
        }
    }
}
