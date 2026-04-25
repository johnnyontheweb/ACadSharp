using ACadSharp.Entities;
using ACadSharp.IO;
using ACadSharp.Objects;
using ACadSharp.Tables;
using ACadSharp.Tables.Collections;
using CSMath;
using System;
using System.IO;
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


			// Hatch circolare
			Hatch hatch = new Hatch();
			hatch.IsSolid = true;
			hatch.Pattern = HatchPattern.Solid;
			hatch.Color = Color.Yellow;

			// Creazione del cerchio per il bordo
			Circle circle = new Circle
			{
				Center = new XYZ(50, 50, 0),
				Radius = 20,
				Color = Color.Cyan
			};

			// Percorso di confine (Boundary Path)
			Hatch.BoundaryPath path = new Hatch.BoundaryPath();
			path.Edges.Add(new Hatch.BoundaryPath.Arc(circle));
			hatch.Paths.Add(path);

			doc.Entities.Add(circle);
			doc.Entities.Add(hatch);

			// Quotatura lineare (Linear Dimension) per la linea della X
			DimensionStyle dimStyle = doc.DimensionStyles.FirstOrDefault() ?? DimensionStyle.Default;
			dimStyle.TextHeight = 5.0; // Aumenta dimensione testo
			dimStyle.TickSize = 2.0;   // Frecce oblique (tratti) invece di punte di freccia

			DimensionAligned dim = new DimensionAligned
			{
				FirstPoint = line1.StartPoint,
				SecondPoint = line1.EndPoint,
				Style = dimStyle,
				Offset = 15,
				Color = Color.Magenta
			};

			doc.Entities.Add(dim);



			if (doc.Layouts.TryGet("Layout1", out Layout layout))
			{
				layout.PaperSize = "ISO_A4_(210.00_x_297.00_MM)";
				layout.PaperHeight = 210.0;
				layout.PaperWidth = 297.0;
				layout.PaperUnits = PlotPaperUnits.Millimeters;

				// Viewport centrato sulla carta A4
				// Margini: 5mm su tutti i lati
				double paperWidth = 297.0;
				double paperHeight = 210.0;
				double margin = 5.0;

				double viewportWidth = paperWidth - 2 * margin;   // 287mm
				double viewportHeight = paperHeight - 2 * margin;  // 200mm
				double centerX = paperWidth / 2;                   // 148.5mm
				double centerY = paperHeight / 2;                  // 105mm

				// Calcolo della scala: vogliamo vedere l'area 0-100 in entrambi gli assi
				// con un piccolo margine intorno
				// ViewHeight è in unità model space
				// Height è in unità paper space (mm)
				// La formula è: ViewWidth/ViewHeight = Width/Height (rapporto di aspetto)
				// Se vogliamo mostrare area da 0-100, con margine 10, abbiamo bisogno di 120 unità
				double viewHeight = 120.0;      // 120 unità model space (mostra 0-100 con margine)
				double viewWidth = viewHeight * (viewportWidth / viewportHeight);  // Mantiene rapporto

				double viewCenterX = 50.0;     // Centro a X=50 (centro dell'area 0-100)
				double viewCenterY = 50.0;     // Centro a Y=50 (centro dell'area 0-100)

				Console.WriteLine($"DEBUG - Scala viewport:");
				Console.WriteLine($"  Paper: {viewportWidth}mm x {viewportHeight}mm");
				Console.WriteLine($"  Model: {viewWidth} x {viewHeight} unità");
				Console.WriteLine($"  Scala: {viewWidth/viewportWidth} unità/mm (X), {viewHeight/viewportHeight} unità/mm (Y)");

				Viewport vp = new Viewport
				{
					Center = new XYZ(centerX, centerY, 0),
					Width = viewportWidth,
					Height = viewportHeight,
					//ViewCenter = new XY(viewCenterX, viewCenterY), // no, è in DCS
					ViewHeight = viewHeight,

					ViewTarget = new XYZ(viewCenterX, viewCenterY, 0),
					ViewDirection = new XYZ(0, 0, 1),

					Status = ViewportStatusFlags.UcsIconVisibility | ViewportStatusFlags.FastZoom | ViewportStatusFlags.CurrentlyAlwaysEnabled
				};

				//layout.AssociatedBlock.Entities.Add(vp);
				doc.PaperSpace.Entities.Add(vp);

				LwPolyline border = new LwPolyline();
				border.Vertices.Add(new LwPolyline.Vertex(new XY(margin, margin)));
				border.Vertices.Add(new LwPolyline.Vertex(new XY(paperWidth - margin, margin)));
				border.Vertices.Add(new LwPolyline.Vertex(new XY(paperWidth - margin, paperHeight - margin)));
				border.Vertices.Add(new LwPolyline.Vertex(new XY(margin, paperHeight - margin)));
				border.IsClosed = true;
				border.Color = Color.Blue;
				layout.AssociatedBlock.Entities.Add(border);

				// table
				TableEntity table = new TableEntity
				{
					InsertPoint = new XYZ(50, 50, 0),
					HorizontalDirection = new XYZ(1, 0, 0)
				};
				table.Columns.Add(new TableEntity.Column { Width = 40 });
				table.Columns.Add(new TableEntity.Column { Width = 40 });
				table.Columns.Add(new TableEntity.Column { Width = 40 });
				for (int i = 0; i < 3; i++)
				{
					var row = new TableEntity.Row { Height = 10 };
					for (int j = 0; j < 3; j++)
					{
						var cell = new TableEntity.Cell();
						var content = new TableEntity.CellContent();
						content.ContentType = TableEntity.TableCellContentType.Value;
						cell.Contents.Add(content);
						row.Cells.Add(cell);
					}
					table.Rows.Add(row);
				}
				table.GetCell(0, 0).Content.CadValue.SetValue("Title 1", CadValueType.String);
				table.GetCell(0, 1).Content.CadValue.SetValue("Title 2", CadValueType.String);
				table.GetCell(1, 0).Content.CadValue.SetValue("Data 1", CadValueType.String);

				layout.AssociatedBlock.Entities.Add(table);

				
			}



			using (DwgWriter writer = new DwgWriter(outFile, doc))
			{
				writer.Write();
			}
			Console.WriteLine($"Successfully created: {outFile}");

			// dxf test with stream
			MemoryStream ms = new MemoryStream();
			DxfWriterConfiguration config = new DxfWriterConfiguration(); config.CloseStream = false;
			DxfWriter.Write(ms, doc, false, config);
			Console.WriteLine($"Successfully created DXF in memory stream, is open: {ms.CanSeek}");
		}
	}
}