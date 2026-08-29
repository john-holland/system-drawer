using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Measures = yes/no passing laws; questions = yes/no jurisdictional changes; candidates = electoral.
/// Fold into government mix when the kind applies; otherwise collect errors.
/// </summary>
public static class BallotGovFold
{
    public const string RoleLaw = "law";
    public const string RoleJurisdiction = "jurisdiction";
    public const string RoleElectoral = "electoral";

    public static string RoleFor(BallotKind kind)
    {
        if (kind == BallotKind.Measure) return RoleLaw;
        if (kind == BallotKind.Candidate) return RoleElectoral;
        return RoleJurisdiction;
    }

    public static string ListLabel(BallotKind kind)
    {
        if (kind == BallotKind.Measure) return "measures";
        if (kind == BallotKind.Candidate) return "candidates";
        return "questions";
    }

    public static void EnsureKindDefaults(BallotSpec spec)
    {
        if (spec == null) return;
        if (spec.kind == BallotKind.Candidate)
            return;
        spec.EnsureQuestionDefaults();
        if (spec.options == null) return;
        for (int i = 0; i < spec.options.Count; i++)
        {
            var opt = spec.options[i];
            if (opt == null || (opt.win != null && opt.win.Count > 0)) continue;
            if (opt.win == null) opt.win = new List<VotePropertyAssignment>();
            string prop = (spec.kind == BallotKind.Measure ? "law." : "jurisdiction.") + spec.ballotId;
            string value = opt.optionId == "yes" ? "true" : "false";
            opt.win.Add(new VotePropertyAssignment(prop, value));
        }
    }

    public static List<string> ErrorsFor(BallotKind kind, GovernmentFlavorMix mix, BallotTallyMethod tallyMethod = BallotTallyMethod.Plurality)
    {
        var errors = new List<string>();
        if ((tallyMethod == BallotTallyMethod.Irv || tallyMethod == BallotTallyMethod.Stv) && kind != BallotKind.Candidate)
            errors.Add("ranked choice (IRV/STV) is only used on candidate ballots");
        if (mix == null) return errors;
        float sum = mix.republic01 + mix.parliamentary01 + mix.theocracy01 + mix.monarchyCeremonial01 + mix.monarchyReal01 + mix.junta01;
        if (sum <= 1e-5f) return errors;
        float junta = mix.junta01 / sum;
        float realMon = mix.monarchyReal01 / sum;
        float theo = mix.theocracy01 / sum;
        float civic = (mix.republic01 + mix.parliamentary01 + mix.monarchyCeremonial01) / sum;
        string role = RoleFor(kind);
        if (junta >= 0.45f && role != RoleLaw)
            errors.Add(role + " ballots are not used under junta government; use measures for laws");
        if (realMon >= 0.45f && role == RoleElectoral)
            errors.Add("electoral ballots are not used under real monarchy");
        if (theo >= 0.45f && !mix.parliamentarySenateEnablesTheocracy && role == RoleElectoral)
            errors.Add("electoral ballots are not used under theocracy without a parliamentary senate");
        if (civic < 0.2f && role == RoleJurisdiction)
            errors.Add("jurisdictional questions require a civic government mix");
        return errors;
    }
}
