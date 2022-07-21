using System.Text;
using Newtonsoft.Json;
using SixLabors.ImageSharp.Processing;
using Macroscop_FaceRecReport.Entities;

namespace Macroscop_FaceRecReport.Helpers
{
    public static class ImageProvider
    {
        private static readonly Dictionary<string, Picture?> _dbImagesCache = new() { { "", null } };

        public static bool TryGetPictureFromDatabase(this Event macroscopEvent, out Picture? picture)
        {
            picture = null;

            if (!macroscopEvent.IsIdentified) return false;
            if (macroscopEvent.ExternalId == string.Empty) return false;
            if (_dbImagesCache.TryGetValue(macroscopEvent.ExternalId, out picture)) return picture != null;

            if (TryFindUniquePersonInDatabase(macroscopEvent, out var person))
            {
                var message = new HttpRequestMessage(HttpMethod.Get, $"api/faces/{person.Id}?module={person.Module}&onlymainsample=true");
                message.Headers.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes(Program.AuthString))}");

                var response = Program.MacroscopHttpClient.Send(message);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var answer = response.Content.ReadAsStringAsync().Result;
                    if (answer != string.Empty)
                    {
                        var face = JsonConvert.DeserializeObject<dynamic>(answer);
                        if (face != null)
                        {
                            byte[] imageBytes = Convert.FromBase64String((face.face_images).ToObject<string[]>()[0]);
                            picture = new Picture(imageBytes);
                        }
                    }
                }

            }

            _dbImagesCache.Add(macroscopEvent.ExternalId, picture);
            return picture != null;
        }

        public static bool TryGetPictureFromDatabase(this Event macroscopEvent, out Picture? picture, int width, int height)
        {
            macroscopEvent.TryGetPictureFromDatabase(out picture);
            if (picture != null) picture.Image.Mutate(i => i.Resize(width, height));
            return picture != null;
        }

        public static bool TryGetPicture(this Event macroscopEvent, out Picture? picture)
        {
            picture = null;

            if (macroscopEvent.ImageBytes != string.Empty)
            {
                byte[] imageBytes = Convert.FromBase64String(macroscopEvent.ImageBytes);
                picture = new Picture(imageBytes);
            }

            return picture != null;
        }

        public static bool TryGetPicture(this Event macroscopEvent, out Picture? picture, int width, int height)
        {
            macroscopEvent.TryGetPicture(out picture);
            if (picture != null) picture.Image.Mutate(i => i.Resize(width, height));
            return picture != null;
        }

        private static bool TryFindUniquePersonInDatabase(Event macroscopEvent, out Person? person)
        {
            person = null;

            var message = new HttpRequestMessage(HttpMethod.Get, $"api/faceconfig");
            message.Headers.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes(Program.AuthString))}");

            var response = Program.MacroscopHttpClient.Send(message);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var answer = response.Content.ReadAsStringAsync().Result;
                if (answer != string.Empty)
                {
                    var modules = JsonConvert.DeserializeObject<dynamic>(answer);
                    if (modules != null)
                    {
                        foreach (dynamic module in modules.faces_modules)
                        {
                            if (!(bool)module.enabled) continue;

                            message = new HttpRequestMessage(HttpMethod.Get, $"api/faces?module={module.name}&filter=external_id='{macroscopEvent.ExternalId}'");
                            message.Headers.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes(Program.AuthString))}");

                            response = Program.MacroscopHttpClient.Send(message);
                            if (response.StatusCode != System.Net.HttpStatusCode.OK) continue;

                            answer = response.Content.ReadAsStringAsync().Result;
                            if (answer == string.Empty) continue;

                            var faces = JsonConvert.DeserializeObject<dynamic>(answer);
                            if (faces == null) continue;

                            if (faces.total_count > 1 || (faces.total_count > 0 && person != null))
                            {
                                var filter = $"external_id='{macroscopEvent.ExternalId}'" +
                                    $" AND first_name='{macroscopEvent.FirstName}'" +
                                    $" AND last_name='{macroscopEvent.LastName}'" +
                                    $" AND patronymic='{macroscopEvent.Patronymic}'" +
                                    $" AND required_ration='100'";
                                foreach (var groupId in macroscopEvent.Groups)
                                    filter += $" AND group_id='{groupId}'";

                                message = new HttpRequestMessage(HttpMethod.Get, $"api/faces?module={module.name}&filter={filter}");
                                message.Headers.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes(Program.AuthString))}");

                                response = Program.MacroscopHttpClient.Send(message);
                                if (response.StatusCode != System.Net.HttpStatusCode.OK) continue;

                                answer = response.Content.ReadAsStringAsync().Result;
                                if (answer == string.Empty) continue;

                                faces = JsonConvert.DeserializeObject<dynamic>(answer);
                                if (faces == null) continue;

                                if (faces.total_count > 1 || (faces.total_count > 0 && person != null))
                                {
                                    person = null;
                                    break;
                                }
                            }

                            person = new((string)faces.faces[0].id, (string)module.name);
                        }
                    }
                }
            }

            return person != null;
        }
    }
}
