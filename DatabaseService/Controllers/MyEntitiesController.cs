using Microsoft.AspNetCore.Mvc;
using DatabaseService.Data;
using DatabaseService.Models;
using RabbitMQ.Client;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MyEntitiesController : ControllerBase
{
    private readonly MyDbContext _context;

    public MyEntitiesController(MyDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MyEntity entity)
    {
        _context.MyEntities.Add(entity);
        await _context.SaveChangesAsync();

        // Отправляем сообщение в RabbitMQ
        var factory = new ConnectionFactory { HostName = "rabbitmq" };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync(); // Используем CreateChannelAsync

        await channel.QueueDeclareAsync(queue: "my_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);

        var message = $"New entity created: {entity.Name}";
        var body = Encoding.UTF8.GetBytes(message);

        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: "my_queue",
            mandatory: true,            
            body: body);

        return Ok(entity);
    }
}