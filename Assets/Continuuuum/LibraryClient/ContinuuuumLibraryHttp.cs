using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Continuuuum.Library
{
    public static class ContinuuuumLibraryHttp
    {
        public static string BuildGeocodeUrl(string baseUrl, string address)
        {
            return new Uri(new Uri(baseUrl.TrimEnd('/') + "/"),
                "api/geocode?address=" + Uri.EscapeDataString(address ?? "")).ToString();
        }

        public static string BuildSearchUrl(
            string baseUrl,
            string searchQuery,
            int documentTypeIndex,
            string searchLat,
            string searchLon,
            int distanceIndex)
        {
            var q = new List<string>();
            if (!string.IsNullOrWhiteSpace(searchQuery))
                q.Add("q=" + Uri.EscapeDataString(searchQuery));
            if (documentTypeIndex > 0 && documentTypeIndex < ContinuuuumLibraryQuery.DocumentTypes.Length)
                q.Add("document_type=" + Uri.EscapeDataString(ContinuuuumLibraryQuery.DocumentTypes[documentTypeIndex]));
            if (!string.IsNullOrWhiteSpace(searchLat))
                q.Add("lat=" + Uri.EscapeDataString(searchLat));
            if (!string.IsNullOrWhiteSpace(searchLon))
                q.Add("lon=" + Uri.EscapeDataString(searchLon));
            q.Add("distance_mi=" + ContinuuuumLibraryQuery.DistanceMilesQueryValue(distanceIndex));
            return new Uri(new Uri(baseUrl.TrimEnd('/') + "/"),
                "api/library/search?" + string.Join("&", q)).ToString();
        }

        public static string BuildDownloadUrl(string baseUrl, string tenantId, int documentId)
        {
            string path = "api/library/documents/" + documentId + "/download";
            if (!string.IsNullOrWhiteSpace(tenantId))
                path += "?tenant=" + Uri.EscapeDataString(tenantId.Trim());
            return new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), path).ToString();
        }

        /// <summary>
        /// Synchronous GET for Editor windows. Sends X-Tenant-ID when tenant is set.
        /// Optional authToken is sent as Bearer when non-empty.
        /// </summary>
        public static bool TryGetJsonSync(
            string url,
            string tenantId,
            string authToken,
            out string json,
            out string error)
        {
            json = null;
            error = null;
            try
            {
                using (var client = new HttpClient())
                {
                    if (!string.IsNullOrWhiteSpace(tenantId))
                        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Tenant-ID", tenantId.Trim());
                    if (!string.IsNullOrWhiteSpace(authToken))
                        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + authToken.Trim());

                    var resp = client.GetAsync(url).GetAwaiter().GetResult();
                    json = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (!resp.IsSuccessStatusCode)
                    {
                        error = string.IsNullOrEmpty(json) ? resp.ReasonPhrase : json;
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
