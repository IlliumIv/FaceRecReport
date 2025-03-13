Tool to export face recognition report with images from Macroscop and Eocortex servers.
Work with 4.3.87+ versions.

Usage: `.\Macroscop_FaceRecReport.exe`

`--server <url>` - Server address. Default value is 127.0.0.1.\
`--port <number>` - Server port. Default value is 8080.\
`--ssl` - Connect over HTTPS.\
`--login <string>` - Login. Default value is "root". Must specify `--ad` if using a Active Directory user.\
`--active-directory, --ad` - Specify that is Active Directory user.\
`--password <string>` - Password. Default value is empty string.\
`--channelid <string>` - Specify channelid to filter output to specific channel.\
`--eventlid <string>` - Specify eventlid of face detection event. Default value is 427f1cc3-2c2f-4f50-8865-56ae99c3610d.\
`--output <string>` - Specify path of .pdf table. By default file will be created in current directory.\
`--images-width <int>` - Specify size of images in .pdf table. Default value is 100.\
`--format <int>` - Specify pages format. Default value is A4.\
`--fontsize <int>` - Specify font size. Default value is 8.\
`--withdbimages` - Starts retrieving images for recognized faces from the face database. At least one face recognizing module must be enabled. Images may not be retrieved if the "external_id" parameter is not unique or was not set before the generation of the face recognition event.\
`--saveimages` - Starts saving events images in output folder. Warning! It will rewrite images if they already exist.\
`--starttime <string>` - Specify events start local time.\
`--endtime <string>` - Specify events end local time.\
`--help, -h, -?` - Show this message and exit.

---

Утилита экспорта отчёта о распознанных лицах с изображениями с серверов Macroscop и Eocortex. Работает с версиями 4.3.87+.