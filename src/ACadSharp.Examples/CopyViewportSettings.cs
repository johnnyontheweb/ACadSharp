using ACadSharp.Entities;
using ACadSharp.IO;
using ACadSharp.Objects;
using ACadSharp.Tables;
using CSMath;
using System;
using System.Linq;

namespace ACadSharp.Examples
{
    public static class CopyViewportSettings
    {
        /// <summary>
        /// Legge un file DWG di esempio, estrae un viewport funzionante e lo clona in un nuovo documento
        /// </summary>
        public static void CreateDwgByCloning(string sampleFile, string outputFile)
        {
            // Leggi il file di esempio
            CadDocument sourceDoc;
            using (DwgReader reader = new DwgReader(sampleFile))
            {
                sourceDoc = reader.Read();
            }

            // Trova il primo layout con viewport (escludendo Model)
            Layout sourceLayout = sourceDoc.Layouts
                .FirstOrDefault(l => l.IsPaperSpace && l.AssociatedBlock?.Viewports.Count() > 1);

            if (sourceLayout == null)
            {
                Console.WriteLine("Nessun layout con viewport trovato nel file di esempio!");
                return;
            }

            Console.WriteLine($"Layout di origine trovato: {sourceLayout.Name}");
            var sourceViewports = sourceLayout.AssociatedBlock.Viewports.ToList();
            Console.WriteLine($"Viewport nel layout: {sourceViewports.Count}");

            // Prendi il viewport effettivo (non il paper viewport, che ha Id=1)
            Viewport sourceViewport = sourceViewports.FirstOrDefault(v => !v.RepresentsPaper);

            if (sourceViewport == null)
            {
                Console.WriteLine("Nessun viewport effettivo trovato!");
                return;
            }

            Console.WriteLine($"\nViewport di origine (Id={sourceViewport.Id}):");
            Console.WriteLine($"  Center: {sourceViewport.Center}");
            Console.WriteLine($"  Width: {sourceViewport.Width}");
            Console.WriteLine($"  Height: {sourceViewport.Height}");
            Console.WriteLine($"  ViewCenter: {sourceViewport.ViewCenter}");
            Console.WriteLine($"  ViewHeight: {sourceViewport.ViewHeight}");
            Console.WriteLine($"  ViewTarget: {sourceViewport.ViewTarget}");
            Console.WriteLine($"  ViewDirection: {sourceViewport.ViewDirection}");
            Console.WriteLine($"  Status: {sourceViewport.Status}");

            // ===== CREA NUOVO DOCUMENTO =====
            CadDocument doc = new CadDocument();

            // Disegna alcune linee nel ModelSpace
            Line line1 = new Line
            {
                StartPoint = new XYZ(0, 0, 0),
                EndPoint = new XYZ(100, 100, 0),
                Color = Color.Red
            };

            Line line2 = new Line
            {
                StartPoint = new XYZ(100, 0, 0),
                EndPoint = new XYZ(0, 100, 0),
                Color = Color.Green
            };

            doc.Entities.Add(line1);
            doc.Entities.Add(line2);

            // Configura Layout1 per A4
            if (doc.Layouts.TryGet("Layout1", out Layout layout))
            {
                layout.PaperSize = "ISO_A4_(210.00_x_297.00_MM)";
                layout.PaperHeight = 210.0;
                layout.PaperWidth = 297.0;
                layout.PaperUnits = PlotPaperUnits.Millimeters;

                // CLONA il viewport dal file di esempio e modificalo per il nostro documento
                Viewport vp = (Viewport)sourceViewport.Clone();

                // Adatta le dimensioni/posizione per il foglio A4
                // Mantieni le proporzioni e i parametri di vista
                vp.Center = new XYZ(105, 148.5, 0);      // Centro del foglio A4 (297x210mm)
                vp.Width = 150;                           // Dimensioni del viewport nel paper space
                vp.Height = 200;
                vp.ViewCenter = new XY(50, 50);           // Cosa guardare nel model space
                vp.ViewHeight = 120;                      // Quanto del model space mostrare

                // Assicurati che i valori di vista siano correttamente impostati
                // Questi sono critici per visualizzare il viewport
                if (vp.ViewTarget == null || vp.ViewTarget == XYZ.Zero)
                {
                    vp.ViewTarget = new XYZ(50, 50, 0);
                }
                if (vp.ViewDirection == null || vp.ViewDirection == XYZ.Zero)
                {
                    vp.ViewDirection = new XYZ(0, 0, 1);
                }

                // Copia anche gli status flag importanti dal viewport di origine
                vp.Status = sourceViewport.Status;

                Console.WriteLine($"\nViewport clonato e adattato:");
                Console.WriteLine($"  Center: {vp.Center}");
                Console.WriteLine($"  Width: {vp.Width}");
                Console.WriteLine($"  Height: {vp.Height}");
                Console.WriteLine($"  ViewCenter: {vp.ViewCenter}");
                Console.WriteLine($"  ViewHeight: {vp.ViewHeight}");
                Console.WriteLine($"  ViewTarget: {vp.ViewTarget}");
                Console.WriteLine($"  ViewDirection: {vp.ViewDirection}");
                Console.WriteLine($"  Status: {vp.Status}");

                layout.AssociatedBlock.Entities.Add(vp);

                // Bordo del foglio
                LwPolyline border = new LwPolyline();
                border.Vertices.Add(new LwPolyline.Vertex(new XY(5, 5)));
                border.Vertices.Add(new LwPolyline.Vertex(new XY(292, 5)));
                border.Vertices.Add(new LwPolyline.Vertex(new XY(292, 205)));
                border.Vertices.Add(new LwPolyline.Vertex(new XY(5, 205)));
                border.IsClosed = true;
                border.Color = Color.Blue;
                layout.AssociatedBlock.Entities.Add(border);
            }

            // Scrivi il file DWG
            using (DwgWriter writer = new DwgWriter(outputFile, doc))
            {
                writer.Write();
            }

            Console.WriteLine($"\nFile DWG creato con successo: {outputFile}");
        }
    }
}
