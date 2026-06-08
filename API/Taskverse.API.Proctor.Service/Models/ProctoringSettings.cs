namespace Taskverse.API.Proctor.Service.Models;

public class ProctoringSettings
{
    public bool Enabled { get; set; }
    public FullscreenSettings Fullscreen { get; set; } = new();
    public TabSwitchingSettings TabSwitching { get; set; } = new();
    public ClipboardSettings Clipboard { get; set; } = new();
    public ContextMenuSettings ContextMenu { get; set; } = new();
    public KeyboardShortcutsSettings KeyboardShortcuts { get; set; } = new();
    public DevToolsSettings DevTools { get; set; } = new();
    public NetworkSettings Network { get; set; } = new();
    public WarningsSettings Warnings { get; set; } = new();
    public RiskScoringSettings RiskScoring { get; set; } = new();
    public EventLoggingSettings EventLogging { get; set; } = new();
}

public class ThresholdProctoringRuleSettings
{
    public string WarningMessage { get; set; } = string.Empty;
    public bool LockAttemptOnLimitExceeded { get; set; }
    public bool AutoSubmitOnLimitExceeded { get; set; }
}

public class FullscreenSettings : ThresholdProctoringRuleSettings
{
    public bool Required { get; set; }
    public int MaxExitsAllowed { get; set; }
}

public class TabSwitchingSettings : ThresholdProctoringRuleSettings
{
    public bool DetectionEnabled { get; set; }
    public int MaxSwitchesAllowed { get; set; }
}

public class ClipboardSettings : ThresholdProctoringRuleSettings
{
    public bool DisableCopy { get; set; }
    public bool DisablePaste { get; set; }
    public bool DisableCut { get; set; }
    public int MaxCopyAttemptsAllowed { get; set; }
    public int MaxPasteAttemptsAllowed { get; set; }
    public int MaxCutAttemptsAllowed { get; set; }
}

public class ContextMenuSettings : ThresholdProctoringRuleSettings
{
    public bool Disabled { get; set; }
    public int MaxAttemptsAllowed { get; set; }
}

public class KeyboardShortcutsSettings : ThresholdProctoringRuleSettings
{
    public bool Disabled { get; set; }
    public int MaxBlockedShortcutAttemptsAllowed { get; set; }
    public List<string> BlockedKeys { get; set; } = [];
    public List<string> BlockedCombinations { get; set; } = [];
}

public class DevToolsSettings : ThresholdProctoringRuleSettings
{
    public bool DetectionEnabled { get; set; }
    public bool BlockCommonShortcuts { get; set; }
    public int DetectionIntervalSeconds { get; set; }
    public int ViewportDifferenceThresholdPixels { get; set; }
    public int MaxDetectionsAllowed { get; set; }
}

public class NetworkSettings : ThresholdProctoringRuleSettings
{
    public bool TrackDisconnects { get; set; }
    public int MaxDisconnectsAllowed { get; set; }
    public int MaxOfflineDurationSeconds { get; set; }
}

public class WarningsSettings
{
    public bool ShowCandidateWarnings { get; set; }
    public bool ShowViolationCount { get; set; }
    public bool RequireAcknowledgement { get; set; }
    public int DefaultWarningDurationSeconds { get; set; }
}

public class RiskScoringSettings
{
    public bool Enabled { get; set; }
    public RiskScoringWeightsSettings Weights { get; set; } = new();
    public RiskScoringLevelsSettings Levels { get; set; } = new();
}

public class RiskScoringWeightsSettings
{
    public int TabSwitch { get; set; }
    public int FullscreenExit { get; set; }
    public int CopyAttempt { get; set; }
    public int PasteAttempt { get; set; }
    public int CutAttempt { get; set; }
    public int ContextMenuAttempt { get; set; }
    public int BlockedShortcut { get; set; }
    public int PossibleDevTools { get; set; }
    public int NetworkDisconnect { get; set; }
}

public class RiskScoringLevelsSettings
{
    public RiskScoreRangeSettings Low { get; set; } = new();
    public RiskScoreRangeSettings Medium { get; set; } = new();
    public RiskScoreRangeSettings High { get; set; } = new();
    public RiskScoreRangeSettings Critical { get; set; } = new();
}

public class RiskScoreRangeSettings
{
    public int MinScore { get; set; }
    public int MaxScore { get; set; }
}

public class EventLoggingSettings
{
    public bool Enabled { get; set; }
    public bool BatchEvents { get; set; }
    public int BatchSize { get; set; }
    public int FlushIntervalSeconds { get; set; }
    public bool FlushOnTabHidden { get; set; }
    public bool FlushOnBeforeUnload { get; set; }
    public bool StoreClientTimestamp { get; set; }
    public bool StoreUserAgent { get; set; }
    public bool StoreIpAddress { get; set; }
    public bool StoreQuestionId { get; set; }
}
