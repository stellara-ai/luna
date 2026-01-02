namespace Luna.Contracts.Realtime.Payloads;

using Luna.Contracts.Realtime;

public class ControlSignalPayload : WsPayload
{
    public ControlSignal Signal { get; set; }
}