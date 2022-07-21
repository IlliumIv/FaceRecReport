using System.Text;
using MigraDoc.Rendering;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using Macroscop_FaceRecReport.Enums;
using Macroscop_FaceRecReport.Helpers;
using Macroscop_FaceRecReport.Entities;

namespace Macroscop_FaceRecReport
{
    internal static class Program
    {
        static string ServerHost = string.Empty;
        static ushort ServerPort = 8080;
        public static string ServerLogin = string.Empty;
        public static string ServerPassword = "";
        static DateTime StartTime;
        static DateTime EndTime = DateTime.Now;
        static string ChannelId = string.Empty;
        static FileInfo? OutputFile;
        static int ImagesWidth = 100;
        static int FontSize = 8;
        static PageFormat PageFormat = PageFormat.A4;
        static bool ExtractImagesFromDatabase = false;

        public static string AuthString => $"{ServerLogin}:{ServerPassword.CreateMD5()}";
        private static readonly string _faceDetectEventId = "427f1cc3-2c2f-4f50-8865-56ae99c3610d";

        static readonly HashSet<Event> Events = new();
        public static HttpClient MacroscopHttpClient = new();

        public static void Main(string[] args)
        {
            ParseArgs(args);
            if (ServerHost == string.Empty || ServerLogin == string.Empty) return;

            MacroscopHttpClient.BaseAddress = new Uri($"http://{ServerHost}:{ServerPort}");
            var starttime = StartTime;

            // Macroscop can return only first 1000 events at one time. We should send new request to reach remaining events.
            // See "specialarchiveevents" documentation: https://macroscop.com/media/5348/download/macroscop-sdk-api-ru.pdf?v=3 page 55.
            // Supported Macroscop 2.1+
            while (Events.Count % 1000 == 0)
            {
                try 
                {
                    var request = $"specialarchiveevents?startTime={starttime.ToUniversalTime()}&endTime={EndTime.ToUniversalTime()}&eventId={_faceDetectEventId}";
                    request += ChannelId == string.Empty ? "" : $"&channelid={ChannelId}";
                    var message = new HttpRequestMessage(HttpMethod.Get, request);
                    message.Headers.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes(AuthString))}");
                    var response = MacroscopHttpClient.Send(message);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var answer = response.Content.ReadAsStringAsync().Result;

                        if (answer == string.Empty)
                        {
                            Console.WriteLine($"Empty answer for request:\n{response.RequestMessage}");
                            return;
                        }

                        var events = ParseAnswer(answer);
                        if (events.Count > 0) Events.UnionWith(events);
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
            if (Events.Count == 0) return;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var document = new Document();
            document.Info.Author = "IlliumIv";
            document.CreateDocument();

            var pdf = new PdfDocumentRenderer(true);
            pdf.Document = document;
            pdf.RenderDocument();
            var filePath = OutputFile != null? OutputFile.FullName : $"Отчёт распознавания лиц {StartTime:dd.MM.yyyy HH.mm.ss} - {EndTime:dd.MM.yyyy HH.mm.ss}.pdf";
            pdf.Save(filePath);
        }

