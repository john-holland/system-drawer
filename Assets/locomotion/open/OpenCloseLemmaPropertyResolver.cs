using System;
using System.Collections.Generic;
using Locomotion.Narrative;

using Locomotion.Drink;

namespace Locomotion.Open
{
    public static class OpenCloseLemmaPropertyResolver
    {
        public static OpenCloseLemmaProperties Resolve(
            IReadOnlyList<PromptSegment> promptSegments = null,
            IReadOnlyList<LocalizationClauseBindingRecord> clauseBindings = null,
            IReadOnlyList<ThesaurusEntryPropertyRecord> lemmaProperties = null,
            int charStart = -1,
            int charEnd = -1)
        {
            var d = OpenCloseLemmaProperties.Defaults;
            d.openAngleDeg = ResolveFloat(OpenCloseLemmaPropertyKeys.OpenAngleDeg, promptSegments, clauseBindings, lemmaProperties, "90", charStart, charEnd);
            d.driveMode = ParseDriveMode(ResolveString(OpenCloseLemmaPropertyKeys.DriveMode, promptSegments, clauseBindings, lemmaProperties, "hybrid", charStart, charEnd));
            d.requireToolLemma = ResolveString(OpenCloseLemmaPropertyKeys.RequireToolLemma, promptSegments, clauseBindings, lemmaProperties, "", charStart, charEnd);
            d.unlockBeforeOpen = ResolveBool(OpenCloseLemmaPropertyKeys.UnlockBeforeOpen, promptSegments, clauseBindings, lemmaProperties, "false", charStart, charEnd);
            d.cameraStopId = ResolveString(OpenCloseLemmaPropertyKeys.CameraStopId, promptSegments, clauseBindings, lemmaProperties, "", charStart, charEnd);
            d.linearOnly = ResolveBool(OpenCloseLemmaPropertyKeys.LinearOnly, promptSegments, clauseBindings, lemmaProperties, "false", charStart, charEnd);
            d.actorIkProfileRef = ResolveString(OpenCloseLemmaPropertyKeys.ActorIkProfileRef, promptSegments, clauseBindings, lemmaProperties, "", charStart, charEnd);
            d.objectIkProfileRef = ResolveString(OpenCloseLemmaPropertyKeys.ObjectIkProfileRef, promptSegments, clauseBindings, lemmaProperties, "", charStart, charEnd);
            d.openAnimationRef = ResolveString(OpenCloseLemmaPropertyKeys.OpenAnimationRef, promptSegments, clauseBindings, lemmaProperties, "", charStart, charEnd);
            d.closeAnimationRef = ResolveString(OpenCloseLemmaPropertyKeys.CloseAnimationRef, promptSegments, clauseBindings, lemmaProperties, "", charStart, charEnd);
            d.soundOpenRef = ResolveString(OpenCloseLemmaPropertyKeys.SoundOpenRef, promptSegments, clauseBindings, lemmaProperties, "", charStart, charEnd);
            d.soundCloseRef = ResolveString(OpenCloseLemmaPropertyKeys.SoundCloseRef, promptSegments, clauseBindings, lemmaProperties, "", charStart, charEnd);
            d.dialogueSpanRef = ResolveString(OpenCloseLemmaPropertyKeys.DialogueSpanRef, promptSegments, clauseBindings, lemmaProperties, "", charStart, charEnd);
            d.questHintKind = ParseQuestHint(ResolveString(OpenCloseLemmaPropertyKeys.QuestHintKind, promptSegments, clauseBindings, lemmaProperties, "none", charStart, charEnd));
            d.questObjectiveId = ResolveString(OpenCloseLemmaPropertyKeys.QuestObjectiveId, promptSegments, clauseBindings, lemmaProperties, "", charStart, charEnd);
            d.autoCloseBt = ParseAutoCloseBt(ResolveString(OpenCloseLemmaPropertyKeys.AutoCloseBt, promptSegments, clauseBindings, lemmaProperties, "on-stop-exit", charStart, charEnd));
            d.autoCloseOnExit = ResolveBool(OpenCloseLemmaPropertyKeys.AutoCloseOnExit, promptSegments, clauseBindings, lemmaProperties, "false", charStart, charEnd);
            d.compileCloseAmbulation = ResolveBool(OpenCloseLemmaPropertyKeys.CompileCloseAmbulation, promptSegments, clauseBindings, lemmaProperties, "false", charStart, charEnd);
            d.closureMode = ParseClosureMode(ResolveString(OpenCloseLemmaPropertyKeys.ClosureMode, promptSegments, clauseBindings, lemmaProperties, "auto", charStart, charEnd));
            d.arrivalBlendCoefficient = UnityEngine.Mathf.Clamp01(ResolveFloat(OpenCloseLemmaPropertyKeys.ArrivalBlendCoefficient, promptSegments, clauseBindings, lemmaProperties, "0", charStart, charEnd));
            d.reachRadiusMeters = UnityEngine.Mathf.Max(0.1f, ResolveFloat(OpenCloseLemmaPropertyKeys.ReachRadiusMeters, promptSegments, clauseBindings, lemmaProperties, "0.6", charStart, charEnd));
            d.requireFacingTarget = ResolveBool(OpenCloseLemmaPropertyKeys.RequireFacingTarget, promptSegments, clauseBindings, lemmaProperties, "true", charStart, charEnd);
            return d;
        }

