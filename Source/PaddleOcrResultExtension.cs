using OpenCvSharp;
using Sdcb.PaddleOCR;
using System.Collections.Generic;
using System.Linq;

namespace SilverMation
{
    public static class PaddleOcrResultExtension
    {
        public static bool RegionHasText(this PaddleOcrResult result, ReadOnlySpan<char> text)
        {
            foreach (ref readonly PaddleOcrResultRegion item in result.Regions.AsSpan())
            {
                if (item.Text.AsSpan().Contains(text, StringComparison.InvariantCulture))
                {
                    return true;
                }
            }
            return false;
        }

        public static PaddleOcrResultRegion FindRegionByText(this PaddleOcrResult result, ReadOnlySpan<char> text)
        {
            foreach (ref readonly PaddleOcrResultRegion item in result.Regions.AsSpan())
            {
                if (item.Text.AsSpan().Contains(text, StringComparison.InvariantCulture))
                {
                    return item;
                }
            }
            return default;
        }

        public static Rect FindRectByText(this PaddleOcrResult result, string text)
        {
            foreach (ref PaddleOcrResultRegion item in result.Regions.AsSpan())
            {
                if (item.Text.Contains(text))
                {
                    return item.Rect.BoundingRect();
                }
            }
            return default;
        }
    }
}