        private static void CreateDocument(this Document document)
        {
            var section = document.AddSection();
            section.PageSetup.PageFormat = PageFormat;
            section.PageSetup.Orientation = Orientation.Landscape;
            section.PageSetup.LeftMargin = "1cm";
            section.PageSetup.RightMargin = "1cm";
            section.PageSetup.TopMargin = "1cm";
            section.PageSetup.OddAndEvenPagesHeaderFooter = true;

            var pageNumber = new Paragraph();
            pageNumber.AddPageField();
            pageNumber.Format.Font.Size = FontSize;
            pageNumber.Format.Alignment = ParagraphAlignment.Center;
            section.Footers.Primary.Add(pageNumber);
            section.Footers.EvenPage.Add(pageNumber.Clone());

            var paragraph = section.AddParagraph();
            paragraph.AddText($"Отчёт распознавания лиц");
            paragraph.Format.Alignment = ParagraphAlignment.Center;
            paragraph.Format.Font.Bold = true;
            paragraph.Format.Font.Size = FontSize + 10;

            paragraph = section.AddParagraph();
            paragraph.AddText($"Период: {StartTime:dd.MM.yyyy HH.mm.ss} - {EndTime:dd.MM.yyyy HH.mm.ss}. Событий: {Events.Count}");
            paragraph.Format.Alignment = ParagraphAlignment.Center;
            paragraph.Format.Font.Size = FontSize;
            paragraph.Format.SpaceBefore = 6;
            paragraph.Format.SpaceAfter = 6;

            var table = section.AddTable();
            table.Borders.Color = Colors.Black;
            table.Borders.Width = 0.25;
            table.Format.Font.Size = FontSize;
            table.Rows.Alignment = RowAlignment.Center;

            var column = table.AddColumn();
            column.Width = FontSize * 6;
            column.Format.Alignment = ParagraphAlignment.Center;
            column = table.AddColumn();
            column.LeftPadding = 0;
            column.Width = ImagesWidth * 0.75;
            if (ExtractImagesFromDatabase)
            {
                column = table.AddColumn();
                column.LeftPadding = 0;
                column.Width = ImagesWidth * 0.75;
            }

            column = table.AddColumn();
            column.Width = FontSize * 12;
            column = table.AddColumn();
            column.Width = FontSize * 12;
            table.AddColumn();
            column = table.AddColumn();
            column.Width = FontSize * 12;
            column = table.AddColumn();
            column.Width = FontSize * 5;
            column.Format.Alignment = ParagraphAlignment.Center;
            column = table.AddColumn();
            column.Width = FontSize * 5;
            column.Format.Alignment = ParagraphAlignment.Center;
            column = table.AddColumn();
            column.Width = FontSize * 12;

            var row = table.AddRow();
            row.HeadingFormat = true;
            row.Format.SpaceBefore = 2;
            row.Format.SpaceAfter = 2;
            row.Format.Font.Bold = true;
            row.Format.Alignment = ParagraphAlignment.Center;
            row.VerticalAlignment = VerticalAlignment.Center;
            row.Shading.Color = Colors.LightGray;

            var offset = ExtractImagesFromDatabase ? 1 : 0;

            row[0].AddParagraph("Время");
            row[1].AddParagraph("Фото");
            if (ExtractImagesFromDatabase) row[2].AddParagraph("Фото в базе");
            row[offset + 2].AddParagraph("ФИО, Уверенность");
            row[offset + 3].AddParagraph("Доп. информация");
            row[offset + 4].AddParagraph("Группы");
            row[offset + 5].AddParagraph("Камера");
            row[offset + 6].AddParagraph("Пол");
            row[offset + 7].AddParagraph("Возраст");
            row[offset + 8].AddParagraph("Эмоции");

            foreach (var macroscopEvent in Events)
            {
                if (macroscopEvent == null) continue;
                row = table.AddRow();
                row.HeightRule = RowHeightRule.Exactly;
                row.Height = ImagesWidth * 0.75;

                row[0].AddParagraph($"{macroscopEvent.Timestamp.ToLocalTime()}");
                if (macroscopEvent.TryGetPicture(out var picture, ImagesWidth, 0)) row[1].AddImage("base64:" + picture.Base64);
                if (ExtractImagesFromDatabase) if (macroscopEvent.TryGetPictureFromDatabase(out picture, ImagesWidth, 0)) row[2].AddImage("base64:" + picture.Base64);
                var name = $"{macroscopEvent.FirstName} {macroscopEvent.LastName} {macroscopEvent.Patronymic}".Trim();
                var similarity = macroscopEvent.Similarity > 0 ? $"{string.Format("{0:P0}", macroscopEvent.Similarity).Replace(" ", "")}" : string.Empty;
                name += name != string.Empty ? $", {similarity}" : similarity;
                row[offset + 2].AddParagraph($"{name}");
                row[offset + 3].AddParagraph($"{macroscopEvent.AdditionalInfo}");
                row[offset + 4].AddParagraph($"{string.Join(", ", macroscopEvent.Groups)}");
                row[offset + 5].AddParagraph($"{macroscopEvent.ChannelName}");
                row[offset + 6].AddParagraph($"{StringEnum.GetStringValue(macroscopEvent.Gender)}");
                row[offset + 7].AddParagraph($"{macroscopEvent.Age}");
                row[offset + 8].AddParagraph($"{StringEnum.GetStringValue(macroscopEvent.Emotion)}, {string.Format("{0:P2}", macroscopEvent.EmotionConfidence).Replace(" ", "")}");
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
            using var reader = new StringReader(answer);
            var eventStrings = string.Empty;
            var line = string.Empty;

            var events = new HashSet<Event>();

            while ((line = reader.ReadLine()) != null)
            {
                if (line == "}{")
                {
                    events.Add(new Event(eventStrings + "}"));
                    eventStrings = "{";
                }
                else eventStrings += line;
            }

            events.Add(new Event(eventStrings));
            return events;
        }

        private static void ParseArgs(string[] args)
        {
            if (args.Length == 0) ShowHelp(); // then close the app

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i][1..];
                try
                {
                    if ((args[i].Length == 2 && !args[i].Contains(':')) || args[i].StartsWith("--"))
                    {
                        _ = arg.ToLower() switch
                        {
                            "-server" => (ServerHost = args[i + 1], i++),
                            "-port" => (ServerPort = ushort.Parse(args[i + 1]), i++),
                            "-login" => (ServerLogin = args[i + 1], i++),
                            "-password" => (ServerPassword = args[i + 1], i++),
                            "-starttime" => (StartTime = DateTime.Parse(args[i + 1]), i++),
                            "-endtime" => (EndTime = DateTime.Parse(args[i + 1]), i++),
                            "-channelid" => (ChannelId = args[i + 1], i++),
                            "-output" => (OutputFile = new FileInfo(args[i + 1]), i++),
                            "-imageswidth" => (ImagesWidth = int.Parse(args[i + 1]), i++),
                            "-format" => (PageFormat = Enum.Parse<PageFormat>(args[i + 1]), i++),
                            "-fontsize" => (FontSize = int.Parse(args[i + 1]), i++),
                            "-withdbimages" => ExtractImagesFromDatabase = true,
                            "-help" => ShowHelp(),
                            "?" => ShowHelp(),
                            _ => throw new InvalidOperationException(message: $"Invalid input parameter: \"{args[i]}\". Specify --Help to see more."),
                        };
                    }
                }
                catch (Exception e) 
                {
                    Console.WriteLine($"Invalid value for parameter: -{arg}. {e.Message} Specify --Help to see more.");
                    Environment.Exit(1);
                }
            }
        }

