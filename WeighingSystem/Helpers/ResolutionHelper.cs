using System;
using System.Collections.Generic;
using System.Drawing;
using WinSize = System.Windows.Size;
using System.Linq;
using System.Windows.Media;

namespace WeighingSystem.Helpers
{
    public class ResolutionHelper
    {
        private static readonly List<Size> PopularResolutions = new List<Size>
        {
            new Size(320, 240),
            new Size(640, 360),   // 360p
            new Size(854, 480),   // 480p
            new Size(960, 540),   // 540p
            new Size(1280, 720),  // 720p
            new Size(1600, 900),  // 900p
            new Size(1920, 1080), // 1080p
            new Size(2560, 1440)  // 1440p (2K)
        };

        public static WinSize GetClosestResolution(WinSize input)
        {
            var resolution = GetClosestResolution(new Size((int)input.Width,(int)input.Height));
            return new WinSize(resolution.Width, resolution.Height);
        }

        public static Size GetClosestResolution(Size input)
        {
            // Находим разрешение с минимальной разницей площади
            return PopularResolutions
                .OrderBy(r => Math.Abs(GetArea(r) - GetArea(input)))
                .First();
        }

        private static int GetArea(Size size) => size.Width * size.Height;
    } 
}