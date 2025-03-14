using CLIArgsHandler;
using MigraDoc.DocumentObjectModel;
using System.Globalization;

namespace FaceRecReport;

public class Parameters
{
    public static Parameter ShowHelpMessage { get; } =
        new(prefixes: ["--Help", "--help", "-h", "-?"],
            format: string.Empty,
            descriptionFormatter: () => "Show this message and exit.",
            sortingOrder: 11,
            parser: (args, i) =>
            {
                ArgumentsHandler<Parameters>.ShowHelp();
                Environment.Exit(0);
                return args.RemoveAt(i, 1);
            });

    public static Parameter<string> ServerHost { get; } =
        new(prefixes: ["--Server", "--server"],
            value: "127.0.0.1",
            format: "url",
            descriptionFormatter: () => $"Server address. Current value is \"{ServerHost?.Value}\".",
            parser: (args, i) =>
            {
                ServerHost!.Value = args[i + 1];
                return args.RemoveAt(i, 2);
            });

    public static Parameter<ushort> Port { get; } =
        new(prefixes: ["--Port", "--port"],
            value: 8080,
            format: "number",
            descriptionFormatter: () => $"Server port. Current value is \"{Port?.Value}\".",
            parser: (args, i) =>
            {
                Port!.Value = ushort.Parse(args[i + 1]);
                return args.RemoveAt(i, 2);
            });

    public static Parameter<bool> UseSSL { get; } =
        new(prefixes: ["--SSL", "--ssl"],
            value: false,
            format: string.Empty,
            descriptionFormatter: () => $"Connect over HTTPS. Current value is \"{UseSSL?.Value}\".",
            parser: (args, i) =>
            {
                UseSSL!.Value = true;
                return args.RemoveAt(i, 1);
            });

    public static Parameter<bool> IsActiveDirectoryUser { get; } =
        new(prefixes: ["--Active-Directory", "--active-directory", "--ad"],
            value: false,
            format: string.Empty,
            descriptionFormatter: () => $"Specify that is Active Directory user. Current value is \"{IsActiveDirectoryUser?.Value}\".",
            sortingOrder: 2,
            parser: (args, i) =>
            {
                IsActiveDirectoryUser!.Value = true;
                return args.RemoveAt(i, 1);
            });

    public static Parameter<string> Login { get; } =
        new(prefixes: ["--Login", "--login"],
            value: "root",
            format: "string",
            descriptionFormatter: () => $"Login. Current value is \"{Login?.Value}\". " +
                $"Must specify {string.Join(" or ", IsActiveDirectoryUser.Prefixes)} if using a Active Directory user.",
            parser: (args, i) =>
            {
                Login!.Value = args[i + 1];
                return args.RemoveAt(i, 2);
            });

    public static Parameter<string> Password { get; } =
        new(prefixes: ["--Password", "--password"],
            value: string.Empty,
            format: "string",
            descriptionFormatter: () => $"Password. Current value is \"{Password?.Value}\".",
            sortingOrder: 3,
            parser: (args, i) =>
            {
                Password!.Value = args[i + 1];
                return args.RemoveAt(i, 2);
            });

    public static Parameter<DateTime> StartTime { get; } =
        new(prefixes: ["--StartTime", "--starttime"],
            value: DateTime.MinValue,
            format: "string",
            descriptionFormatter: () => $"Specify events start local time. Default value is {StartTime?.Value}.",
            sortingOrder: 9,
            parser: (args, i) =>
            {
                StartTime!.Value = DateTime.Parse(args[i + 1]);
                return args.RemoveAt(i, 2);
            });

    public static Parameter<DateTime> EndTime { get; } =
        new(prefixes: ["--EndTime", "--endtime"],
            value: DateTime.Now,
            format: "string",
            descriptionFormatter: () => $"Specify events end local time. Default value is {EndTime?.Value}.",
            sortingOrder: 9,
            parser: (args, i) =>
            {
                EndTime!.Value = DateTime.Parse(args[i + 1]);
                return args.RemoveAt(i, 2);
            });

