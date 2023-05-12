using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Pagamentos.Context;
using Pagamentos.Models;
using System.Data.SqlClient;

namespace Pagamentos.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BoletoController : ControllerBase
    {
        private readonly BancoP _context;

        public BoletoController(BancoP context)
        {
            _context = context;
        }

        // GET: api/Boleto
        [HttpGet]
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

            return boletos;
        }

        // GET: api/Boleto
        [HttpGet]
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

            return boletos;
        }

        // POST: api/Boleto/pagar/5
        [HttpPost("PagarBoleto")]
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
                foreach (var item in boletos)
                {
                    // Verifica se o boleto está dentro do prazo de pagamento
                    var prazo = item.Inclusao.AddDays(2);
                    if (DateTime.Now > prazo)
                    {
                        item.Valido = false;
                    }

                    item.Valor = 0;
                    if (item.Valor == 0 && item.Valido == true || item.Valido == null)
                    {
                        item.Valor = 0;
                        _context.Entry(item).State = EntityState.Modified;
                        _context.SaveChanges();
                        mensagens.Add("Boleto pago do: " + item.Nome + " CPF: " + item.CPF);
                    }
                    else
                    {
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
            string directoryPath = @"C:\Users\Pichau\source\repos\Pagamentos\Pagamentos\BoletoBancario";
            string remessa = "remessa_0.txt";
            string filePath = Path.Combine(directoryPath, remessa);

            using (StreamReader reader = new StreamReader(filePath))
            {
                string[] linhas = reader.ReadToEnd().Split('\n');
                // "Extraindo" valores das remessas
                string dia = linhas[0].Substring(143, 2);
                string mes = linhas[0].Substring(145, 2);
                string ano = linhas[0].Substring(147, 4);
                string preco = linhas[2].Substring(86, 12);
                string centavos = linhas[2].Substring(98, 2);
                string diaVencimento = linhas[2].Substring(77, 2);
                string mesVencimento = linhas[2].Substring(79, 2);
                string anoVencimento = linhas[2].Substring(81, 4);
                string nomePagador = linhas[3].Substring(33, 40).Trim();
                string cpf = linhas[3].Substring(19, 14).Trim();

                string nomeBeneficiario = linhas[0].Substring(72, 30).Trim();
                decimal valorTitulo = decimal.Parse($"{preco},{centavos}");

                string dataBoleto = $"{dia}/{mes}/{ano}";
                string dataGeracao = $"{dia}/{mes}/{ano}".Trim();
                string dataVencimento = $"{diaVencimento}/{mesVencimento}/{anoVencimento}".Trim();
                string valor = $"{valorTitulo}";

                var boleto = new Boleto
                {

                    Nome = nomePagador,
                    Valor = Convert.ToDecimal(valor),
                    CPF = cpf,
                    Inclusao = Convert.ToDateTime(dataVencimento).Add(new TimeSpan(12, 30, 45, 500)).ToUniversalTime(),

                };

                var existeiguais = _context.Boletos.Where(x => x.CPF == cpf).FirstOrDefault();
                if (existeiguais != null)
                {
                    return BadRequest("Existem CPFS iguais");
                }

                // Salva os boletos no banco de dados
                _context.Boletos.Add(boleto);
                _context.SaveChanges();

                return Ok("Os boletos foram importados com sucesso.");

            }
        }

        //[HttpPost]
        //[Route("Import")]
        //public ActionResult ImportarBoletos()
        //{

        //    string filePath = @"C:\Users\Pichau\Desktop\BoletosNaoPagos\Boletos.xlsx";
        //    ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
        //    // Abre o arquivo da planilha
        //    var fileInfo = new FileInfo(filePath);
        //    using var package = new ExcelPackage(fileInfo);

        //    // Obtém a planilha "Boletos"
        //    var worksheet = package.Workbook.Worksheets["Dados"];
        //    if (worksheet == null)
        //    {
        //        return BadRequest("A planilha não contém uma aba chamada 'Dados'.");
        //    }

        //    // Lê os dados da planilha
        //    var boletos = new List<Boleto>();
        //    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
        //    {
        //        var nome = worksheet.Cells[row, 1].GetValue<string>();
        //        var valor = worksheet.Cells[row, 2].GetValue<string>();
        //        var cpf = worksheet.Cells[row, 3].GetValue<string>();
        //        var matricula = worksheet.Cells[row, 4].GetValue<string>();
        //        var inclusao = worksheet.Cells[row, 5].GetValue<string>();

        //        if (!string.IsNullOrEmpty(cpf)) // Verifica se o CPF não é nulo ou vazio
        //        {
        //            var existeiguais = _context.Boletos.Where(x => x.CPF == cpf).FirstOrDefault();
        //            if (existeiguais != null)
        //            {
        //                return BadRequest("Existem CPFS iguais na Planilha");
        //            }
        //            valor = valor.Replace("R$", "");
        //            var boleto = new Boleto
        //            {
        //                Nome = nome,
        //                Valor = Convert.ToDecimal(valor),
        //                CPF = cpf,
        //                Matricula = Convert.ToDouble(matricula),
        //                Inclusao = Convert.ToDateTime(inclusao)

        //            };
        //            boletos.Add(boleto);
        //        }


        //    }



        //    // Salva os boletos no banco de dados
        //    _context.Boletos.AddRangeAsync(boletos);
        //    _context.SaveChanges();

        //    return Ok("Os boletos foram importados com sucesso.");
        //}


        // GET: api/Boleto/5

        [HttpGet("BuscarPorCpf")]
        public ActionResult<Boleto> GetBoleto(string cpf)
        {
            var boleto = _context.Boletos.Find(cpf);

            if (boleto == null)
            {
                return NotFound();
            }

            return boleto;
        }

        //// POST: api/Boleto
        //[HttpPost]
        //public ActionResult<Boleto> PostBoleto(Boleto boleto)
        //{
        //    return CreatedAtAction(nameof(GetBoleto), new
        //    {
        //        id = boleto.CPF
        //    }, boleto);
        //}

        //// PUT: api/Boleto/5
        //[HttpPut("{id}")]
        //public IActionResult PutBoleto(string id, Boleto boleto)
        //{
        //    if (id != boleto.CPF)
        //    {
        //        return BadRequest();
        //    }

        //    _context.Entry(boleto).State = EntityState.Modified;
        //    _context.SaveChanges();

        //    return NoContent();
        //}

        // DELETE: api/Boleto/5

        [HttpDelete("ExcluirBoletos")]
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