using System.Text;
using MigraDoc.Rendering;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using Macroscop_FaceRecReport.Enums;
using Macroscop_FaceRecReport.Helpers;
using Macroscop_FaceRecReport.Entities;
using Newtonsoft.Json;
using CLIArgsHandler;
using SixLabors.ImageSharp.Formats.Png;

namespace Macroscop_FaceRecReport;

public static class Program
{
    static readonly HashSet<Event> Events = [];

    public static void Main(string[] args)
    {
        var nonParams = ArgumentsHandler<Parameters>.Parse(args, $"Usage: {nameof(Macroscop_FaceRecReport)}",
            "See https://github.com/IlliumIv/Macroscop_FaceRecReport/ to check new versions, report bugs and ask for help.");

        if (Parameters.ServerHost.Value == string.Empty || Parameters.Login.Value == string.Empty)
            return;

        var starttime = Parameters.StartTime.Value.ToUniversalTime();

        // Macroscop can return only first 1000 events at one time. We should send new request to reach remaining events.
        // See "specialarchiveevents" documentation: https://macroscop.com/media/5348/download/macroscop-sdk-api-ru.pdf?v=3 page 55.
        // Supported Macroscop 2.1+
        while (Events.Count % 1000 == 0)
        {
            try
            {
                var request = $"specialarchiveevents?startTime={starttime}&endTime={Parameters.EndTime.Value.ToUniversalTime()}&eventId={Parameters.EventId.Value}";
                request += Parameters.ChannelId.Value == string.Empty ? "" : $"&channelid={Parameters.ChannelId.Value}";
                var message = new HttpRequestMessage(HttpMethod.Get, request);

                if (Connection.SendRequest(message, out var response))
                {
                    var answer = response.Content.ReadAsStringAsync().Result;

                    if (string.IsNullOrEmpty(answer))
                    {
                        Console.WriteLine($"Empty answer for request:\n{response.RequestMessage}");
                        return;
                    }

                    var events = ParseAnswer(answer);
                    if (events.Count > 0)
                        Events.UnionWith(events);
                    else
                        return;
                    starttime = events.Last().Timestamp;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message}");
                return;
            }
        }

        ProceedEvents();
    }

    private static void ProceedEvents()
    {
        if (Events.Count == 0)
            return;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var document = new Document();
        document.Info.Author = "IlliumIv";
        document.CreateDocument();

        PdfDocumentRenderer pdf = new() { Document = document };
        pdf.RenderDocument();
        var outputFile = string.IsNullOrEmpty(Parameters.OutputFile.Value) ? null : new FileInfo(Parameters.OutputFile.Value);
        var filePath = outputFile != null ? outputFile.FullName : $"Отчёт распознавания лиц {Parameters.StartTime.Value:dd.MM.yyyy HH.mm.ss} - {Parameters.EndTime.Value:dd.MM.yyyy HH.mm.ss}.pdf";
        pdf.Save(filePath);
    }

