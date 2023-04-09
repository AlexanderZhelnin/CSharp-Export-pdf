using drawer.Models;
using iText.IO.Font;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Action;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;
using Newtonsoft.Json;

var dest = "d:\\pdf\\demo.pdf";
var scale = 2.5f;

var json = File.ReadAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"primitives.json"));
var ls = JsonConvert.DeserializeObject<ILegend[]>(json)!;

ls = ls.ToList()
       .OrderBy(l => l.Priority)
       .ToArray();

var r = new Rect { Left = 1200, Bottom = 50, Right = 4000, Top = 2850 };

var (left, bottom) = (r.Left, r.Bottom);

var file = new FileInfo(dest);
file.Directory?.Create();

var docPdf = new PdfDocument(new PdfWriter(dest));
var size = new PageSize((float)(r.Right - r.Left) * scale, (float)(r.Top - r.Bottom) * scale);
var doc = new Document(docPdf, size);
var page = docPdf.AddNewPage(size);

var FONT = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FreeSans.ttf");
var font = PdfFontFactory.CreateFont(FONT, PdfEncodings.IDENTITY_H);

docPdf.AddFont(font);

var canvas = new PdfCanvas(page);

DrawMap();

BuildTable();

docPdf.Close();

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
                        p.Add(new Link(g.Name, PdfAction.CreateGoTo("d:\\")).SetFont(font));
                        p.SetFixedPosition((float)cX, (float)cY, 200);
                        p.SetRotationAngle(angle);
                        doc.Add(p);
                    }
                    break;
                case GrTypeEnum.Polygon:
                    {
                        var (x, y) = TranslateCoord((g.Rect.Left + g.Rect.Right) / 2, (g.Rect.Bottom + g.Rect.Top) / 2);
                        var p = new Paragraph();

                        p.Add(new Link(g.Name, PdfAction.CreateGoTo("d:\\")).SetFont(font));
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


void BuildTable()
{
    var table = new Table(new float[] { 30, 100 })
            .UseAllAvailableWidth();

    var prs = ls.SelectMany(l => l.Primitives).Where(p => p.Name.Length > 10).Take(100);
    var i = 0;
    foreach (var p in prs)
    {
        var cell = new Cell();
        cell.Add(new Paragraph((++i).ToString()));
        table.AddCell(cell);

        cell = new Cell();
        var par = new Paragraph();
        par.Add(new Link(p.Name, PdfAction.CreateGoTo("d:\\")).SetFont(font));
        cell.Add(par);
        table.AddCell(cell);
    }

    table.SetFixedPosition(100, 300, 500);

    doc.Add(table);
}