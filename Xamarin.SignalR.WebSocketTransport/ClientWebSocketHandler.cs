namespace Xamarin.SignalR.Transport
{
    internal class ClientWebSocketHandler : WebSocketHandler
    {
        private readonly WebSocketTransport _webSocketTransport;

        public ClientWebSocketHandler(WebSocketTransport webSocketTransport)
            : base(maxIncomingMessageSize: null)
        {
            _webSocketTransport = webSocketTransport;
        }

        public override void OnMessage(string message)
        {
            _webSocketTransport.OnMessage(message);
        }

        public override void OnOpen()
        {
            _webSocketTransport.OnOpen();
        }

        public override void OnClose()
        {
            _webSocketTransport.OnClose();
        }

        public override void OnError()
        {
            _webSocketTransport.OnError(Error);
        }
    }
}
