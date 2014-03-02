/* Program.cs - (c) James S Renwick 2014
 * -------------------------------------
 * Version 1.0.0
 * 
 * Quick and dirty CLI for reporting and
 * changing the current user's wallpaper.
 */
using System;
using System.Linq;
using Win32.Desktop;

namespace Wallpaper
{
    class Program
    {
        /// <summary>
        /// Writes an error message to stderr.
        /// </summary>
        /// <param name="message">The message to print.</param>
        static void errorOut(string message)
        {
            Console.Error.WriteLine("[ERROR] " + message);
            Environment.Exit(-1);
        }

        static void printUsage()
        {
            Console.WriteLine(@"
Windows Wallpaper Utility
=========================
(c) James S Renwick 2014

usage:
    wallpaper                         Prints details of the current wallpaper.
    wallpaper /?                      Prints this help message.
    wallpaper <filepath> [<options>]  Sets the wallpaper to a copy of the image
                                      at the path <filepath>.

options:
    /L             Does not create a copy, just links to the image.
    /P <position>  Specifies the wallpaper position;
                   one of {0}. Defaults to Fill.

", Enum.GetNames(typeof(PicturePosition)).Aggregate((s, t) => s + ", " + t));
        }


        static void Main(string[] args)
        {
            // Output current wallpaper details
            if (args.Length == 0)
            {
                Console.WriteLine("Current Wallpaper ");
                Console.WriteLine("\t Filepath: \"{0}\"", WallpaperInterop.GetWallpaperFilepath());
                Console.WriteLine("\t Position: {0}",     WallpaperInterop.GetWallpaperPosition());
            }
            // Output usage info
            else if (args.Contains("/?"))
            {
                Program.printUsage();
            }
            // Perform wallpaper change
            else
            {
                bool copy     = true;
                var  position = PicturePosition.Fill;

                // Process options
                for (int i = 1; i < args.Length; i++)
                {
                    if (args[i] == "/L") { // Don't copy
                        copy = false;
                    }
                    else if (args[i] == "/P") // Set PicturePosition
                    {
                        if (args.Length == i + 1) {
                            errorOut("Expected parameter for option '/P'");
                        }
                        else
                        {
                            if (!Enum.TryParse(args[i + 1], out position)) {
                                errorOut("Invalid parameter for option '/P'");
                            }
                        }
                        i++; // Skip parameter
                    }
                    else errorOut("Invalid parameter: " + args[i]);
                }
                try
                {
                    // Call interop code
                    WallpaperInterop.SetWallpaper(args[0], position, copy);
                }
                catch (Exception e)
                {
                    errorOut(e.Message);
                }
            }
        }
    }
}