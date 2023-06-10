using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pagamentos.Context;
using Pagamentos.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.RegularExpressions;
using YourNamespace.Controllers;

namespace Pagamentos.Controllers
{
    [ApiController]
    [Route("api/Boletos")]
    public class BoletoController : ControllerBase
    {
        public static List<string> Mensagens { get; set; } = new List<string>();
        private readonly BancoP _context;
        private static readonly string ExchangeName = "ConfirmacaoRecebimento";
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public BoletoController(BancoP context)
        {
            var factory = new ConnectionFactory() { HostName = "localhost", Port = 32790 }; // Configure o nome do servidor do RabbitMQ
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _context = context;
        }

        // GET: api/Boleto
        [HttpGet]
        [Route("ListarTodosBoletos")]
        public ActionResult<IEnumerable<Boleto>> GetBoletos(int pageSize)
        {
            if (pageSize <= 0)
            {
                return Ok("Informe o número de registros");
            }
            var boletos = _context.Boletos
                .Skip((1 - 1) * pageSize)
                .Take(pageSize)
                .OrderBy(x => x.Nome)
                .ToList();

            int qtdBoletos = boletos.Count;

            var resultado = new
            {
                QuantidadeBoletos = qtdBoletos,
                Boletos = boletos
            };

            return Ok(resultado);
        }

        // GET: api/Boleto
        [HttpGet]
        [Route("BoletoInvalidos")]
        public ActionResult<IEnumerable<Boleto>> BoletoInvalidos(int pageSize)
        {
            if (pageSize <= 0)
            {
                return Ok("Informe o número de registros");
            }
            var boletos = _context.Boletos
                .Skip((1 - 1) * pageSize)
                .Take(pageSize)
                .OrderBy(x => x.Nome)
                .Where(x => x.Valido == false)
                .ToList();

            int qtdBoletos = boletos.Count;

            var resultado = new
            {
                QuantidadeBoletos = qtdBoletos,
                Boletos = boletos
            };

            return Ok(resultado);
        }

        // GET: api/Boleto
        [HttpGet]
        [Route("BoletoPagos")]
        public ActionResult<IEnumerable<Boleto>> BoletoPagos(int pageSize)
        {
            if (pageSize <= 0)
            {
                return Ok("Informe o número de registros");
            }
            var boletos = _context.Boletos
                .Skip((1 - 1) * pageSize)
                .Take(pageSize)
                .OrderBy(x => x.Nome)
                .Where(x => x.Valido == true)
                .ToList();

            int qtdBoletos = boletos.Count;

            var resultado = new
            {
                QuantidadeBoletos = qtdBoletos,
                Boletos = boletos
            };

            return Ok(resultado);
        }

        // POST: api/Boleto/pagar/5
        [HttpPost]
        [Route("PagarBoleto")]
        public IActionResult PagarBoletos(int Quantidade)
        {
            if (Quantidade <= 0)
            {
                return Ok("Informe a quantidade de boletos para pagar!");
            }
            var boletos = _context.Boletos
                .Skip((1 - 1) * Quantidade)
                .Take(Quantidade)
                .OrderBy(x => x.Nome)
                .ToList();

            if (boletos == null)
            {
                return NotFound();
            }

            var mensagens = new List<string>();

            try
            {
                int quantidadeInvalidos = (int)Math.Round(Quantidade * 0.2); // Calcula a quantidade de boletos inválidos

                for (int i = 0; i < boletos.Count; i++)
                {
                    var item = boletos[i];

                    // Verifica se o boleto está dentro do prazo de pagamento
                    if (i >= boletos.Count - quantidadeInvalidos)
                    {
                        item.Valido = false;
                    }
                    if (item.Valido == true || item.Valido == null)
                    {
                        item.Valor = 0;
                        item.Valido = true;
                        _context.Entry(item).State = EntityState.Modified;
                        _context.SaveChanges();
                        mensagens.Add("Boleto pago do: " + item.Nome + " CPF: " + item.CPF);
                    }
                    else
                    {
                        _context.Entry(item).State = EntityState.Modified;
                        _context.SaveChanges();
                        mensagens.Add("Erro ao pagar o boleto do: " + item.Nome);
                    }
                }
            }
            catch (Exception ex)
            {
                mensagens.Add("Erro ao processar o pagamento de boletos!");
                return Ok(mensagens);
            }

            return Ok(mensagens);
        }


