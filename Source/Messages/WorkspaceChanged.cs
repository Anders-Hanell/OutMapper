namespace Messages;

public sealed record WorkspaceChanged(string WorkspaceFolder) : Message;
