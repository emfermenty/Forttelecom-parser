using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Configuration;
using System.Text;
using testparser.Entity;
using testparser;

public class RabbitMqService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqService> _logger;
    private IConnection _connection;
    private IModel _channel;

    private const string QueueName = "parser.run";
    private const string ResultQueueName = "parser.result";

    private readonly string _hostName;
    private readonly int _port;
    private readonly string _userName;
    private readonly string _password;

    public RabbitMqService(IServiceProvider serviceProvider, ILogger<RabbitMqService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // это лежит в app.config
        _hostName = ConfigurationManager.AppSettings["rabbitmq_host"];
        _port = int.Parse(ConfigurationManager.AppSettings["rabbitmq_port"]);
        _userName = ConfigurationManager.AppSettings["rabbitmq_username"];
        _password = ConfigurationManager.AppSettings["rabbitmq_password"];
    }

    public void StartListening()
    {
        var factory = new ConnectionFactory()
        {
            HostName = _hostName,
            Port = _port,
            UserName = _userName,
            Password = _password
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(queue: QueueName,
                            durable: false,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Получено сообщение: {Message}", message);

                using var scope = _serviceProvider.CreateScope();
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
                routingKey: ResultQueueName,
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
            HostName = _hostName,
            Port = _port,
            UserName = _userName,
            Password = _password
        };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(queue: ResultQueueName,
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