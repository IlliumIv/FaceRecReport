using Macroscop_FaceRecReport.ConfigurationEntities;
using Macroscop_FaceRecReport.Enums;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Macroscop_FaceRecReport
{
    internal static class Program
    {
        static string ServerHost = string.Empty;
        static ushort ServerPort = 8080;
        static string ServerLogin = string.Empty;
        static string ServerPassword = "";
        static DateTime StartTime;
        static DateTime EndTime;
        static string ChannelId = string.Empty;
        static FileInfo? OutputFile;
        static int ImagesWidth = 100;
        static int FontSize = 8;
        static PageFormat PageFormat = PageFormat.A4;

        public static HashSet<Event> Events = new();

        public static void Main(string[] args)
        {
            ParseArgs(args);
            if (ServerHost == string.Empty || ServerLogin == string.Empty) return;

            try
            {
                var macroscopClient = new HttpClient();
                macroscopClient.BaseAddress = new Uri($"http://{ServerHost}:{ServerPort}");
                var request = $"specialarchiveevents?startTime={StartTime}&endTime={EndTime}&eventId=427f1cc3-2c2f-4f50-8865-56ae99c3610d";
                request += ChannelId == string.Empty ? "" : $"&channelid={ChannelId}";
                var message = new HttpRequestMessage(HttpMethod.Get, request);
                var auth = $"{ServerLogin}:{CreateMD5(ServerPassword)}";
                message.Headers.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes(auth))}");
                var response = macroscopClient.Send(message);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var answer = response.Content.ReadAsStringAsync().Result;
                    if (answer == string.Empty) Console.WriteLine($"Empty answer for request:\n{response.RequestMessage}");
                    else ParseAnswer(answer);
                    ProceedEvents();
                }
            }
            catch (HttpRequestException e) when (e.InnerException is IOException)
            {
                Console.WriteLine($"{e.Message} {e.InnerException.Message}");
            }
        }

        private static void ProceedEvents()
        {
            if (Events.Count == 0) return;

            var document = new Document();
            document.Info.Author = "IlliumIv";
            document.CreateDocument();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var pdf = new PdfDocumentRenderer(true);
            pdf.Document = document;
            pdf.RenderDocument();
            var filePath = OutputFile != null? OutputFile.FullName : $"Отчёт распознавания лиц {StartTime:dd.MM.yyyy HH.mm.ss} - {EndTime:dd.MM.yyyy HH.mm.ss}.pdf";
            try { pdf.Save(filePath); }
            catch (Exception e) { Console.WriteLine(e.Message); }
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
            paragraph.AddText($"Период: {StartTime:dd.MM.yyyy HH.mm.ss} - {EndTime:dd.MM.yyyy HH.mm.ss}");
            paragraph.Format.Alignment = ParagraphAlignment.Center;
            paragraph.Format.Font.Size = FontSize;
            paragraph.Format.SpaceBefore = 6;
            paragraph.Format.SpaceAfter = 6;

            var table = section.AddTable();
            table.Borders.Color = Colors.Black;
            table.Borders.Width = 0.25;
            table.Format.Font.Size = FontSize;
            table.LeftPadding = 0;
            table.RightPadding = 0;
            table.Rows.Alignment = RowAlignment.Center;

            var columns = new string[9]
            {
                "Время",
                "Фото",
                "ФИО",
                "Доп. информация",
                "Группы",
                "Камера",
                "Пол",
                "Возраст",
                "Эмоции",
            };

            for (int i = 0; i < columns.Length; i++) table.AddColumn();

            table.Columns[0].Width = 50;
            table.Columns[1].Width = ImagesWidth * 0.75;
            table.Columns[7].Width = 50;

            var row = table.AddRow();
            row.HeadingFormat = true;
            row.Format.SpaceBefore = 2;
            row.Format.SpaceAfter = 2;
            row.Format.Font.Bold = true;
            row.Format.Alignment = ParagraphAlignment.Center;
            row.VerticalAlignment = VerticalAlignment.Center;
            row.Shading.Color = Colors.LightGray;
            for (int i = 0; i < columns.Length; i++) row[i].AddParagraph(columns[i]);

            var textFrame = new TextFrame { MarginLeft = 3, MarginRight = 3 };
            paragraph = textFrame.AddParagraph();
            var text = new Text();
            paragraph.Add(text);
            paragraph.Format.Font.Size = FontSize;
            paragraph.Format.SpaceBefore = 2;
            paragraph.Format.SpaceAfter = 2;

            foreach (var macroscopEvent in Events)
            {
                if (macroscopEvent == null) continue;
                row = table.AddRow();
                row.HeightRule = RowHeightRule.Exactly;
                row.Height = ImagesWidth * 0.75;

                text.Content = $"{macroscopEvent.Timestamp.ToLocalTime()}";
                row[0].Add(textFrame.Clone());
                var imageWithFormat = macroscopEvent.GetImageWithFormat(ImagesWidth, 0);
                string imageFileName = "base64:" + imageWithFormat.Image.ToBase64String(imageWithFormat.Format).Split(',')[1];
                row[1].AddImage(imageFileName);
                var similarity = macroscopEvent.Similarity > 0 ? $", {string.Format("{0:P0}", macroscopEvent.Similarity).Replace(" ", "")}" : "";
                text.Content = $"{macroscopEvent.Patronymic} {macroscopEvent.LastName} {macroscopEvent.LastName}{similarity}";
                row[2].Add(textFrame.Clone());
                text.Content = $"{macroscopEvent.AdditionalInfo}";
                row[3].Add(textFrame.Clone());
                text.Content = $"{string.Join(", ", macroscopEvent.Groups)}";
                row[4].Add(textFrame.Clone());
                text.Content = $"{macroscopEvent.ChannelName}";
                row[5].Add(textFrame.Clone());
                text.Content = $"{StringEnum.GetStringValue(macroscopEvent.Gender)}";
                row[6].Add(textFrame.Clone());
                text.Content = $"{macroscopEvent.Age}";
                row[7].Add(textFrame.Clone());
                text.Content = $"{StringEnum.GetStringValue(macroscopEvent.Emotion)}, {string.Format("{0:P2}", macroscopEvent.EmotionConfidence).Replace(" ", "")}";
                row[8].Add(textFrame.Clone());
            }
        }

        private static void ParseAnswer(string answer)
        {
            using var reader = new StringReader(answer);
            var eventStrings = string.Empty;
            var line = string.Empty;

            var jsonSerializerSettings = new JsonSerializerSettings();
            jsonSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
            while ((line = reader.ReadLine()) != null)
            {
                if (line == "}{")
                {
                    var macroscopEvent = new Event(eventStrings + "}");
                    if (macroscopEvent != null) Events.Add(macroscopEvent);
                    eventStrings = "{";
                }
                else eventStrings += line;
            }
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
                            "?" => ShowHelp(),
                            _ => throw new InvalidOperationException(message: $"Invalid input parameter: \"{args[i]}\"."),
                        };
                    }
                }
                catch (Exception e) when (
                    e is InvalidOperationException
                    || e is ArgumentException
                ) {
                    Console.WriteLine(e.Message);
                    Environment.Exit(1);
                }
                catch (IndexOutOfRangeException)
                {
                    Console.WriteLine($"Invalid value for parameter: {arg}.");
                    Environment.Exit(1);
                }
            }
        }

        private static object ShowHelp()
        {
            Console.WriteLine("Usage: Macroscop_FaceRecReport --Server  <address> --Port <string> --Login <string> [--Password <string>]");
            Console.WriteLine("{0,-31}{1}", "", "--StartTime <DateTime> --EndTime <DateTime> [--Channelid <string>]");
            Console.WriteLine("{0,-31}{1}", "", "[--Output <string>] [--ImagesWidth <int>]");
            Console.WriteLine(string.Format(
                "\n {0,-24}{1}\n {2,-24}{3}\n {4,-24}{5}\n {6,-24}{7}\n {8,-24}{9}\n {10,-24}{11}\n {12,-24}{13}\n {14,-24}{15}\n {16,-24}{17}\n {18,-24}{19}\n {20,-24}{21}\n",
                "--Server  <address>", "Server address.",
                "--Port <string>", "Server port. Default value is 8080. Only ports without encryption are supported.",
                "--Login <string>", "Login.",
                "--Password <string>", "Password. Default value is empty string.",
                "--StartTime <DateTime>", "Specify events start time.",
                "--EndTime <DateTime>", "Specify events end time.",
                "--Channelid <string>", "Specify channelid to filter output to specific channel.",
                "--Output <string>", "Specify path of .pdf table. By default file will create in current directory.",
                "--ImagesWidth <int>", "Specify size of images in .pdf table. Default value is 100.",
                "--Format <string>", $"Specify pages format. Default value is A4. Possible values: {string.Join(", ", (PageFormat[])Enum.GetValues(typeof(PageFormat)))}",
                "-?", "Show this message and exit."));

            Environment.Exit(0);
            return null;
        }

        private static string CreateMD5(string input)
        {
            using var md5 = MD5.Create();
            var inputBytes = Encoding.ASCII.GetBytes(input);
            var hashBytes = md5.ComputeHash(inputBytes);
            var sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++) sb.Append(hashBytes[i].ToString("X2", CultureInfo.CurrentCulture));
            return sb.ToString();
        }
    }
}