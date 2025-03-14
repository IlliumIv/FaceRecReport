using System.Text;
using Newtonsoft.Json;
using SixLabors.ImageSharp.Processing;
using FaceRecReport.Entities;

namespace FaceRecReport.Helpers;

public static class ImageProvider
{
    private static readonly Dictionary<string, Picture?> _dbImagesCache = new() { { "", null } };

    public static bool TryGetPictureFromDatabase(this Event @event, out Picture? picture)
    {
        picture = null;

        if (!@event.IsIdentified)
            return false;
        if (@event.ExternalId == string.Empty)
            return false;
        if (_dbImagesCache.TryGetValue(@event.ExternalId ?? string.Empty, out picture))
            return picture != null;

        if (TryFindUniquePersonInDatabase(@event, out var person))
        {
            var message = new HttpRequestMessage(HttpMethod.Get, $"api/faces/{person?.Id}?module={person?.Module}&onlymainsample=true");

            if (Connection.SendRequest(message, out var response))
            {
                var answer = response.Content.ReadAsStringAsync().Result;
                if (answer != string.Empty)
                {
                    var face = JsonConvert.DeserializeObject<dynamic>(answer);
                    if (face != null)
                    {
                        byte[] imageBytes = Convert.FromBase64String(face["face_images"].ToObject<string[]>()[0]);
                        picture = new Picture(imageBytes);
                    }
                }
            }
        }

        _dbImagesCache.Add(@event.ExternalId ?? string.Empty, picture);
        return picture != null;
    }

    public static bool TryGetPictureFromDatabase(this Event @event, out Picture? picture, int width, int height)
    {
        @event.TryGetPictureFromDatabase(out picture);

        width = picture?.Image.Size.Width >= picture?.Image.Size.Height ? width : 0;
        height = picture?.Image.Size.Height >= picture?.Image.Size.Width ? height : 0;

        picture?.Image.Mutate(i => i.Resize(width, height));
        return picture != null;
    }

    public static bool TryGetPicture(this Event @event, out Picture? picture)
    {
        picture = null;

        if (@event.ImageBytes != string.Empty)
        {
            var imageBytes = Convert.FromBase64String(@event.ImageBytes);
            picture = new Picture(imageBytes);
        }

        return picture != null;
    }

    public static bool TryGetPicture(this Event enevt, out Picture? picture, int width, int height)
    {
        enevt.TryGetPicture(out picture);

        width = picture?.Image.Size.Width >= picture?.Image.Size.Height ? width : 0;
        height = picture?.Image.Size.Height >= picture?.Image.Size.Width ? height : 0;

        picture?.Image.Mutate(i => i.Resize(width, height));
        return picture != null;
    }

    private static bool TryFindUniquePersonInDatabase(Event @event, out Person? person)
    {
        person = null;

        var message = new HttpRequestMessage(HttpMethod.Get, $"api/faceconfig");

        if (Connection.SendRequest(message, out var response))
        {
            var answer = response.Content.ReadAsStringAsync().Result;
            if (answer != string.Empty)
            {
                var modules = JsonConvert.DeserializeObject<dynamic>(answer)?["faces_modules"];

                if (modules != null)
                {
                    foreach (var module in modules)
                    {
                        if (!(bool)module["enabled"] || (string)module["name"] == "visitors")
                            continue;

                        message = new HttpRequestMessage(HttpMethod.Get, $"api/faces?module={module["name"]}&filter=external_id='{@event.ExternalId}'");

                        if (!Connection.SendRequest(message, out response))
                            continue;

                        answer = response.Content.ReadAsStringAsync().Result;
                        if (answer == string.Empty)
                            continue;

                        var faces = JsonConvert.DeserializeObject<dynamic>(answer);
                        if (faces == null)
                            continue;

                        if ((int)faces["total_count"] > 1 || (((int)faces["total_count"] > 0) && person != null))
                        {
                            var filter = $"external_id='{@event.ExternalId}'" +
                                $" AND first_name='{@event.FirstName}'" +
                                $" AND last_name='{@event.LastName}'" +
                                $" AND patronymic='{@event.Patronymic}'" +
                                $" AND required_ration='100'";

                            message = new HttpRequestMessage(HttpMethod.Get, $"api/faces?module={module["name"]}&filter={filter}");

                            if (!Connection.SendRequest(message, out response))
                                continue;

                            answer = response.Content.ReadAsStringAsync().Result;
                            if (answer == string.Empty)
                                continue;

                            faces = JsonConvert.DeserializeObject<dynamic>(answer);
                            if (faces == null)
                                continue;

                            if ((int)faces["total_count"] > 1 || (((int)faces["total_count"] > 0) && person != null))
                            {
                                person = null;
                                break;
                            }
                        }

                        person = new((string)faces["faces"][0]["id"], (string)module["name"]);
                    }
                }
            }
        }

        return person != null;
    }
}
