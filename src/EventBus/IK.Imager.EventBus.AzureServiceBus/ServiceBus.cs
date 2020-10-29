using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using IK.Imager.EventBus.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace IK.Imager.EventBus.AzureServiceBus
{
    public class ServiceBus : IEventBus
    {
        private readonly ILogger<ServiceBus> _logger;
        private readonly ServiceBusPersistentConnection _serviceBusPersistentConnection;
        private readonly ConcurrentDictionary<string, int> _subscriptions = new ConcurrentDictionary<string, int>();
        
        public ServiceBus(IOptions<ServiceBusSettings> serviceBusSettings, ILogger<ServiceBus> logger)
        {
            _logger = logger;
            _serviceBusPersistentConnection = new ServiceBusPersistentConnection(serviceBusSettings.Value.ConnectionString);
        }
        
        public async Task Publish<TIntegrationEvent>(string topicName, TIntegrationEvent iEvent)
            where TIntegrationEvent : IntegrationEvent
        {
            var jsonMessage = JsonConvert.SerializeObject(iEvent);
            var body = Encoding.UTF8.GetBytes(jsonMessage);

            var message = new Message
            {
                MessageId = iEvent.MessageId,
                Body = body,
                Label = topicName
            };

            var topicClient = await _serviceBusPersistentConnection.GetTopicClient(topicName);
            await topicClient.SendAsync(message);
        }

        public async Task Subscribe<T>(string topicName, string subscriptionName, IIntegrationEventHandler<T> handler, int maxConcurrentCalls = 1) 
            where T : IntegrationEvent
        {
            if (subscriptionName == null)
                throw new ArgumentNullException(nameof(subscriptionName));

            string key = GetSubscriptionKey(topicName, subscriptionName);
            if (_subscriptions.ContainsKey(key))
                throw new ArgumentException($"Subscription {subscriptionName} for topic {topicName} already exist");

            await _serviceBusPersistentConnection.CreateSubscriptionIfNotExists(topicName, subscriptionName);
            var client = new SubscriptionClient(_serviceBusPersistentConnection.ConnectionString, topicName, subscriptionName);

            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                // Maximum number of Concurrent calls to the callback `ProcessMessagesAsync`, set to 1 for simplicity.
                // Set it according to how many messages the application wants to process in parallel.
                MaxConcurrentCalls = maxConcurrentCalls,

                // Indicates whether MessagePump should automatically complete the messages after returning from User Callback.
                // False below indicates the Complete will be handled by the User Callback as in `ProcessMessagesAsync` below.
                AutoComplete = false
            };
            
            client.RegisterMessageHandler(async (message, token) =>
            {
                var model = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(message.Body));
                await handler.Handle(model);

                await client.CompleteAsync(message.SystemProperties.LockToken);
            }, messageHandlerOptions);

            _subscriptions.TryAdd(key, 0);
        }
        
        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
            _logger.LogError(exceptionReceivedEventArgs.Exception, "Endpoint={0}, EntityPath={1}, Action={2}", context.Endpoint, context.EntityPath, context.Action);
            return Task.CompletedTask;
        }
        
        private string GetSubscriptionKey(string topicName, string subscriptionName)
        {
            return topicName + subscriptionName;
        }
    }
}