    private static void CreateDocument(this Document document)
    {
        var section = document.AddSection();
        section.PageSetup.PageFormat = Parameters.PageFormat.Value;
        section.PageSetup.Orientation = Orientation.Landscape;
        section.PageSetup.LeftMargin = "1cm";
        section.PageSetup.RightMargin = "1cm";
        section.PageSetup.TopMargin = "1cm";
        section.PageSetup.OddAndEvenPagesHeaderFooter = true;

        var pageNumber = new Paragraph();
        pageNumber.AddPageField();
        pageNumber.Format.Font.Size = Parameters.FontSize.Value;
        pageNumber.Format.Alignment = ParagraphAlignment.Center;
        section.Footers.Primary.Add(pageNumber);
        section.Footers.EvenPage.Add(pageNumber.Clone());

        var paragraph = section.AddParagraph();
        paragraph.AddText($"Отчёт распознавания лиц");
        paragraph.Format.Alignment = ParagraphAlignment.Center;
        paragraph.Format.Font.Bold = true;
        paragraph.Format.Font.Size = Parameters.FontSize.Value + 10;

        paragraph = section.AddParagraph();
        paragraph.AddText($"Период: {Parameters.StartTime.Value:dd.MM.yyyy HH.mm.ss} - {Parameters.EndTime.Value:dd.MM.yyyy HH.mm.ss}. Событий: {Events.Count}");
        paragraph.Format.Alignment = ParagraphAlignment.Center;
        paragraph.Format.Font.Size = Parameters.FontSize.Value;
        paragraph.Format.SpaceBefore = 6;
        paragraph.Format.SpaceAfter = 6;

        var table = section.AddTable();
        table.Borders.Color = Colors.Black;
        table.Borders.Width = 0.25;
        table.Format.Font.Size = Parameters.FontSize.Value;
        table.Rows.Alignment = RowAlignment.Center;

        var column = table.AddColumn();
        column.Width = Parameters.FontSize.Value * 2;
        column.Format.Alignment = ParagraphAlignment.Center;

        column = table.AddColumn();
        column.Width = Parameters.FontSize.Value * 6;
        column.Format.Alignment = ParagraphAlignment.Center;

        column = table.AddColumn();
        column.LeftPadding = 0;
        column.Width = Parameters.ImagesWidth.Value * 0.75;

        if (Parameters.ExtractImagesFromDatabase.Value)
        {
            column = table.AddColumn();
            column.LeftPadding = 0;
            column.Width = Parameters.ImagesWidth.Value * 0.75;
            column.Format.Alignment = ParagraphAlignment.Center;
        }

        column = table.AddColumn();
        column.Width = Parameters.FontSize.Value * 12;

        column = table.AddColumn();
        column.Width = Parameters.FontSize.Value * 12;

        table.AddColumn();

        column = table.AddColumn();
        column.Width = Parameters.FontSize.Value * 12;

        column = table.AddColumn();
        column.Width = Parameters.FontSize.Value * 5;
        column.Format.Alignment = ParagraphAlignment.Center;

        column = table.AddColumn();
        column.Width = Parameters.FontSize.Value * 5;
        column.Format.Alignment = ParagraphAlignment.Center;

        column = table.AddColumn();
        column.Width = Parameters.FontSize.Value * 12;

        var row = table.AddRow();
        row.HeadingFormat = true;
        row.Format.SpaceBefore = 2;
        row.Format.SpaceAfter = 2;
        row.Format.Font.Bold = true;
        row.Format.Alignment = ParagraphAlignment.Center;
        row.VerticalAlignment = VerticalAlignment.Center;
        row.Shading.Color = Colors.LightGray;

        var offset = Parameters.ExtractImagesFromDatabase.Value ? 1 : 0;

        row[0].AddParagraph("№");
        row[1].AddParagraph("Время");
        row[2].AddParagraph("Фото");
        if (Parameters.ExtractImagesFromDatabase.Value)
            row[3].AddParagraph("Фото в базе");
        row[offset + 3].AddParagraph("ФИО, Уверенность");
        row[offset + 4].AddParagraph("Доп. информация");
        row[offset + 5].AddParagraph("Группы");
        row[offset + 6].AddParagraph("Камера");
        row[offset + 7].AddParagraph("Пол");
        row[offset + 8].AddParagraph("Возраст");
        row[offset + 9].AddParagraph("Эмоции");

        var i = 0;

        foreach (var macroscopEvent in Events)
        {
            i++;
            if (macroscopEvent == null)
                continue;
            row = table.AddRow();
            row.HeightRule = RowHeightRule.Exactly;
            row.Height = Parameters.ImagesWidth.Value * 0.75;

            row[0].AddParagraph($"{i}");
            row[1].AddParagraph($"{macroscopEvent.Timestamp.ToLocalTime()}");

            if (macroscopEvent.TryGetPicture(out var picture, Parameters.ImagesWidth.Value, Parameters.ImagesWidth.Value))
            {
                row[2].AddImage("base64:" + picture!.Base64);

                if (Parameters.SaveImages.Value && picture is not null)
                {
                    var imageBytes = Convert.FromBase64String(macroscopEvent.ImageBytes);
                    picture = new Picture(imageBytes);

                    using var memoryStream = new MemoryStream();
                    picture.Image.Save(memoryStream, picture.Image.Configuration.ImageFormatsManager.GetEncoder(PngFormat.Instance));

                    var imagesDir = !string.IsNullOrEmpty(Parameters.OutputFile.Value)
                        ? Path.Combine(Directory.GetParent(Parameters.OutputFile.Value)?.FullName ?? string.Empty, $"Images")
                        : Path.Combine(Environment.CurrentDirectory, $"Images");

                    Directory.CreateDirectory(imagesDir);

                    using var fileStream = File.Create(Path.Combine(imagesDir, $"{i}.png"));

                    memoryStream.WriteTo(fileStream);
                }
            }

            if (Parameters.ExtractImagesFromDatabase.Value)
            {
                if (macroscopEvent.TryGetPictureFromDatabase(out picture, Parameters.ImagesWidth.Value, Parameters.ImagesWidth.Value))
                    row[3].AddImage("base64:" + picture!.Base64);
            }

            var name = $"{macroscopEvent.FirstName} {macroscopEvent.LastName} {macroscopEvent.Patronymic}".Trim();
            var similarity = macroscopEvent.Similarity > 0 ? $"{string.Format("{0:P0}", macroscopEvent.Similarity).Replace(" ", "")}" : string.Empty;
            name += name != string.Empty ? $", {similarity}" : similarity;
            row[offset + 3].AddParagraph($"{name}");
            row[offset + 4].AddParagraph($"{macroscopEvent.AdditionalInfo}");
            row[offset + 5].AddParagraph($"{macroscopEvent.Groups?.ReplaceWhitespace()}");
            row[offset + 6].AddParagraph($"{macroscopEvent.ChannelName}");
            row[offset + 7].AddParagraph($"{StringEnum.GetStringValue(macroscopEvent.Gender)}");
            row[offset + 8].AddParagraph($"{macroscopEvent.Age}");
            row[offset + 9].AddParagraph($"{StringEnum.GetStringValue(macroscopEvent.Emotion)}, {string.Format("{0:P2}", macroscopEvent.EmotionConfidence).Replace(" ", "")}");
        }
    }

    /// <summary>
    /// Split server answer to events.
    /// </summary>
    /// <returns>
    /// HashSet contained server events.
    /// </returns>
    private static HashSet<Event> ParseAnswer(string answer)
    {
        var events = new HashSet<Event>();

        using var reader = new StringReader(answer);

        var str = string.Empty;

        while (reader.Peek() >= 0)
        {
            var line = reader.ReadLine();

            if (line?.Length > 0 && line?[0] == '}')
            {
                if (!string.IsNullOrEmpty(str))
                {
                    var recognitionEvent = JsonConvert.DeserializeObject<Event>(str + "}", new JsonSerializerSettings()
                    {
                        DateFormatString = "dd.MM.yyyy HH:mm:ss",
                        Culture = new System.Globalization.CultureInfo("ru-RU")
                    });

                    if (recognitionEvent is not null)
                        events.Add(recognitionEvent);
                }

                str = "{";
            }
            else
            {
                str += line;
                str += Environment.NewLine;
            }
        }

        return events;
    }
}