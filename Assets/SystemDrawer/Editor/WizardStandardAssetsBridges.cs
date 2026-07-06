using System;
using System.Reflection;
using UnityEngine;

internal static class WeatherStandardAssetsBridge
{
    internal static WizardSetupReport Setup(WeatherServiceWizardComponent wizard)
    {
        var report = new WizardSetupReport();
        if (wizard == null)
            return report;

        var type = Type.GetType("Weather.WeatherStandardAssets, Weather.Editor");
        if (type == null)
        {
            report.Warnings.Add("Weather.Editor assembly not loaded — open Weather Service Wizard manually.");
            return report;
        }

        var method = type.GetMethod("SetupForWizard", BindingFlags.Public | BindingFlags.Static);
        if (method == null)
        {
            report.Warnings.Add("WeatherStandardAssets.SetupForWizard not found.");
            return report;
        }

        try
        {
            var result = method.Invoke(null, new object[] { wizard });
            if (result is WizardSetupReport wr)
                return wr;
            report.Linked.Add("Weather system (via Weather.Editor)");
        }
        catch (TargetInvocationException ex)
        {
            report.Warnings.Add("Weather setup failed: " + (ex.InnerException?.Message ?? ex.Message));
        }

        return report;
    }
}

internal static class Spatial4DStandardAssetsBridge
{
    internal static WizardSetupReport Setup(Component wizard)
    {
        var report = new WizardSetupReport();
        if (wizard == null)
            return report;
        if (wizard.GetType().Name != "Spatial4DServiceWizard")
        {
            report.Warnings.Add("Component is not Spatial4DServiceWizard.");
            return report;
        }

        return InvokeSetup("Spatial4DStandardAssets, BedogaGenerator.Editor", wizard, report, "Spatial4D");
    }

    internal static WizardSetupReport SetupLoose(Component wizard) => Setup(wizard);

    static WizardSetupReport InvokeSetup(string typeName, Component wizard, WizardSetupReport report, string label)
    {
        var type = Type.GetType(typeName);
        if (type == null)
        {
            report.Warnings.Add(label + " editor assembly not loaded.");
            return report;
        }

        var method = type.GetMethod("Setup", BindingFlags.Public | BindingFlags.Static);
        if (method == null)
        {
            report.Warnings.Add(label + " StandardAssets.Setup not found.");
            return report;
        }

        try
        {
            var result = method.Invoke(null, new object[] { wizard });
            if (result is WizardSetupReport wr)
                return wr;
        }
        catch (TargetInvocationException ex)
        {
            report.Warnings.Add(label + " setup failed: " + (ex.InnerException?.Message ?? ex.Message));
        }

        return report;
    }
}

internal static class NetworkingStandardAssetsBridge
{
    internal static WizardSetupReport Setup(Component wizard)
    {
        var report = new WizardSetupReport();
        if (wizard == null)
            return report;
        if (wizard.GetType().Name != "NetworkServiceWizard")
        {
            report.Warnings.Add("Component is not NetworkServiceWizard.");
            return report;
        }

        var type = Type.GetType("NetworkingStandardAssets, SystemDrawer.Networking.Editor");
        if (type == null)
        {
            report.Warnings.Add("SystemDrawer.Networking.Editor not loaded.");
            return report;
        }

        var method = type.GetMethod("Setup", BindingFlags.Public | BindingFlags.Static);
        if (method == null)
            return report;

        try
        {
            var result = method.Invoke(null, new object[] { wizard });
            if (result is WizardSetupReport wr)
                return wr;
        }
        catch (TargetInvocationException ex)
        {
            report.Warnings.Add("Network setup failed: " + (ex.InnerException?.Message ?? ex.Message));
        }

        return report;
    }
}
