using ACadSharp.Entities;
using ACadSharp.IO;
using ACadSharp.Objects;
using ACadSharp.Tables;
using ACadSharp.Tables.Collections;
using CSMath;
using System;
using System.Linq;

namespace ACadSharp.Examples
{
	class Program
	{
		const string _file = "../../../../../samples/sample_AC1032.dwg";

		static void Main(string[] args)
		{
			CadDocument doc;
			DwgPreview preview;
			using (DwgReader reader = new DwgReader(_file))
			{
				doc = reader.Read();
				preview = reader.ReadPreview();
			}

			exploreDocument(doc);

			CreateAndSaveLines();
		}

		/// <summary>
		/// Logs in the console the document information
		/// </summary>
		/// <param name="doc"></param>
		static void exploreDocument(CadDocument doc)
		{
			Console.WriteLine();
			Console.WriteLine("SUMMARY INFO:");
			Console.WriteLine($"\tTitle: {doc.SummaryInfo.Title}");
			Console.WriteLine($"\tSubject: {doc.SummaryInfo.Subject}");
			Console.WriteLine($"\tAuthor: {doc.SummaryInfo.Author}");
			Console.WriteLine($"\tKeywords: {doc.SummaryInfo.Keywords}");
			Console.WriteLine($"\tComments: {doc.SummaryInfo.Comments}");
			Console.WriteLine($"\tLastSavedBy: {doc.SummaryInfo.LastSavedBy}");
			Console.WriteLine($"\tRevisionNumber: {doc.SummaryInfo.RevisionNumber}");
			Console.WriteLine($"\tHyperlinkBase: {doc.SummaryInfo.HyperlinkBase}");
			Console.WriteLine($"\tCreatedDate: {doc.SummaryInfo.CreatedDate}");
			Console.WriteLine($"\tModifiedDate: {doc.SummaryInfo.ModifiedDate}");

			exploreTable(doc.AppIds);
			exploreTable(doc.BlockRecords);
			exploreTable(doc.DimensionStyles);
			exploreTable(doc.Layers);
			exploreTable(doc.LineTypes);
			exploreTable(doc.TextStyles);
			exploreTable(doc.UCSs);
			exploreTable(doc.Views);
			exploreTable(doc.VPorts);
		}

		static void exploreTable<T>(Table<T> table)
			where T : TableEntry
		{
			Console.WriteLine($"{table.ObjectName}");
			foreach (var item in table)
			{
				Console.WriteLine($"\tName: {item.Name}");

				if (item.Name == BlockRecord.ModelSpaceName && item is BlockRecord model)
				{
					Console.WriteLine($"\t\tEntities in the model:");
					foreach (var e in model.Entities.GroupBy(i => i.GetType().FullName))
					{
						Console.WriteLine($"\t\t{e.Key}: {e.Count()}");
					}
				}
			}
		}

		/// <summary>
		/// Creates a new document with 2 lines and saves it in both DXF and DWG formats
		/// </summary>
		static void CreateAndSaveLines()
		{
			CadDocument doc = new CadDocument();

			// Create first line
			var line1 = new Line();
			line1.StartPoint = new XYZ(0, 0, 0);
			line1.EndPoint = new XYZ(10, 10, 0);
			line1.Color = new Color(1); // Red

			// Create second line
			var line2 = new Line();
			line2.StartPoint = new XYZ(10, 0, 0);
			line2.EndPoint = new XYZ(0, 10, 0);
			line2.Color = new Color(3); // Green

			// Add lines to model space
			doc.ModelSpace.Entities.Add(line1);
			doc.ModelSpace.Entities.Add(line2);

			SetupLayout(doc, width: 210, height: 297, scale: 1); // A4 paper size in mm with 1:1 scale

			// Save as DWG
			string dwgPath = "../../../../../samples/output_lines.dwg";
			using (DwgWriter dwgWriter = new DwgWriter(dwgPath, doc))
			{
				dwgWriter.Write();
				Console.WriteLine($"DWG file saved: {dwgPath}");
			}

			// Save as DXF (binary)
			string dxfPath = "../../../../../samples/output_lines.dxf";
			using (DxfWriter dxfWriter = new DxfWriter(dxfPath, doc, binary: true))
			{
				dxfWriter.Write();
				Console.WriteLine($"DXF file saved: {dxfPath}");
			}
		}

