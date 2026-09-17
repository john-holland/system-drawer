using System;
using System.Collections.Generic;

/// <summary>
/// Maps surface tokens to canonical lemmas for built-in resolution (same URN as canonical entry).
/// </summary>
public static class BuiltInSynonyms
{
    private static readonly Dictionary<string, string> AliasToCanonical =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "nil", "null" },
            { "first person", "first-person" },
            { "third person", "third-person" },
            { "to the left of", "to-the-left-of" },
            { "to the right of", "to-the-right-of" },
            { "in front of", "in-front-of" },
            { "through there", "through-there" },
            { "over here", "over-here" },
            { "along the road", "along-the-road" },
            { "here here", "here-here" },
            { "there there", "there-there" },
            { "open chat", "open-chat" },
            { "open the chat", "open-chat" },
            { "close chat", "close-chat" },
            { "close the chat", "close-chat" },
            { "dismiss chat", "close-chat" },
            { "chat window", "chat" },
            { "chat box", "chat" },
            { "word bank", "word-bank" },
            { "compose box", "compose-box" },
            { "chat history", "chat-history" },
            { "road lane", "road-lane" },
            { "road_lane", "road-lane" },
            { "grass strip", "grass-strip" },
            { "grass_strip", "grass-strip" },
            { "phone pole", "phone-pole" },
            { "phone_pole", "phone-pole" },
            { "street wire", "street-wire" },
            { "street_wire", "street-wire" },
            { "wire end", "wire-end" },
            { "wire_end", "wire-end" },
            { "hanging shoes", "hanging-shoes" },
            { "hanging_shoes", "hanging-shoes" },
            { "walk button", "walk-button" },
            { "walk_button", "walk-button" },
            { "road sign", "road-sign" },
            { "road_sign", "road-sign" },
            { "jersey barrier", "jersey-barrier" },
            { "jersey_barrier", "jersey-barrier" },
            { "guard rail", "guard-rail" },
            { "guard_rail", "guard-rail" },
            { "emergency bar", "emergency-bar" },
            { "emergency_bar", "emergency-bar" },
            { "street luminaire", "street-luminaire" },
            { "street_luminaire", "street-luminaire" },
            { "street light", "street-light" },
            { "street_light", "street-light" },
            { "traffic signal", "traffic-signal" },
            { "traffic_signal", "traffic-signal" },
            { "single layer mix", "single-layer-mix" },
            { "single_layer_mix", "single-layer-mix" },
            { "max bend deg", "max-bend-deg" },
            { "max_bend_deg", "max-bend-deg" },
            { "see through sec", "see-through-sec" },
            { "see_through_sec", "see-through-sec" },
            { "cap open", "cap-open" },
            { "cap_open", "cap-open" },
            { "course load", "course-load" },
            { "course_load", "course-load" },
            { "age bracket", "age-bracket" },
            { "age_bracket", "age-bracket" },
            { "head master", "headmaster" },
            { "head_master", "headmaster" },
            { "scribe set", "scribe-set" },
            { "scribe_set", "scribe-set" },
            { "pecking order", "pecking-order" },
            { "pecking_order", "pecking-order" },
            { "relationship stage", "stage" },
            { "relationship_stage", "stage" },
            { "bill of rights", "rights" },
            { "bill_of_rights", "rights" },
            { "holy text", "scripture" },
            { "holy_text", "scripture" },
            { "game session", "game-session" },
            { "game_session", "game-session" },
            { "local save", "local-save" },
            { "local_save", "local-save" },
            { "save server to local", "save-server-to-local" },
            { "save_server_to_local", "save-server-to-local" },
            { "local server", "local-server" },
            { "local_server", "local-server" },
            { "home address", "home-address" },
            { "home_address", "home-address" },
            { "homeAddress", "home-address" },
            { "if_so", "if-so" },
            { "geneva conventions", "geneva-conventions" },
            { "geneva_conventions", "geneva-conventions" },
            { "respects geneva conventions", "respects-geneva-conventions" },
            { "respects_geneva_conventions", "respects-geneva-conventions" },
            { "rights returned", "rights-returned" },
            { "rights_returned", "rights-returned" },
            { "announce rights returned", "announce-rights-returned" },
            { "announce_rights_returned", "announce-rights-returned" },
            { "AnnounceRightsReturned", "announce-rights-returned" },
            { "constitution rights returned", "announce-rights-returned" },
            { "constitution_rights_returned", "announce-rights-returned" },
            { "water heater", "water-heater" },
            { "water_heater", "water-heater" },
            { "water main", "water-main" },
            { "water_main", "water-main" },
            { "water filter", "water-filter" },
            { "water_filter", "water-filter" },
            { "circuit breaker", "circuit-breaker" },
            { "circuit_breaker", "circuit-breaker" },
            { "wall plug", "wall-plug" },
            { "wall_plug", "wall-plug" },
            { "jacobs ladder", "jacobs-ladder" },
            { "jacobs_ladder", "jacobs-ladder" },
            { "sump pump", "sump-pump" },
            { "sump_pump", "sump-pump" },
            { "imitirrrr__", "imitirrrr" },
            { "recoup wheel", "recoup" },
            { "recoup_wheel", "recoup" },
            { "frame inclusion", "frame-inclusion" },
            { "frame_inclusion", "frame-inclusion" },
            { "shell inclusion", "shell-inclusion" },
            { "shell_inclusion", "shell-inclusion" },
            { "hollow subtract", "hollow-subtract" },
            { "hollow_subtract", "hollow-subtract" },
            { "frame id", "frame-id" },
            { "frame_id", "frame-id" },
            { "frameId", "frame-id" },
            { "door id", "door-id" },
            { "door_id", "door-id" },
            { "doorId", "door-id" },
            { "hinge label", "hinge-label" },
            { "hinge_label", "hinge-label" },
            { "hingeLabel", "hinge-label" },
            { "slot kind", "slot-kind" },
            { "slot_kind", "slot-kind" },
            { "z index", "z-index" },
            { "z_index", "z-index" },
            { "zIndex", "z-index" },
            { "hollow radius", "hollow-radius" },
            { "hollow_radius", "hollow-radius" },
            { "sewing machine", "sewing-machine" },
            { "sewing_machine", "sewing-machine" },
            { "stitch program", "stitch-program" },
            { "stitch_program", "stitch-program" },
            { "needle throat", "needle-throat" },
            { "needle_throat", "needle-throat" },
            { "bobbin race", "bobbin-race" },
            { "bobbin_race", "bobbin-race" },
            { "thread path", "thread-path" },
            { "thread_path", "thread-path" },
            { "looper race", "looper-race" },
            { "looper_race", "looper-race" },
            { "door bobbin", "door-bobbin" },
            { "door_bobbin", "door-bobbin" },
            { "door bed", "door-bed" },
            { "door_bed", "door-bed" },
            { "door looper", "door-looper" },
            { "door_looper", "door-looper" },
            { "sewing shell", "sewing-shell" },
            { "sewing_shell", "sewing-shell" },
            { "sewing frame", "sewing-frame" },
            { "sewing_frame", "sewing-frame" },
            { "serger shell", "serger-shell" },
            { "serger_shell", "serger-shell" },
            { "looper upper", "looper-upper" },
            { "looper_upper", "looper-upper" },
            { "looper lower", "looper-lower" },
            { "looper_lower", "looper-lower" },
            { "spindle bore", "spindle-bore" },
            { "spindle_bore", "spindle-bore" },
            { "tailstock quill", "tailstock-quill" },
            { "tailstock_quill", "tailstock-quill" },
            { "chip chute", "chip-chute" },
            { "chip_chute", "chip-chute" },
            { "door headstock", "door-headstock" },
            { "door_headstock", "door-headstock" },
            { "door gearbox", "door-gearbox" },
            { "door_gearbox", "door-gearbox" },
            { "door chip pan", "door-chip-pan" },
            { "door_chip_pan", "door-chip-pan" },
            { "lathe frame bed", "lathe-frame-bed" },
            { "lathe_frame_bed", "lathe-frame-bed" },
            { "lathe shell cover", "lathe-shell-cover" },
            { "lathe_shell_cover", "lathe-shell-cover" }
        };

    public static void RegisterAlias(string alias, string canonical)
    {
        if (string.IsNullOrWhiteSpace(alias) || string.IsNullOrWhiteSpace(canonical))
            return;
        AliasToCanonical[alias.Trim().ToLowerInvariant()] = canonical.Trim().ToLowerInvariant();
    }

    /// <summary>Returns canonical lemma for lookup, or original token if none.</summary>
    public static string CanonicalizeToken(string token)
    {
        if (string.IsNullOrEmpty(token)) return token;
        string t = token.Trim().ToLowerInvariant();
        return AliasToCanonical.TryGetValue(t, out string c) ? c : t;
    }

    /// <summary>Normalize phrase tokens through <see cref="CanonicalizeToken"/> (join with spaces).</summary>
    public static string CanonicalizePhraseTokens(string[] tokens)
    {
        if (tokens == null || tokens.Length == 0) return "";
        for (int i = 0; i < tokens.Length; i++)
            tokens[i] = CanonicalizeToken(tokens[i]);
        return string.Join(" ", tokens);
    }

    /// <summary>
    /// If the joined tokens match a known multi-word alias (e.g. <c>first person</c>), returns the canonical lemma (e.g. <c>first-person</c>).
    /// Otherwise returns null.
    /// </summary>
    public static string TryCanonicalizeMultiWordPhrase(string[] tokens)
    {
        if (tokens == null || tokens.Length < 2)
            return null;
        string joined = string.Join(" ", tokens).Trim();
        if (string.IsNullOrEmpty(joined))
            return null;
        return AliasToCanonical.TryGetValue(joined.ToLowerInvariant(), out string c) ? c : null;
    }
}
