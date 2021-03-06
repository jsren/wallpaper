﻿/* Wallpaper.cs - (c) James S Renwick 2014
 * ---------------------------------------
 * Version 1.1.0
 * 
 * P/invoke wrapper for getting/setting current
 * desktop background (wallpaper.)
 */
using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;
using System;

namespace Win32.Desktop
{
    /// <summary>
    /// Sizing options for desktop backgrounds.
    /// </summary>
    public enum PicturePosition
    {
        /// <summary>
        /// Centers the background without resizing.
        /// </summary>
        Center  = 0,
        /// <summary>
        /// Tiles the background without resizing.
        /// </summary>
        Tile    = 1,
        /// <summary>
        /// Stretches the background to fill the desktop.
        /// </summary>
        Stretch = 2,
        /// <summary>
        /// Sizes the image to fit the desktop without trimming. 
        /// Preserves ratio. (Vista and above.)
        /// </summary>
        Fit     = 6,
        /// <summary>
        /// Sizes the image to fit the entire desktop.
        /// Preserves ratio. (Vista and above.)
        /// </summary>
        Fill    = 10,
    }

    /// <summary>
    /// Class providing windows desktop background functions.
    /// </summary>
    public static class WallpaperInterop
    {
        // Interop constants
        private const int  maxPathLength         = 0x104;
        private const uint SPI_GETDESKWALLPAPER  = 0x73;
        private const uint SPI_SETDESKWALLPAPER  = 0x14;
        private const uint SPIF_UPDATEINIFILE    = 0x01;
        private const uint SPIF_SENDWININICHANGE = 0x02;

        // user32 import
        [DllImport("user32.dll", CharSet = CharSet.Auto, BestFitMapping=false)]
        static extern int SystemParametersInfo(uint uAction, uint uParam, string lpvParam, uint fuWinIni);

        /// <summary>
        /// Changes the current desktop background to the given image.
        /// </summary>
        /// <param name="filepath">The path to the image to use.</param>
        /// <param name="sizing">The sizing to use.</param>
        public static void SetWallpaper(string filepath, PicturePosition sizing)
        {
            WallpaperInterop.SetWallpaper(filepath, sizing, false);
        }

        /// <summary>
        /// Changes the current desktop background.
        /// </summary>
        /// <param name="filepath">The path to the image to use.</param>
        /// <param name="sizing">The sizing to use.</param>
        /// <param name="copy">Whether to create and use a copy of the image.</param>
        public static void SetWallpaper(string filepath, PicturePosition sizing, bool copy)
        {
            if (!File.Exists(filepath))
                throw new FileNotFoundException("Invalid filepath specified for wallpaper");

            // Get full path to file
            filepath = Path.GetFullPath(filepath);

            if (copy)
            {
                string tmpFile = Path.GetTempFileName();
                tmpFile = tmpFile.Substring(0, tmpFile.LastIndexOf(".")) + Path.GetExtension(filepath);

                File.Copy(filepath, (filepath = tmpFile), true);
            }

            using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
            {
                key.SetValue("WallpaperStyle", ((int)sizing).ToString());
                key.SetValue("TileWallpaper", sizing == PicturePosition.Tile ? "1" : "0");
            }

            int res = SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, filepath,
                SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);

            if (res < 0) throw new ExternalException("Error setting desktop background", res);
        }

		/// <summary>
		/// Sets the system login (startup) background to a copy of the given JPEG image.
		/// The image must be less than 244KiB.
		/// </summary>
		/// <param name="filepath">The path to the JPEG to set as the login background.</param>
		public static void SetLoginBG(string filepath)
		{
			string bgDefaultPath = Environment.GetFolderPath(Environment.SpecialFolder.System)
				+ @"\oobe\info\backgrounds\backgroundDefault.jpg";

			// Create dir if not already existing
			Directory.CreateDirectory(Path.GetDirectoryName(bgDefaultPath));

			// Reject image if too large or wrong format
			// TODO: Resize image?
			var fileInfo = new FileInfo(filepath);

            if (fileInfo.Length > 244 * 1024)
				Wallpaper.Program.errorOut("Image filesize must be less than 244KiB to set as login background.");

			// TODO: Check format properly? Convert image?
			if (fileInfo.Extension != ".jpg")
				Wallpaper.Program.errorOut("Image must be in JPEG (JFIF) format to set as login background.");

			File.Copy(filepath, bgDefaultPath, true);

			using (var key = Registry.LocalMachine.CreateSubKey(
					@"SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\LogonUI\Background", RegistryKeyPermissionCheck.ReadWriteSubTree))
			{
				key.SetValue("OEMBackground", 1);
            }
		}


		/// <summary>
		/// Gets a path to the current system login (startup) background image. Returns null
		/// if using default image.
		/// </summary>
		public static string GetLoginBGFilepath()
		{
			string bgDefaultPath = Environment.GetFolderPath(Environment.SpecialFolder.System)
				+ @"\oobe\info\backgrounds\backgroundDefault.jpg";

            var key = Registry.LocalMachine.OpenSubKey(
				@"SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\LogonUI\Background", false);

			if (key == null) return null;

			using (key)
			{
				object value = key.GetValue("OEMBackground");
				if (value == null || (int)value == 0) return null;
			}
			return File.Exists(bgDefaultPath) ? Path.GetFullPath(bgDefaultPath) : null;
		}


		/// <summary>
		/// Gets a path to the current desktop background image.
		/// </summary>
		/// <returns>A path to the current desktop background image.</returns>
		public static string GetWallpaperFilepath()
        {
            string filepath = new string('\0', maxPathLength);

            int res = SystemParametersInfo(SPI_GETDESKWALLPAPER, (uint)maxPathLength, filepath, 0);
            if (res < 0) throw new ExternalException("Error retrieving current wallpaper filepath", res);

            return filepath.Substring(0, filepath.IndexOf('\0'));
        }

        /// <summary>
        /// Gets the sizing of the current desktop background image.
        /// </summary>
        /// <returns>The sizing of the current desktop background image.</returns>
        public static PicturePosition GetWallpaperPosition()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
            {
                if ((string)key.GetValue("TileWallpaper") == "1") {
                    return PicturePosition.Tile;
                }
                else {
                    return (PicturePosition)int.Parse((string)key.GetValue("WallpaperStyle"));
                }
            }
        }

    }
}