		/// <summary>
		/// Sets up a layout with a viewport configured for printing on a specified paper size.
		/// The viewport is centered with margins and automatically scales to fit the drawing content.
		/// Fold marks are added for printing convenience.
		/// </summary>
		/// <param name="drawing">The CAD document to configure</param>
		/// <param name="width">Paper width in millimeters</param>
		/// <param name="height">Paper height in millimeters</param>
		/// <param name="scale">Scale factor for the viewport (drawing units per paper unit)</param>
		/// <param name="margin">Margin size in millimeters (default 5mm)</param>
		static void SetupLayout(CadDocument drawing, double width, double height, double scale, double margin = 5.0)
		{
			if (!drawing.Layouts.TryGet("Layout1", out Layout layout))
			{
				// If layout doesn't exist, create a new one
				layout = new Layout("Layout1");
				drawing.Layouts.Add(layout);
			}

			// Center viewport on paper
			// Margins: 5mm on all sides
			double paperWidth = width;
			double paperHeight = height;

			layout.PaperHeight = width;
			layout.PaperWidth = height;
			layout.PaperUnits = PlotPaperUnits.Millimeters;

			double viewportWidth = paperWidth - 2 * margin;
			double viewportHeight = paperHeight - 2 * margin;
			double centerX = paperWidth / 2;
			double centerY = paperHeight / 2;

			// Calculate bounding box
			double minX = double.MaxValue;
			double minY = double.MaxValue;
			double maxX = double.MinValue;
			double maxY = double.MinValue;

			// Get extents from model space entities
			foreach (var entity in drawing.ModelSpace.Entities)
			{
				if (entity is Line line)
				{
					minX = Math.Min(minX, Math.Min(line.StartPoint.X, line.EndPoint.X));
					maxX = Math.Max(maxX, Math.Max(line.StartPoint.X, line.EndPoint.X));
					minY = Math.Min(minY, Math.Min(line.StartPoint.Y, line.EndPoint.Y));
					maxY = Math.Max(maxY, Math.Max(line.StartPoint.Y, line.EndPoint.Y));
				}
				// Add more entity types as needed (Circle, Arc, etc.)
			}

			// Calculate viewport dimensions using bounding box
			// Add 10% margin around the drawing
			double bbWidth = maxX - minX;
			double bbHeight = maxY - minY;
			double marginPercentage = 0.1; // 10% margin
			double margin3D = Math.Max(bbWidth, bbHeight) * marginPercentage;

			double viewHeight = bbHeight + 2 * margin3D; // Viewport height (including margin)
			double viewWidth = bbWidth + 2 * margin3D;   // Viewport width (including margin)

			// Adapt the view to the paper's aspect ratio
			// If aspect ratios differ, recalculate to maintain correct ratio and include everything
			double paperAspectRatio = viewportWidth / viewportHeight;
			double bbAspectRatio = viewWidth / viewHeight;

			if (bbAspectRatio > paperAspectRatio)
			{
				// Bounding box is wider: adjust height
				viewHeight = viewWidth / paperAspectRatio;
			}
			else
			{
				// Bounding box is taller: adjust width
				viewWidth = viewHeight * paperAspectRatio;
			}

			double viewCenterX = minX + bbWidth / 2;  // Center X of bounding box
			double viewCenterY = minY + bbHeight / 2; // Center Y of bounding box

			// Remove any existing viewports
			var viewportsToRemove = new System.Collections.Generic.List<Viewport>();
			foreach (var entity in drawing.PaperSpace.Entities)
			{
				if (entity is Viewport vp)
				{
					viewportsToRemove.Add(vp);
				}
			}
			foreach (var vp in viewportsToRemove)
			{
				drawing.PaperSpace.Entities.Remove(vp);
			}

			// Find and modify existing viewport, otherwise create one
			Viewport existingViewport = null;
			foreach (var entity in drawing.PaperSpace.Entities)
			{
				if (entity is Viewport vp)
				{
					existingViewport = vp;
					break;
				}
			}

			// Modify or create viewport only if dimensions are valid
			if (viewHeight > 0 && viewWidth > 0 && viewportWidth > 0 && viewportHeight > 0)
			{
				if (existingViewport != null)
				{
					// Modify existing viewport
					existingViewport.Center = new XYZ(centerX, centerY, 0);
					existingViewport.Width = viewportWidth;
					existingViewport.Height = viewportHeight;
					existingViewport.ViewHeight = viewHeight;
					existingViewport.ViewTarget = new XYZ(viewCenterX, viewCenterY, 0);
					existingViewport.ViewDirection = new XYZ(0, 0, 1);
					existingViewport.IsInvisible = false;
					existingViewport.Scale = new Scale("scale") { DrawingUnits = scale, PaperUnits = 1 };
					existingViewport.Status = ViewportStatusFlags.UcsIconVisibility | ViewportStatusFlags.FastZoom | ViewportStatusFlags.CurrentlyAlwaysEnabled;
				}
				else
				{
					// Create new viewport
					var vp = new Viewport
					{
						Center = new XYZ(centerX, centerY, 0),
						Width = viewportWidth,
						Height = viewportHeight,
						ViewHeight = viewHeight,
						ViewTarget = new XYZ(viewCenterX, viewCenterY, 0),
						ViewDirection = new XYZ(0, 0, 1),
						IsInvisible = false,
						Scale = new Scale("scale") { DrawingUnits = scale, PaperUnits = 1 },
						Status = ViewportStatusFlags.UcsIconVisibility | ViewportStatusFlags.FastZoom | ViewportStatusFlags.CurrentlyAlwaysEnabled
					};
					// layout.AssociatedBlock.Entities.Add(vp);
					drawing.PaperSpace.Entities.Add(vp);
				}
			}
			layout.UpdatePaperViewport();
		}
	}
}