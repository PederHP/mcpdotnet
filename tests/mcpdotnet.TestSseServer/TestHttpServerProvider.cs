public class TestHttpServerProvider : HttpListenerServerProvider
{
    public List<string> SentEvents { get; } = new List<string>();
    public List<string> ReceivedMessages { get; } = new List<string>();

    public TestHttpServerProvider(int port) : base(port) { }

    public new Task SendEvent(string data, string eventId = null)
    {
        lock (SentEvents)
        {
            SentEvents.Add(data);
        }
        return base.SendEvent(data, eventId);
    }

    public new Task InitializeMessageHandler(Func<string, CancellationToken, bool> messageHandler)
    {
        // Wrap the handler to record messages
        return base.InitializeMessageHandler((message, token) =>
        {
            lock (ReceivedMessages)
            {
                ReceivedMessages.Add(message);
            }
            return messageHandler(message, token);
        });
    }
}
