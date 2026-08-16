namespace TaskManager;

using Messages;

public interface IGatewayReceiver
{
    public void ReceiveMessage(Message message);
}