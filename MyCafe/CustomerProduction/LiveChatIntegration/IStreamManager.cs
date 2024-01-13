using System;
using System.Threading.Tasks;

namespace MyCafe.CustomerProduction;
internal interface IStreamManager
{
    public event EventHandler<ChatMessageReceivedEventArgs> OnChatMessageReceived;
    public Task<bool> Connect();
    public Task StartListening();
}

public class ChatMessageReceivedEventArgs : EventArgs
{
    public string Username { get; set; }
    public string Message { get; set; }
}
