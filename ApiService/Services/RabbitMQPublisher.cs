using ApiService.Models;
using DatabaseService.Models;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace ApiService.Services
{
    public interface IRabbitMQPublisher
    {
        Task PublishMessageAsync<T>(T message);
    }

    public class RabbitMQPublisher : IRabbitMQPublisher
    {
        private readonly string _queueName = "create_entity_queue";
        private IConnection _connection;
        private IChannel _channel;

        public RabbitMQPublisher()
        {
            var factory = new ConnectionFactory
            {
                //HostName = "localhost",
                //Port = 5672,
                //UserName = "guest",
                //Password = "guest",
                HostName = "rabbitmq",
                AutomaticRecoveryEnabled = true,
            };
            _connection = factory.CreateConnectionAsync().Result;
            _channel = _connection.CreateChannelAsync().Result;
            _channel.QueueDeclareAsync(queue: _queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
        }


        public async Task PublishMessageAsync<T>(T message)
        {
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            await _channel.BasicPublishAsync(
                exchange: "",
                routingKey: "create_entity_queue",
                mandatory: true,
                body: body);
        }

        public void Dispose()
        {
            _channel?.CloseAsync();
            _connection?.CloseAsync();
        }

    }
}

