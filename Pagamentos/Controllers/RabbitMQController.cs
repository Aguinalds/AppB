using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Text;

namespace YourNamespace.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RabbitMQController : ControllerBase
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        public static List<string> Mensagens { get; set; } = new List<string>();

        public RabbitMQController()
        {
            var factory = new ConnectionFactory() { HostName = "localhost", Port = 32790 }; // Configure o nome do servidor do RabbitMQ
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
            
     
        }

        [HttpPost("Consumer")]
        public IActionResult ConsumeMessages()
        {
            _channel.QueueDeclare(queue: "EnviarRemessa", // Configure o nome da fila
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                // Processa a mensagem recebida
                Mensagens.Add(message);

                // Realiza qualquer lógica adicional necessária com a mensagem

                _channel.BasicAck(ea.DeliveryTag, multiple: false);

            };

            _channel.BasicConsume(queue: "EnviarRemessa", // Configure o nome da fila
                                  autoAck: false,
                                  consumer: consumer);


            return Ok(Mensagens);
        }

        [HttpGet("close")]
        public IActionResult CloseConnection()
        {
            _channel.Close();
            _connection.Close();

            return Ok("Conexão encerrada");
        }

    }
}
