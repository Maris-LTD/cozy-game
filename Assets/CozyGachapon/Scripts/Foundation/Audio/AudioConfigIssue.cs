namespace CozyGachapon.Audio
{
    public readonly struct AudioConfigIssue
    {
        public AudioConfigIssueCode Code { get; }
        public string CueId { get; }
        public string Subject { get; }
        public int NoteIndex { get; }
        public float Value { get; }
        public float SecondaryValue { get; }
        public float Limit { get; }
        public bool IsError { get; }

        public AudioConfigIssue(
            AudioConfigIssueCode code,
            string cueId,
            string subject,
            int noteIndex,
            float value,
            float secondaryValue,
            float limit,
            bool isError)
        {
            Code = code;
            CueId = cueId;
            Subject = subject;
            NoteIndex = noteIndex;
            Value = value;
            SecondaryValue = secondaryValue;
            Limit = limit;
            IsError = isError;
        }

        public static AudioConfigIssue ForCue(AudioConfigIssueCode code, string cueId)
        {
            return new AudioConfigIssue(code, cueId, null, -1, 0f, 0f, 0f, true);
        }

        public static AudioConfigIssue ForCueValue(AudioConfigIssueCode code, string cueId, float value, float limit)
        {
            return new AudioConfigIssue(code, cueId, null, -1, value, 0f, limit, true);
        }

        public static AudioConfigIssue ForKnob(AudioConfigIssueCode code, string subject, float value, float limit)
        {
            return new AudioConfigIssue(code, null, subject, -1, value, 0f, limit, true);
        }
    }
}
