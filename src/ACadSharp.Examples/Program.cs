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
			CreateDwgWithA4Layout("output_a4_example.dwg");
			//Console.WriteLine("=== ANALYZING SAMPLE FILE ===");
			//AnalyzeSampleViewports.AnalyzeSample();

			//Console.WriteLine("\n\n=== CREATING NEW FILE BY CLONING VIEWPORT SETTINGS ===");
			//CopyViewportSettings.CreateDwgByCloning(_file, "output_a4_example.dwg");

			CadDocument doc;
			DwgPreview preview;
			using (DwgReader reader = new DwgReader(_file))
			{
				doc = reader.Read();
				preview = reader.ReadPreview();
			}

			exploreDocument(doc);
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

		static void CreateDwgWithA4Layout(string outFile)
		{
			CadDocument doc = new CadDocument();
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

			if (doc.Layouts.TryGet("Layout1", out Layout layout))
			{
				layout.PaperSize = "ISO_A4_(210.00_x_297.00_MM)";
				layout.PaperHeight = 210.0;
				layout.PaperWidth = 297.0;
				layout.PaperUnits = PlotPaperUnits.Millimeters;

				Viewport vp = new Viewport
				{
					Center = new XYZ(148.5, 105, 0),
					Width = 200,
					Height = 150,
					ViewCenter = new XY(50, 50),
					ViewHeight = 120,

					ViewTarget = new XYZ(50, 50, 0),
					ViewDirection = new XYZ(0, 0, 1),

					Status = ViewportStatusFlags.UcsIconVisibility | ViewportStatusFlags.FastZoom | ViewportStatusFlags.CurrentlyAlwaysEnabled
				};

				//layout.AssociatedBlock.Entities.Add(vp);
				doc.PaperSpace.Entities.Add(vp);

				LwPolyline border = new LwPolyline();
				border.Vertices.Add(new LwPolyline.Vertex(new XY(5, 5)));
				border.Vertices.Add(new LwPolyline.Vertex(new XY(292, 5)));
				border.Vertices.Add(new LwPolyline.Vertex(new XY(292, 205)));
				border.Vertices.Add(new LwPolyline.Vertex(new XY(5, 205)));
				border.IsClosed = true;
				border.Color = Color.Blue;
				layout.AssociatedBlock.Entities.Add(border);
			}

			using (DwgWriter writer = new DwgWriter(outFile, doc))
			{
				writer.Write();
			}
			Console.WriteLine($"Successfully created: {outFile}");
		}
	}
}