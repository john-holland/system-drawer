using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Locomotion.Narrative;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Export/import 4D spatial expressions to flat file (JSON, YAML, or XML).
/// Append = read existing if exists, merge entries, write. Overwrite = write fresh.
/// YAML uses same serialization as JSON when YamlDotNet is not available in this assembly.
/// </summary>
public static class Spatial4DExportUtility
{
    private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    public static string ExportToJson(Spatial4DExpressionsDto dto)
    {
        return JsonConvert.SerializeObject(dto, JsonSettings);
    }

    public static Spatial4DExpressionsDto ImportFromJson(string json)
    {
        return JsonConvert.DeserializeObject<Spatial4DExpressionsDto>(json, JsonSettings);
    }

    /// <summary>Export to YAML. When YamlDotNet is not referenced, returns JSON instead.</summary>
    public static string ExportToYaml(Spatial4DExpressionsDto dto)
    {
        return ExportToJson(dto);
    }

    public static Spatial4DExpressionsDto ImportFromYaml(string yaml)
    {
        return ImportFromJson(yaml);
    }

    private static readonly XmlSerializer XmlSer = new XmlSerializer(typeof(Spatial4DExpressionsDto));

    public static string ExportToXml(Spatial4DExpressionsDto dto)
    {
        using (var sw = new StringWriter())
        {
            XmlSer.Serialize(sw, dto);
            return sw.ToString();
        }
    }

    public static Spatial4DExpressionsDto ImportFromXml(string xml)
    {
        using (var sr = new StringReader(xml))
        {
            return (Spatial4DExpressionsDto)XmlSer.Deserialize(sr);
        }
    }

    public enum OutputFormat { Json, Yaml, Xml }

    public static OutputFormat FromSpatial4DOutputFormat(Spatial4DOutputFormat f)
    {
        switch (f)
        {
            case Spatial4DOutputFormat.Json: return OutputFormat.Json;
            case Spatial4DOutputFormat.Yaml: return OutputFormat.Yaml;
            case Spatial4DOutputFormat.Xml: return OutputFormat.Xml;
            default: return OutputFormat.Json;
        }
    }

    public static string Serialize(Spatial4DExpressionsDto dto, OutputFormat format)
    {
        switch (format)
        {
            case OutputFormat.Json: return ExportToJson(dto);
            case OutputFormat.Yaml: return ExportToYaml(dto);
            case OutputFormat.Xml: return ExportToXml(dto);
            default: return ExportToJson(dto);
        }
    }

    public static Spatial4DExpressionsDto Deserialize(string content, OutputFormat format)
    {
        switch (format)
        {
            case OutputFormat.Json: return ImportFromJson(content);
            case OutputFormat.Yaml: return ImportFromYaml(content);
            case OutputFormat.Xml: return ImportFromXml(content);
            default: return ImportFromJson(content);
        }
    }

    /// <summary>Resolve path: if relative, resolve against persistentDataPath at runtime.</summary>
    public static string ResolvePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        if (Path.IsPathRooted(path)) return path;
        return Path.Combine(Application.persistentDataPath, path);
    }

    /// <summary>Write dto to file (overwrite).</summary>
    public static void WriteToFile(string path, Spatial4DExpressionsDto dto, OutputFormat format)
    {
        path = ResolvePath(path);
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        string content = Serialize(dto, format);
        File.WriteAllText(path, content);
    }

    /// <summary>Append new entries to file: read existing, merge entries, write.</summary>
    public static void AppendToFile(string path, List<Spatial4DExpressionEntryDto> newEntries, OutputFormat format)
    {
        path = ResolvePath(path);
        Spatial4DExpressionsDto existing = new Spatial4DExpressionsDto();
        if (File.Exists(path))
        {
            try
            {
                string content = File.ReadAllText(path);
                existing = Deserialize(content, format);
                if (existing?.entries == null)
                    existing = new Spatial4DExpressionsDto();
            }
            catch
            {
                existing = new Spatial4DExpressionsDto();
            }
        }
        if (existing.entries == null)
            existing.entries = new List<Spatial4DExpressionEntryDto>();
        foreach (var e in newEntries)
            existing.entries.Add(e);
        WriteToFile(path, existing, format);
    }
}
