using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

//File:   PigSprites.cs
//Maps a pig's landing position (the strings used by the game engine) to one of
//the sprite images in Resources\.
//
//Two visual variants exist per pose:
//  -blank / -dot         the scoring dot on the flank hidden / shown
//  side / side-mirrored   a sider facing left / right
//
//For a "Side" position the dot variant is fixed by the rules (No Dot -> blank,
//Dot -> dot) and only the facing is cosmetic; for every other pose the dot is
//cosmetic too. Cosmetic choices are made at random per roll.

namespace PassThePigsConsole
{
    internal static class PigSprites
    {
        private static readonly string ResourceDir =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");

        //Loaded images are frozen, so the cache is safe to share across threads.
        private static readonly Dictionary<string, ImageSource> Cache =
            new Dictionary<string, ImageSource>();

        //The sprite image for a pig that landed in `position`. `rng` chooses
        //between the cosmetic variants.
        public static ImageSource Image(string position, Random rng)
        {
            return Load(FileName(position, rng));
        }

        //Just the file name for `position` (e.g. "razorback-dot.png"). Useful for logging.
        public static string FileName(string position, Random rng)
        {
            string pose;
            bool dot;

            switch (position)
            {
                case "Side (No Dot)": pose = SideFacing(rng); dot = false; break;
                case "Side (Dot)": pose = SideFacing(rng); dot = true; break;
                case "Razorback": pose = "razorback"; dot = Coin(rng); break;
                case "Trotter": pose = "trotter"; dot = Coin(rng); break;
                case "Snouter": pose = "snouter"; dot = Coin(rng); break;
                case "Leaning Jowler": pose = "jowler"; dot = Coin(rng); break;
                default: pose = "trotter"; dot = false; break;
            }

            return pose + (dot ? "-dot" : "-blank") + ".png";
        }

        private static bool Coin(Random rng)
        {
            return rng.Next(2) == 0;
        }

        private static string SideFacing(Random rng)
        {
            return rng.Next(2) == 0 ? "side" : "side-mirrored";
        }

        private static ImageSource Load(string fileName)
        {
            lock (Cache)
            {
                ImageSource cached;
                if (Cache.TryGetValue(fileName, out cached))
                {
                    return cached;
                }

                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(Path.Combine(ResourceDir, fileName));
                bmp.EndInit();
                bmp.Freeze();

                Cache[fileName] = bmp;
                return bmp;
            }
        }
    }
}
