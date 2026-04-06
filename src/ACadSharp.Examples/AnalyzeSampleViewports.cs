using ACadSharp.IO;
using System;
using System.Linq;

namespace ACadSharp.Examples
{
    public static class AnalyzeSampleViewports
    {
        public static void AnalyzeSample()
        {
            const string sampleFile = "../../../../../samples/sample_AC1032.dwg";

            using (DwgReader reader = new DwgReader(sampleFile))
            {
                var doc = reader.Read();

                Console.WriteLine("\n=== LAYOUT ANALYSIS ===");
                foreach (var layout in doc.Layouts)
                {
                    Console.WriteLine($"\nLayout: {layout.Name}");
                    Console.WriteLine($"  Paper Size: {layout.PaperSize}");
                    Console.WriteLine($"  Paper Height: {layout.PaperHeight}");
                    Console.WriteLine($"  Paper Width: {layout.PaperWidth}");

                    if (layout.AssociatedBlock != null)
                    {
                        var vpList = layout.AssociatedBlock.Viewports.ToList();
                        Console.WriteLine($"  Associated Block Viewports Count: {vpList.Count}");

                        int vpIndex = 1;
                        foreach (var vp in vpList)
                        {
                            Console.WriteLine($"\n    Viewport #{vpIndex} (Id={vp.Id}):");
                            Console.WriteLine($"      RepresentsPaper: {vp.RepresentsPaper}");
                            Console.WriteLine($"      Center: {vp.Center}");
                            Console.WriteLine($"      Width: {vp.Width}");
                            Console.WriteLine($"      Height: {vp.Height}");
                            Console.WriteLine($"      ViewCenter: {vp.ViewCenter}");
                            Console.WriteLine($"      ViewHeight: {vp.ViewHeight}");
                            Console.WriteLine($"      ViewWidth: {vp.ViewWidth}");
                            Console.WriteLine($"      ViewTarget: {vp.ViewTarget}");
                            Console.WriteLine($"      ViewDirection: {vp.ViewDirection}");
                            Console.WriteLine($"      Status: {vp.Status}");
                            vpIndex++;
                        }
                    }
                    else
                    {
                        Console.WriteLine("  No Associated Block!");
                    }
                }
            }
        }
    }
}
