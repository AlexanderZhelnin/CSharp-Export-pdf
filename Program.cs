using drawer.Models;
using iText.IO.Font;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Action;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Filespec;
using iText.Layout;
using iText.Layout.Element;
using Newtonsoft.Json;

var dest = "d:\\pdf\\demo.pdf";
var file = new FileInfo(dest);
file.Directory?.Create();

#region InitMap
var scale = 2.5f;
var json = File.ReadAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"primitives.json"));
var ls = JsonConvert.DeserializeObject<ILegend[]>(json)!;

ls = ls.ToList()
       .OrderBy(l => l.Priority)
       .ToArray();

var r = new Rect { Left = 1200, Bottom = 50, Right = 4000, Top = 2850 };

var (left, bottom) = (r.Left, r.Bottom);
#endregion

var docPdf = new PdfDocument(new PdfWriter(dest));
var size = new PageSize((float)(r.Right - r.Left) * scale, (float)(r.Top - r.Bottom) * scale);
var doc = new Document(docPdf, size);
var page = docPdf.AddNewPage(size);


var font = PdfFontFactory.CreateFont(
    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FreeSans.ttf"),
    PdfEncodings.IDENTITY_H);

docPdf.AddFont(font);

var canvas = new PdfCanvas(page);

DrawMap();

BuildTable();

doc.Add(new Paragraph("1 It wight to happy sighed into begun change which nor tales not from would run had plain bliss nor but")
    .SetFontSize(100));

var style = new Style()
    .SetFontSize(50)
    .SetBold()
    .SetPaddingLeft(100)
    .SetUnderline()
    .SetOpacity(0.5f)
    .SetFontColor(new DeviceRgb(255, 0, 0));

doc.Add(new Paragraph("2 It wight to happy sighed into begun change which nor tales not from would run had plain bliss nor but").AddStyle(style));
doc.Add(new Paragraph("3 It wight to happy sighed into begun change which nor tales not from would run had plain bliss nor but").AddStyle(style));
doc.Add(new Paragraph("4 It wight to happy sighed into begun change which nor tales not from would run had plain bliss nor but").AddStyle(style));

doc.Add(
    new Paragraph()
        .AddStyle(style)
        .Add(new Div()
            .Add(new Image(ImageDataFactory.Create(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo_146.png")))))
    );

docPdf.Close();

#region DrawMap
void DrawMap()
{
    foreach (var l in ls)
        foreach (var g in l.Primitives)
        {
            canvas.SetStrokeColor(new DeviceRgb(0, 0, 0));
            canvas.SetFillColor(new DeviceRgb(255, 255, 255));

            var cs = Transform(g.Coords);

            switch (l.Type)
            {
                case GrTypeEnum.Line:

                    if (cs.Length < 4) { continue; }

                    canvas.MoveTo(cs[0], cs[1]);

                    for (var i = 2; i < cs.Length; i += 2)
                        canvas.LineTo(cs[i], cs[i + 1]);

                    canvas.Stroke();

                    break;
                case GrTypeEnum.Polygon:
                    if (cs.Length < 5) { continue; }
                    if ((g.Rect.Right - g.Rect.Left) > 2000) { continue; }

                    canvas.MoveTo(cs[0], cs[1]);

                    for (var i = 2; i < cs.Length; i += 2)
                        canvas.LineTo(cs[i], cs[i + 1]);

                    canvas.ClosePathFillStroke();

                    break;
            }
        }

    canvas.SetFillColor(new DeviceRgb(0, 0, 0));
    foreach (var l in ls)
        foreach (var g in l.Primitives)
        {
            switch (l.Type)
            {
                case GrTypeEnum.Line:
                    {
                        var cs = Transform(g.Coords);

                        if (cs.Length < 2) continue;

                        var max = 0.0;
                        var maxIndex = 0;

                        for (var i = 2; i < cs.Length; i += 2)
                        {
                            var (dx, dy) = (cs[i] - cs[i - 2], cs[i + 1] - cs[i - 1]);

                            var maxI = Math.Sqrt(dx * dx + dy * dy);
                            if (maxI > max)
                            {
                                max = maxI;
                                maxIndex = i;
                            }
                        }
                        if (max == 0) continue;

                        var (p1X, p1Y) = (cs[maxIndex], cs[maxIndex + 1]);
                        var (p2X, p2Y) = (cs[maxIndex - 2], cs[maxIndex - 1]);

                        var (cX, cY) = ((p1X + p2X) / 2, (p1Y + p2Y) / 2);

                        if (p1X > p2X) (p1X, p1Y, p2X, p2Y) = (p2X, p2Y, p1X, p1Y);

                        var (dX, dY) = (p2X - p1X, p2Y - p1Y);

                        var angle = Math.Atan2(dY, dX);

                        var p = new Paragraph();
                        p.Add(new Link(g.Name, PdfAction.CreateURI("file:///d:/")).SetFont(font));
                        p.SetFixedPosition((float)cX, (float)cY, 200);
                        p.SetRotationAngle(angle);
                        doc.Add(p);
                    }
                    break;
                case GrTypeEnum.Polygon:
                    {
                        var (x, y) = TranslateCoord((g.Rect.Left + g.Rect.Right) / 2, (g.Rect.Bottom + g.Rect.Top) / 2);
                        var p = new Paragraph();

                        p.Add(new Link(g.Name, PdfAction.CreateURI("file:///d:/")).SetFont(font));
                        p.SetFixedPosition((float)x, (float)y, 200);
                        doc.Add(p);
                    }
                    break;
            }
        }
}

double[] Transform(double[] cs)
{
    var result = new double[cs.Length];

    for (var i = 0; i < cs.Length; i += 2)
    {
        result[i] = (cs[i] - left) * scale;
        result[i + 1] = (cs[i + 1] - bottom) * scale;
    }

    return result;
}

(double x, double y) TranslateCoord(double x, double y) => ((x - left) * scale, (y - bottom) * scale);

#endregion
void BuildTable()
{
    var table = new Table(new float[] { 40, 100 })
            .UseAllAvailableWidth()
            .SetFontSize(30)
            .SetFont(font);

    var prs = ls
        .SelectMany(l => l.Primitives)
        .OrderByDescending(p => p.Name.Length)
        .DistinctBy(p => p.Name)
        .Take(50);

    var i = 0;
    foreach (var p in prs)
    {

        table
            .AddCell(new Cell()
                .Add(new Paragraph((++i).ToString()))
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))

            .AddCell(new Cell()
                .Add(new Paragraph()
                    .Add(new Link(p.Name.Replace("\n", "").Replace("\r", ""), PdfAction.CreateURI("https://www.youtube.com/channel/UCGntVzOD7faGCYbrUfd8PQg")))));
    }

    table.SetFixedPosition(100, 3000, 500);

    doc.Add(table);
}