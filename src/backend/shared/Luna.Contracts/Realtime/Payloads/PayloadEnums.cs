namespace Luna.Contracts.Realtime.Payloads;

public enum InputType
{
    Text,
    Voice,
    GestureControl
}

public enum ResponseType
{
    TextExplanation,
    AudioStream,
    Question,
    Feedback,
    Encouragement
}

public enum TextDeltaOperation
{
    Append,
    Replace
}




