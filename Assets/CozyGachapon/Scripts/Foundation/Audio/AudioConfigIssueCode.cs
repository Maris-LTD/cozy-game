namespace CozyGachapon.Audio
{
    public enum AudioConfigIssueCode
    {
        None,
        CueMissing,
        CueIdMissing,
        InvalidBus,
        NoClip,
        MaxInstancesInvalid,
        MinIntervalNegative,
        PriorityOutOfRange,
        DuplicateCueId,
        ScaleSamplesMissing,
        PitchShiftExceeded,
        VolumeAboveZero,
        VoiceBudgetExceeded,
        StormPeakGainExceeded,
        StealAgeTooShort,
        SpawnCueMissing,
        SpawnCueNotDropNew,
        SpawnClipTooLong,
        ManualSpawnClipExceedsCooldownMargin,
        SpawnClipMarginOutOfRange,
        TotalGainAboveZero
    }
}
