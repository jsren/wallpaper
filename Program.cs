/* Program.cs - (c) James S Renwick 2014
 * -------------------------------------
 * Version 1.3.0
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
        internal static void errorOut(string message)
        {
            Console.Error.WriteLine("[ERROR] " + message);
            Environment.Exit(-1);
        }

        static void printUsage()
        {
            Console.WriteLine(@"
Windows Wallpaper Utility
=========================
(c) James S Renwick 2015

usage:
    wallpaper                         Prints details of the current wallpaper.
    wallpaper /?                      Prints this help message.
    wallpaper /endshow                Ends the current slideshow.
    wallpaper /next                   Advances the current slideshow.
	wallpaper /login <path>           Sets the login background to a copy of the
                                      image at the path <path>.
    wallpaper <path> [<options>]      Sets the wallpaper to a copy of the JPEG image
                                      at the path <path>.

options:
    /L             Does not create a copy, just links to the image.
    /P <position>  Specifies the wallpaper position;
                   one of {0}. Defaults to Fill.
    /D             Indicates that the path is a directory. If /S is not
                   given, a random image from the folder will be selected.
    /S             Begins a new slideshow using the backgrounds found
                   in given directory. Implies /D and /L.
    /I <mins>      Interval in minutes between slideshow backgrounds. 
                   Defaults to 60, max is 44640. Implies /S.
    /i             The index of the file within the given directory with
                   which to begin the slideshow. Defaults to 0. Implies /S.
    /endshow       Indicates that the current slideshow is to be ended.

", Enum.GetNames(typeof(PicturePosition)).Aggregate((s, t) => s + ", " + t));
        }


        static void Main(string[] args)
        {
            try
            {
                // Output current wallpaper details
                if (args.Length == 0)
                {
                    Console.WriteLine("Current Wallpaper ");
                    Console.WriteLine("\t Filepath: \"{0}\"", WallpaperInterop.GetWallpaperFilepath());
                    Console.WriteLine("\t Position: {0}",     WallpaperInterop.GetWallpaperPosition());
					Console.WriteLine("Current login background ");
					Console.WriteLine("\t Filepath: \"{0}\"", WallpaperInterop.GetLoginBGFilepath());
				}
                // Output usage info
                else if (args.Contains("/?")) {
                    Program.printUsage();
                }
                // Handle just next background
                else if (args[0] == "/next") {
                    SlideshowHelper.NextBackground();
                }
                // Handle just endshow
                else if (args[0] == "/endshow")
                {
                    SlideshowHelper.CancelSlideshow();
                }
				else if (args[0] == "/login")
				{
					if (args.Length == 1) Program.errorOut("Expected path for option '/login'");
                    WallpaperInterop.SetLoginBG(args[1]);
				}
                // Perform wallpaper change
                else
                {
                    string path = args[0];
                    var opts = new ProgramOptions(args);

                    // End show as requested
                    if (opts.EndShow)
                    {
                        SlideshowHelper.CancelSlideshow();
                    }
                    if (opts.Slideshow)
                    {
                        // Begin slideshow
                        SlideshowHelper.BeginSlideshow(path, opts.Position,
                            opts.ShowInterval, opts.ShowIndex);
                    }
                    else
                    {
                        // Get random image if selecting from directory
                        if (opts.Directory)
                        {
                            string[] walls = SlideshowHelper.GetBackgroundsInFolder(path);
                            path = walls[new Random().Next(0, walls.Length)];
                        }
                        // Call interop code
                        WallpaperInterop.SetWallpaper(path, opts.Position, opts.Copy);
                    }

                }
            }
            catch (Exception e) {
                errorOut(e.Message);
            }
        }

        public sealed class ProgramOptions
        {
            /// <summary>
            /// Get if the source file(s) are to be copied.
            /// </summary>
            public bool Copy { get; private set; }
            /// <summary>
            /// Gets whether the directory flag (/D) is set.
            /// </summary>
            public bool Directory { get; private set; }
            /// <summary>
            /// Gets whether the slideshow flag (/S) is set.
            /// </summary>
            public bool Slideshow { get; private set; }
            /// <summary>
            /// Gets whether the end show flag (/endshow) is set.
            /// </summary>
            public bool EndShow { get; private set; }
            /// <summary>
            /// Gets the requested initial file index for a slideshow.
            /// </summary>
            public int ShowIndex { get; private set; }
            /// <summary>
            /// Gets the requested slideshow interval.
            /// </summary>
            public int ShowInterval { get; private set; }
            /// <summary>
            /// Gets the picture position to set.
            /// </summary>
            public PicturePosition Position { get; private set; }
            
            /// <summary>
            /// Creates a new ProgramOptions object, processing the given 
            /// command line arguments.
            /// </summary>
            public ProgramOptions(string[] args)
            {
                // Set defaults
                this.ShowInterval = 60;
                this.Position     = PicturePosition.Fill;

                // Process options
                for (int i = 1; i < args.Length; i++)
                {
                    if (args[1] == "/endshow") {
                        this.EndShow = true;
                    }
                    else if (args[i] == "/L") { // Don't copy
                        this.Copy = false;
                    }
                    else if (args[i] == "/D") { // Directory
                        this.Directory = true; 
                    }
                    else if (args[i] == "/S") { // Slideshow
                        this.Slideshow = true;
                    }
                    else if (args[i] == "/I") // Interval
                    {
                        i++;
                        if (args.Length == i) {
                            Program.errorOut("Expected parameter for option '/I'");
                        }
                        else
                        {
                            uint interval;
                            if (!UInt32.TryParse(args[i], out interval)) {
                               Program. errorOut("Invalid parameter for option '/I'");
                            }
                            this.ShowInterval = (int)Math.Min(interval, int.MaxValue);
                        }
                        this.Slideshow = true;
                    }
                    else if (args[i] == "/i") // Index
                    {
                        i++;
                        if (args.Length == i) {
                            Program.errorOut("Expected parameter for option '/i'");
                        }
                        else
                        {
                            uint index;
                            if (!UInt32.TryParse(args[i], out index)) {
                                Program.errorOut("Invalid parameter for option '/i'");
                            }
                            this.ShowIndex = (int)Math.Min(index, int.MaxValue);
                        }
                        this.Slideshow = true;
                    }
                    else if (args[i] == "/P") // Set PicturePosition
                    {
                        i++;
                        if (args.Length == i) {
                            Program.errorOut("Expected parameter for option '/P'");
                        }
                        else
                        {
                            PicturePosition pos;
                            if (!Enum.TryParse(args[i], out pos)) {
                                Program.errorOut("Invalid parameter for option '/P'");
                            }
                            this.Position = pos;
                        }
                    }
                    else Program.errorOut("Invalid parameter: " + args[i]);
                }
            }
        }
    }
}