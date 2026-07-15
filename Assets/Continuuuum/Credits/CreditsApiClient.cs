using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Continuuuum.Credits
{
    public sealed class CreditsApiClient : MonoBehaviour
    {
        public string listId;

        string Base => ContinuuuumApiConfig.GetApiBaseUrl().TrimEnd('/');

        public async Task<CreditsListDto> GetListAsync(string id, bool includeHidden = false)
        {
            string q = includeHidden ? "?includeHidden=1" : "";
            using var req = UnityWebRequest.Get($"{Base}/api/credits/lists/{UnityWebRequest.EscapeURL(id)}{q}");
            await Send(req);
            return JsonConvert.DeserializeObject<CreditsListDto>(req.downloadHandler.text);
        }

        public async Task<CreditsListDto> UpdateListAsync(string id, string mode, string episodeId)
        {
            var payload = new
            {
                mode = mode ?? "work_orders",
                episodeId = string.IsNullOrEmpty(episodeId) ? null : episodeId,
                source = "unity",
            };
            string body = JsonConvert.SerializeObject(payload);
            using var req = new UnityWebRequest($"{Base}/api/credits/lists/{UnityWebRequest.EscapeURL(id)}/update-list", "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            await Send(req);
            return JsonConvert.DeserializeObject<CreditsListDto>(req.downloadHandler.text);
        }

        static Task Send(UnityWebRequest req)
        {
            var op = req.SendWebRequest();
            var tcs = new TaskCompletionSource<bool>();
            op.completed += _ =>
            {
                if (req.result != UnityWebRequest.Result.Success)
                    tcs.TrySetException(new Exception(req.error + " " + req.downloadHandler?.text));
                else
                    tcs.TrySetResult(true);
            };
            return tcs.Task;
        }
    }
}
