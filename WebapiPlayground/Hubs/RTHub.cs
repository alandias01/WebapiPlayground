﻿using Microsoft.AspNetCore.SignalR;
using System.Threading.Channels;

namespace WebapiPlayground.Hubs
{
    public class RTHub : Hub
    {
        INotificationService _notificationService;
        public RTHub(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }
        public Task SendMessage(string user)
        {
            //await Clients.All.SendAsync("ReceiveMessage", user);
            return _notificationService.Subscribe1(Context.ConnectionId);
        }

        public void SendMessage2(string user)
        {
            //await Clients.All.SendAsync("ReceiveMessage", user);
            _notificationService.Subscribe2(Context.ConnectionId);
        }

        public Task SendMessage3(string message)
        {
            var ds = new DataService();
            //await Clients.All.SendAsync("ReceiveMessage", message);

            while (true)
            {
                ds.ExecuteAsync(msg =>
                {
                    Clients.Caller.SendAsync("ReceiveMessage", message + msg);
                });
            }
        }

        public ChannelReader<string> SendChannel(CancellationToken cancellationToken)
        {
            var channel = Channel.CreateUnbounded<string>();
            _ = WriteItemsAsync(channel.Writer, cancellationToken);
            return channel.Reader;
        }

        private async Task WriteItemsAsync(ChannelWriter<string> writer, CancellationToken cancellationToken)
        {
            Exception localException = null;
            try
            {
                for (var i = 0; i < 5; i++)
                {
                    await writer.WriteAsync(i.ToString(), cancellationToken);

                    // Use the cancellationToken in other APIs that accept cancellation
                    // tokens so the cancellation can flow down to them.
                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                localException = e;
            }
            finally
            {
                writer.Complete(localException);
            }
        }

        //Any new connection, will trigger if user refreshes browser
        //A connected user will not trigger this if they keep calling hub methods
        //You can use this virtual method to ad connectionIds to a queue
        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        //Called when client refreshes, exception is null
        //Called when client calls stop(), exception is null
        //Called when network connection fails, exception has value
        //You can use this virtual method to remove connectionIds from a queue
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            return base.OnDisconnectedAsync(exception);
        }
    }

    public interface INotificationService
    {
        public Task Subscribe1(string connectionId);
        public void Subscribe2(string connectionId);
    }

    public class NotificationService : INotificationService
    {
        private IHubContext<RTHub> _hub;
        public List<string> ConnectionIds = new List<string>();
        private DataService ds = new DataService();

        public NotificationService(IHubContext<RTHub> hub)
        {
            _hub = hub;
        }

        public async Task Subscribe1(string connectionId)
        {
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(500);
                _hub.Clients.Client(connectionId).SendAsync("ReceiveMessage", i);
            }
        }

        public void Subscribe2(string connectionId)
        {
            ds.ExecuteAsync((x) =>
            {
                _hub.Clients.Client(connectionId).SendAsync("ReceiveMessage", x);
            });
        }        
    }

    public class DataService
    {
        public void ExecuteAsync(Action<string> callback)
        {
            new Thread(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    var uniqueData = DateTime.Now.Millisecond.ToString();
                    callback(uniqueData);
                    Thread.Sleep(100000);
                }
                callback("done");

            }).Start();
        }
    }
}
