using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;

namespace IK.Imager.EventBus.AzureServiceBus
{
     /// <summary>
    /// Manages service bus topics and subscription
    /// Create and provides single topic client for each topic 
    /// </summary>
    public class ServiceBusPersistentConnection
    {
        private readonly ConcurrentDictionary<string, ITopicClient> _topicClients = new ConcurrentDictionary<string, ITopicClient>();
        
        private readonly ManagementClient _managementClient;

        public ServiceBusPersistentConnection(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            ConnectionString = connectionString;
            _managementClient = new ManagementClient(ConnectionString);
        }

        public string ConnectionString { get; }

        /// <summary>
        /// Create or get client for topic with name <paramref name="topicName"/>
        /// </summary>
        /// <param name="topicName"></param>
        /// <returns></returns>
        public async Task<ITopicClient> GetTopicClient(string topicName)
        {
            ITopicClient topicClient;

            if (_topicClients.ContainsKey(topicName))
            {
                topicClient = _topicClients[topicName];
                if (topicClient.IsClosedOrClosing)
                {
                    topicClient = new TopicClient(ConnectionString, topicName, RetryPolicy.Default);
                }
            }
            else
            {
                await CreateTopicIfNotExists(topicName);
                topicClient = new TopicClient(ConnectionString, topicName, RetryPolicy.Default);
                _topicClients.TryAdd(topicName, topicClient);
            }

            return topicClient;
        }

        /// <summary>
        /// Create the subscription (if does not exist) with name <paramref name="topicName"/> in topic <paramref name="topicName"/>
        /// </summary>
        /// <param name="topicName"></param>
        /// <param name="subscriptionName"></param>
        /// <returns></returns>
        public async Task CreateSubscriptionIfNotExists(string topicName, string subscriptionName)
        {
            await CreateTopicIfNotExists(topicName);

            if (!await _managementClient.SubscriptionExistsAsync(topicName, subscriptionName))
                await _managementClient.CreateSubscriptionAsync(new SubscriptionDescription(topicName, subscriptionName));
        }

        private async Task CreateTopicIfNotExists(string topicName)
        {
            if (!await _managementClient.TopicExistsAsync(topicName))
                await _managementClient.CreateTopicAsync(topicName);
        }
    }
}