        public static AutoCloseBtMode ToRuntimeAutoClose(OpenCloseLemmaAutoCloseBtMode mode) => mode switch
        {
            OpenCloseLemmaAutoCloseBtMode.None => AutoCloseBtMode.None,
            OpenCloseLemmaAutoCloseBtMode.AfterChildren => AutoCloseBtMode.AfterChildren,
            OpenCloseLemmaAutoCloseBtMode.OnSequenceEnd => AutoCloseBtMode.OnSequenceEnd,
            OpenCloseLemmaAutoCloseBtMode.Manual => AutoCloseBtMode.Manual,
            _ => AutoCloseBtMode.OnStopExit,
        };

        static OpenCloseLemmaDriveMode ParseDriveMode(string raw)
        {
            raw = (raw ?? "hybrid").Trim().ToLowerInvariant();
            return raw switch
            {
                "physics" => OpenCloseLemmaDriveMode.Physics,
                "animation" => OpenCloseLemmaDriveMode.Animation,
                _ => OpenCloseLemmaDriveMode.Hybrid,
            };
        }

        static OpenCloseLemmaQuestHintKind ParseQuestHint(string raw)
        {
            raw = (raw ?? "none").Trim().ToLowerInvariant().Replace("_", "-");
            return raw switch
            {
                "complete" => OpenCloseLemmaQuestHintKind.Complete,
                "advance" => OpenCloseLemmaQuestHintKind.Advance,
                "note" => OpenCloseLemmaQuestHintKind.Note,
                "change" => OpenCloseLemmaQuestHintKind.Change,
                _ => OpenCloseLemmaQuestHintKind.None,
            };
        }

        static OpenCloseLemmaAutoCloseBtMode ParseAutoCloseBt(string raw)
        {
            raw = (raw ?? "on-stop-exit").Trim().ToLowerInvariant().Replace("_", "-");
            return raw switch
            {
                "none" => OpenCloseLemmaAutoCloseBtMode.None,
                "after-children" => OpenCloseLemmaAutoCloseBtMode.AfterChildren,
                "on-sequence-end" => OpenCloseLemmaAutoCloseBtMode.OnSequenceEnd,
                "manual" => OpenCloseLemmaAutoCloseBtMode.Manual,
                _ => OpenCloseLemmaAutoCloseBtMode.OnStopExit,
            };
        }

        static OpenCloseLemmaClosureMode ParseClosureMode(string raw)
        {
            raw = (raw ?? "auto").Trim().ToLowerInvariant().Replace("_", "-");
            return raw switch
            {
                "open-beat-closed" => OpenCloseLemmaClosureMode.OpenBeatClosed,
                "latch-failed" => OpenCloseLemmaClosureMode.LatchFailed,
                "close-beat-closed" => OpenCloseLemmaClosureMode.CloseBeatClosed,
                "cancelled" => OpenCloseLemmaClosureMode.Cancelled,
                _ => OpenCloseLemmaClosureMode.Auto,
            };
        }

        static bool ResolveBool(string key, IReadOnlyList<PromptSegment> segs, IReadOnlyList<LocalizationClauseBindingRecord> clauses, IReadOnlyList<ThesaurusEntryPropertyRecord> props, string def, int cs, int ce) =>
            DrinkLemmaPropertyResolver.ResolveBool(key, segs, clauses, props, def, cs, ce);

        static string ResolveString(string key, IReadOnlyList<PromptSegment> segs, IReadOnlyList<LocalizationClauseBindingRecord> clauses, IReadOnlyList<ThesaurusEntryPropertyRecord> props, string def, int cs, int ce) =>
            DrinkLemmaPropertyResolver.ResolveString(key, segs, clauses, props, def, cs, ce);

        static float ResolveFloat(string key, IReadOnlyList<PromptSegment> segs, IReadOnlyList<LocalizationClauseBindingRecord> clauses, IReadOnlyList<ThesaurusEntryPropertyRecord> props, string def, int cs, int ce) =>
            DrinkLemmaPropertyResolver.ResolveFloat(key, segs, clauses, props, def, cs, ce);
    }
}
