using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Continuuuum.Library
{
    public static class ContinuuuumLibraryJson
    {
        public static string TryGetDisplayTitle(ContinuuuumLibraryDocument doc)
        {
            if (doc == null || string.IsNullOrEmpty(doc.type_metadata))
                return null;
            try
            {
                var jo = JObject.Parse(doc.type_metadata);
                var title = jo["title"]?.ToString() ?? jo["author"]?.ToString();
                return string.IsNullOrEmpty(title) ? null : title;
            }
            catch
            {
                return null;
            }
        }

        public static bool TryParseGeocode(string json, out string latStr, out string lonStr)
        {
            latStr = null;
            lonStr = null;
            if (string.IsNullOrWhiteSpace(json))
                return false;
            try
            {
                var jo = JObject.Parse(json);
                var latTok = jo["lat"];
                var lonTok = jo["lon"];
                if (latTok != null && latTok.Type != JTokenType.Null)
                    latStr = latTok.ToString();
                if (lonTok != null && lonTok.Type != JTokenType.Null)
                    lonStr = lonTok.ToString();
                return !string.IsNullOrEmpty(latStr) || !string.IsNullOrEmpty(lonStr);
            }
            catch
            {
                return false;
            }
        }

        public static List<ContinuuuumLibraryDocument> ParseSearchResults(string json)
        {
            var results = new List<ContinuuuumLibraryDocument>();
            if (string.IsNullOrWhiteSpace(json))
                return results;
            try
            {
                var arr = JArray.Parse(json);
                foreach (var tok in arr)
                {
                    var obj = tok as JObject;
                    if (obj == null) continue;
                    var doc = new ContinuuuumLibraryDocument
                    {
                        id = obj["id"] != null ? (int)obj["id"] : 0,
                        document_type = obj["document_type"]?.ToString() ?? "",
                        url = obj["url"]?.ToString(),
                        type_metadata = obj["type_metadata"]?.ToString(),
                        blob_ref = obj["blob_ref"]?.ToString(),
                    };
                    if (obj["lat"] != null && obj["lat"].Type != JTokenType.Null && obj["lat"].Type != JTokenType.Undefined)
                        doc.lat = (double)obj["lat"];
                    if (obj["lon"] != null && obj["lon"].Type != JTokenType.Null && obj["lon"].Type != JTokenType.Undefined)
                        doc.lon = (double)obj["lon"];
                    results.Add(doc);
                }
            }
            catch (Exception)
            {
                // Caller surfaces empty/partial results via UI error separately when needed.
            }
            return results;
        }
    }
}
