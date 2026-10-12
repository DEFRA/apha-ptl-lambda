namespace PTL.Lambda.Shared;

/// <summary>
/// Display/identity metadata for a single-pass cleanup, grouped out of CleanupFunctionRunner.RunAsync's
/// parameter list to stay under SonarCloud's S107 max-parameter-count rule.
/// </summary>
public sealed record CleanupDescriptor(string CleanupName, LegacyServiceType ServiceType, string ResultNoun, string ActionVerb);
