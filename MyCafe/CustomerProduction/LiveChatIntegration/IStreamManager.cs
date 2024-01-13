﻿using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace MyCafe.CustomerProduction;
internal interface IStreamManager
{
    public event EventHandler<ChatMessageReceivedEventArgs> OnChatMessageReceived;
    public Task<bool> Connect();
}

public class ChatMessageReceivedEventArgs : EventArgs
{
    public string Username { get; set; }
    public string Message { get; set; }
    public Color Color { get; set; } = Color.White;
    
}
