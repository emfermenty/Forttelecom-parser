using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using testparser;
using testparser.Entity;

public class RabbitMqService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqService> _logger;
    private IConnection _connection;
    private IModel _channel;
    private const string QueueName = "parser.run";

    public RabbitMqService(IServiceProvider serviceProvider, ILogger<RabbitMqService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    //слушает хост rabbitmq
    public void StartListening()
    {
        var factory = new ConnectionFactory()
        {
            HostName = "sersh.keenetic.name", 
            Port = 5672,
            UserName = "guest",     
            Password = "guest"      
        };
        //организует соединение 
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        //подключается к очереди
        _channel.QueueDeclare(queue: QueueName,
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        var consumer = new EventingBasicConsumer(_channel);
        //при подключении к очереди попадает сюда
        consumer.Received += async (model, ea) =>
        {
            try
            { //получаем сообщение через rabbit
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Получено сообщение: {Message}", message);

                using var scope = _serviceProvider.CreateScope();
                //при получении запускаем приложуху
                var app = scope.ServiceProvider.GetRequiredService<App>();
                await app.Run();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Ошибка при обработке сообщения: {Message}", ex.Message);
            }
        };

        _channel.BasicConsume(queue: QueueName,
                             autoAck: true,
                             consumer: consumer);

        _logger.LogInformation("Слушатель RabbitMQ запущен для очереди: {QueueName}", QueueName);
    }

    public void SendParserResult(loggerdto logData)
    {
        try
        {
            if (_channel == null || _channel.IsClosed)
            {
                InitializeConnection();
            }

            var json = System.Text.Json.JsonSerializer.Serialize(logData);
            var body = Encoding.UTF8.GetBytes(json);

            _channel.BasicPublish(
                exchange: "",
                routingKey: "parser.result",
                basicProperties: null,
                body: body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке результата в очередь");
        }
    }

    private void InitializeConnection()
    {
        var factory = new ConnectionFactory()
        {
            HostName = "sersh.keenetic.name",
            Port = 5672,
            UserName = "guest",
            Password = "guest"
        };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Объявляем очередь для результатов
        _channel.QueueDeclare(queue: "parser.result",
                            durable: false,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null);
    }

    public void StopListening()
    {
        _channel?.Close();
        _connection?.Close();
        _logger.LogInformation("Слушатель RabbitMQ остановлен");
    }
}