    public static Parameter<string> ChannelId { get; } =
        new(prefixes: ["--ChannelId", "--channelid"],
            value: string.Empty,
            format: "string",
            descriptionFormatter: () => "Specify channelid to filter output to specific channel.",
            sortingOrder: 3,
            parser: (args, i) =>
            {
                ChannelId!.Value = args[i + 1];
                return args.RemoveAt(i, 2);
            });

    public static Parameter<string> EventId { get; } =
        new(prefixes: ["--EventId", "--eventlid"],
            value: "427f1cc3-2c2f-4f50-8865-56ae99c3610d",
            format: "string",
            descriptionFormatter: () => $"Specify eventlid of face detection event. Default value is {EventId?.Value}.",
            sortingOrder: 3,
            parser: (args, i) =>
            {
                EventId!.Value = args[i + 1];
                return args.RemoveAt(i, 2);
            });

    public static Parameter<string> OutputFile { get; } =
        new(prefixes: ["--Output", "--output"],
            value: string.Empty,
            format: "string",
            descriptionFormatter: () => $"Specify path of .pdf table. By default file will be created in current directory: {Environment.CurrentDirectory}.",
            sortingOrder: 3,
            parser: (args, i) =>
            {
                OutputFile!.Value = args[i + 1];
                return args.RemoveAt(i, 2);
            });

    public static Parameter<int> ImagesWidth { get; } =
        new(prefixes: ["--Images-Width", "--images-width"],
            value: 100,
            format: "int",
            descriptionFormatter: () => $"Specify size of images in .pdf table. Default value is {ImagesWidth?.Value}.",
            sortingOrder: 3,
            parser: (args, i) =>
            {
                ImagesWidth!.Value = int.Parse(args[i + 1]);
                return args.RemoveAt(i, 2);
            });

    public static Parameter<PageFormat> PageFormat { get; } =
        new(prefixes: ["--Format", "--format"],
            value: MigraDoc.DocumentObjectModel.PageFormat.A4,
            format: "int",
            descriptionFormatter: () => $"Specify pages format. Default value is {MigraDoc.DocumentObjectModel.PageFormat.A4}. Possible values: {string.Join(", ", (PageFormat[])Enum.GetValues(typeof(PageFormat)))}",
            sortingOrder: 3,
            parser: (args, i) =>
            {
                PageFormat!.Value = Enum.Parse<PageFormat>(args[i + 1]);
                return args.RemoveAt(i, 2);
            });

    public static Parameter<int> FontSize { get; } =
        new(prefixes: ["--FontSize", "--fontsize"],
            value: 8,
            format: "int",
            descriptionFormatter: () => $"Specify font size. Default value is {FontSize?.Value}.",
            sortingOrder: 3,
            parser: (args, i) =>
            {
                FontSize!.Value = int.Parse(args[i + 1]);
                return args.RemoveAt(i, 2);
            });

    public static Parameter<bool> ExtractImagesFromDatabase { get; } =
        new(prefixes: ["--WithDbImages", "--withdbimages"],
            value: false,
            format: "",
            descriptionFormatter: () => $"Starts retrieving images for recognized faces from the face database. At least one face recognizing module must be enabled. Images may not be retrieved if the \"external_id\" parameter is not unique or was not set before the generation of the face recognition event.",
            sortingOrder: 3,
            parser: (args, i) =>
            {
                ExtractImagesFromDatabase!.Value = true;
                return args.RemoveAt(i, 1);
            });

    public static Parameter<bool> SaveImages { get; } =
        new(prefixes: ["--SaveImages", "--saveimages"],
            value: false,
            format: "",
            descriptionFormatter: () => "Starts saving events images in output folder. Warning! It will rewrite images if they already exist.",
            sortingOrder: 3,
            parser: (args, i) =>
            {
                SaveImages!.Value = true;
                return args.RemoveAt(i, 1);
            });
}