        private static object ShowHelp()
        {
            Console.WriteLine("Usage: Macroscop_FaceRecReport --Server  <address> --Port <string> --Login <string> [--Password <string>]");
            Console.WriteLine("{0,-31}{1}", "", "--StartTime <DateTime> [--EndTime <DateTime>] [--Channelid <string>]");
            Console.WriteLine("{0,-31}{1}", "", "[--Output <string>] [--ImagesWidth <int>] [--FontSize <int>] [--WithDbImages]");
            Console.WriteLine(string.Format(
                "\n {0,-24}{1}\n {2,-24}{3}\n {4,-24}{5}\n {6,-24}{7}\n {8,-24}{9}\n {10,-24}{11}\n {12,-24}{13}\n {14,-24}{15}\n {16,-24}{17}\n {18,-24}{19}\n {20,-24}{21}\n {22,-24}{23}\n {24,-24}{25}\n",
                "--Server  <address>", "Server address.",
                "--Port <string>", "Server port. Default value is 8080. Only ports without encryption are supported.",
                "--Login <string>", "Macroscop login.",
                "--Password <string>", "Macroscop password. Default value is empty string.",
                "--StartTime <DateTime>", "Specify events start local time.",
                "--EndTime <DateTime>", "Specify events end local time. Default value is now.",
                "--Channelid <string>", "Specify channelid to filter output to specific channel.",
                "--Output <string>", "Specify path of .pdf table. By default file will be created in current directory.",
                "--ImagesWidth <int>", $"Specify size of images in .pdf table. Default value is {ImagesWidth}.",
                "--Format <string>", $"Specify pages format. Default value is A4. Possible values: {string.Join(", ", (PageFormat[])Enum.GetValues(typeof(PageFormat)))}",
                "--FontSize <int>", $"Specify font size. Default value is {FontSize}.",
                "--WithDbImages", $"Starts retrieving images for recognized faces from the face database. At least one face recognizing module must be enabled. Images may not be retrieved if the \"external_id\" parameter is not unique or was not set before the generation of the face recognition event.",
                "--Help, -?", "Show this message and exit."));

            Environment.Exit(0);
            return null;
        }
    }
}