        [HttpPost]
        [Route("LerRemessa")]
        public ActionResult LerRemessa()
        {
            //DIRETORIO DE ONDE AS REMESSAS SE ENCONTRAM
            string directoryPath = @"C:\Users\Pichau\source\repos\Pagamentos\Pagamentos\BoletoBancario";
            string[] files = Directory.GetFiles(directoryPath, "remessa*.txt");

  


            var mensagens = new List<string>();
            foreach (string filePath in files)
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string[] linhas = reader.ReadToEnd().Split('\n');
                    // "Extraindo" valores das remessas
                    string dia = linhas[0].Substring(143, 2);
                    string mes = linhas[0].Substring(145, 2);
                    string ano = linhas[0].Substring(147, 4);
                    string preco = linhas[1].Substring(86, 12);
                    string centavos = linhas[1].Substring(98, 2);
                    string diaVencimento = linhas[1].Substring(77, 2);
                    string mesVencimento = linhas[1].Substring(79, 2);
                    string anoVencimento = linhas[1].Substring(81, 4);
                    string nomePagador = linhas[2].Substring(33, 40).Trim();
                    string cpf = linhas[2].Substring(19, 14).Trim();

                    string nomeBeneficiario = linhas[0].Substring(72, 30).Trim();
                    if (!preco.EndsWith("."))
                    {
                        preco += ".";
                    }
                    centavos = centavos.TrimStart('.');
                    decimal valorTitulo = decimal.Parse($"{preco},{centavos}");

                    string dataBoleto = $"{dia}/{mes}/{ano}";
                    string dataGeracao = $"{dia}/{mes}/{ano}".Trim();
                    string dataVencimento = $"{diaVencimento}/{mesVencimento}/{anoVencimento}".Trim();
                    string valor = $"{valorTitulo}";

                    //TRANSFORMA EM OBJETO
                    var boleto = new Boleto
                    {

                        Nome = nomePagador,
                        Valor = Convert.ToDecimal(valor),
                        CPF = cpf,
                        Inclusao = Convert.ToDateTime(dataVencimento).Add(new TimeSpan(12, 30, 45, 500)).ToUniversalTime(),

                    };

                    string pattern = @"remessa(\d+)";
                    string numeroRemessa = "";
                    Match match = Regex.Match(filePath, pattern);
                    if (match.Success)
                    {
                      numeroRemessa = match.Groups[1].Value;  // Número da remessa, por exemplo, "991"
                    }
                    

                    //VEREFICAÇÃO DE CPFS DUPLICADOS
                    var versetemboletos =  _context.Boletos.ToList();
                    if (versetemboletos.Count > 0)
                    {
                        var existeiguais = _context.Boletos.Where(x => x.CPF == cpf).FirstOrDefault();
                        if (existeiguais != null)
                        {
                            mensagens.Add("Erro ao importar o boleto do: " + boleto.Nome + " CPF: " + boleto.CPF + " Nº: " + numeroRemessa);
                        }
                        else
                        {
                            // Salva os boletos no banco de dados
                            mensagens.Add("Boleto importado com Sucesso: " + boleto.Nome + " CPF: " + boleto.CPF + " Nº: " + numeroRemessa);
         
                            _context.Boletos.Add(boleto);
                            _context.SaveChanges();
                        }

                    }
                    else
                    {
                        // Salva os boletos no banco de dados
                        mensagens.Add("Boleto importado com Sucesso: " + boleto.Nome + "CPF: " + boleto.CPF);
                        _context.Boletos.Add(boleto);
                        _context.SaveChanges();
                    }



                }            
            }
            // Ordena as mensagens em ordem crescente
            mensagens = mensagens.OrderBy(mensagem => mensagem).ToList();
            ProduterMessages();
            return Ok(mensagens);
        }


        [HttpPost("Produter")]
        public IActionResult ProduterMessages()
        {
            _channel.QueueDeclare(queue: ExchangeName,
                     durable: false,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);

            var mensagemConfirmacao = "Todas as remessas foram lidas!";
            var body = Encoding.UTF8.GetBytes(mensagemConfirmacao);
            _channel.BasicPublish(exchange: "",
                                 routingKey: ExchangeName,
                                 basicProperties: null,
                                 body: body);

            return Ok("Mensagem enviada para confirmação de leitura das Remessas!");
        }

        

        [HttpGet]
        [Route("BuscarPorCpf")]
        public ActionResult<Boleto> GetBoleto(string cpf)
        {
            var boleto = _context.Boletos.Find(cpf);

            if (boleto == null)
            {
                return NotFound();
            }

            return boleto;
        }  

        [HttpDelete]
        [Route("DeletarTodosBoletos")]
        public IActionResult DeleteBoleto()
        {
            var boleto = _context.Boletos.ToList();

            if (boleto == null)
            {
                return NotFound();
            }

            foreach (var item in boleto)
            {
                _context.Boletos.Remove(item);
                _context.SaveChanges();
                
            }


            return Ok("Todos os Boletos Foram Excluídos");
        }
    }
}