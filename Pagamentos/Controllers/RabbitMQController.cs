using Microsoft.AspNetCore.Mvc;
using Pagamentos.Controllers;
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

        private readonly BoletoController _boletoController;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private static readonly string QueueName = "EnviarRemessa";
        private static readonly ManualResetEvent messageReceivedEvent = new ManualResetEvent(false);
        public static List<string> Mensagens { get; set; } = new List<string>();

        public RabbitMQController(BoletoController boletoController)
        {
            var factory = new ConnectionFactory() { HostName = "localhost", Port = 32790 }; // Configure o nome do servidor do RabbitMQ
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _boletoController = boletoController;
        }

        [HttpPost("Consumer")]
        public IActionResult ConsumeMessages()
        {
            _channel.QueueDeclare(queue: QueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                // Processa a mensagem recebida
                Mensagens.Add(message);

                // Realiza qualquer lógica adicional necessária com a mensagem

                _channel.BasicAck(ea.DeliveryTag, multiple: false);

                if (_channel.MessageCount(QueueName) == 0)
                {
                    messageReceivedEvent.Set(); // Todas as mensagens foram recebidas
                }
            };

            _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);

            messageReceivedEvent.WaitOne(); // Aguarda até que todas as mensagens tenham sido recebidas

            _boletoController.LerRemessa();

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
