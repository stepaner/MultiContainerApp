using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using DatabaseService.Data;
using Microsoft.EntityFrameworkCore.Metadata;
using DatabaseService.Models;
using System.Threading.Tasks;


namespace DatabaseService.BackGRoundServices;

public class RabbitMQBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _queueName = "create_entity_queue";
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly ILogger<RabbitMQBackgroundService> _logger;


    public RabbitMQBackgroundService(IServiceProvider serviceProvider, ILogger<RabbitMQBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory {
            //HostName = "localhost",
            //Port = 5672,
            //UserName = "guest",
            //Password = "guest",
            HostName = "rabbitmq",
            AutomaticRecoveryEnabled = true,
        };
        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(queue: _queueName, durable: false, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var entity = JsonSerializer.Deserialize<MyEntity>(message);
                if (entity is null)
                {
                    Console.WriteLine($"Entity don't creared, entinty is null!");
                    return;
                }

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();
                var dbEntity = dbContext.MyEntities.FirstOrDefault(e => e.Id == entity.Id);
                if (dbEntity == null)
                {
                    dbContext.MyEntities.Add(entity);
                }   
                else
                {
                    dbEntity.Name = entity.Name;
                }
                    
                await dbContext.SaveChangesAsync();
                Console.WriteLine($"Entity {(dbEntity == null ? "created" : "change")}: {entity.Name}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
            }
        };

        await _channel.BasicConsumeAsync(queue: _queueName, autoAck: true, consumer: consumer);

        // Логирование состояния подключения
        _connection.ConnectionShutdownAsync += async (sender, args) =>
        {
            Console.WriteLine("RabbitMQ connection lost. Attempting to reconnect...");
            await Restart();
        };
        Console.WriteLine("RabbitMq connection");
    }

    public async Task Restart()
    {
        _channel?.CloseAsync();
        _connection?.CloseAsync();
        await ExecuteAsync(CancellationToken.None);
    }   

    public override void Dispose()
    {
        _channel?.CloseAsync();
        _connection?.CloseAsync();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}