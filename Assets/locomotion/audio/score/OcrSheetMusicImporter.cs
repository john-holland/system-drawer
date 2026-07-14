using System;
using UnityEngine;

namespace Locomotion.Audio
{
    /// <summary>
    /// OCR sheet-music path (feature-flagged). Produces MusicXML-like text for MusicXmlImporter.
    /// </summary>
    public static class OcrSheetMusicImporter
    {
        public static bool EnableOcrSheetMusic;

        public static ScoreDocument ImportFromImage(Texture2D sheetImage, float bpm = 120f, string proxyVoiceId = "ocr-0")
        {
            if (!EnableOcrSheetMusic)
            {
                Debug.LogWarning("[OcrSheetMusicImporter] OCR path disabled (EnableOcrSheetMusic=false).");
                return new ScoreDocument { title = "ocr-disabled", bpm = bpm, events = Array.Empty<ScoreEvent>() };
            }

            // Scaffold: real OCR service/model plugs in here; for now return empty with metadata.
            _ = sheetImage;
            return new ScoreDocument
            {
                title = sheetImage != null ? sheetImage.name : "ocr-sheet",
                bpm = bpm,
                events = Array.Empty<ScoreEvent>(),
                partNames = new[] { proxyVoiceId }
            };
        }

        public static ScoreDocument ImportRecognizedMusicXml(string recognizedXml, float bpm = 120f, string proxyVoiceId = "ocr-0")
        {
            if (!EnableOcrSheetMusic)
                return new ScoreDocument { title = "ocr-disabled", bpm = bpm, events = Array.Empty<ScoreEvent>() };
            return MusicXmlImporter.ImportXml(recognizedXml, bpm, proxyVoiceId);
        }
    }
}
