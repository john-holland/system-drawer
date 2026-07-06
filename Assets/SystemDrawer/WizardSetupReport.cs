using System.Collections.Generic;
using System.Text;

/// <summary>Result of a wizard standard-assets setup pass (created / linked / skipped).</summary>
public sealed class WizardSetupReport
{
    public readonly List<string> Created = new List<string>();
    public readonly List<string> Linked = new List<string>();
    public readonly List<string> Skipped = new List<string>();
    public readonly List<string> Warnings = new List<string>();

    public void Merge(WizardSetupReport other)
    {
        if (other == null)
            return;
        Created.AddRange(other.Created);
        Linked.AddRange(other.Linked);
        Skipped.AddRange(other.Skipped);
        Warnings.AddRange(other.Warnings);
    }

    public string Summary
    {
        get
        {
            var sb = new StringBuilder();
            AppendSection(sb, "Created", Created);
            AppendSection(sb, "Linked", Linked);
            AppendSection(sb, "Skipped (already present)", Skipped);
            AppendSection(sb, "Warnings", Warnings);
            if (sb.Length == 0)
                sb.Append("Nothing to do.");
            return sb.ToString();
        }
    }

    static void AppendSection(StringBuilder sb, string title, List<string> items)
    {
        if (items == null || items.Count == 0)
            return;
        if (sb.Length > 0)
            sb.AppendLine();
        sb.AppendLine(title + ":");
        for (int i = 0; i < items.Count; i++)
            sb.AppendLine("  • " + items[i]);
    }
}
