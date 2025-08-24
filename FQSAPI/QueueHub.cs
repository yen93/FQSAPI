using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.SignalR;

namespace FQSAPI
{
    [EnableCors("AllowAll")]
    public class QueueHub : Hub
    {
        public async Task SendQueueCode(int queueCode)
        {
            await Clients.All.SendAsync("ReceiveQueueCode", queueCode);
        }
    }
}
