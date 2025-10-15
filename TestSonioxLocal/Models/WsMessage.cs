using System.Net.WebSockets;

namespace TestSonioxLocal.Models;

public class WsMessage
{
    public WsMessage(ArraySegment<byte> payload, WebSocketMessageType messageType, bool endOfMessage, bool isInit)
    {
        Payload = payload;
        MessageType = messageType;
        EndOfMessage = endOfMessage;
        IsInit = isInit;
    }

    public ArraySegment<byte> Payload { get; private set; }
    public WebSocketMessageType MessageType { get; private set; }
    public bool EndOfMessage { get; private set; }
    public bool IsInit { get; private set; }
}

