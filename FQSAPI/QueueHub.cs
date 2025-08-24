using Microsoft.AspNetCore.SignalR;

namespace FQSAPI
{
    public class QueueHub : Hub
    {
        public async Task SendQueueCode(int queueCode)
        {
            await Clients.All.SendAsync("ReceiveQueueCode", queueCode);
        }
    }
}
