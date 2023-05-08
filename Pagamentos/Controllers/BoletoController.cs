using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Pagamentos.Context;
using Pagamentos.Models;
using System.ComponentModel;

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
        public ActionResult<IEnumerable<Boleto>> GetBoletos()
        {
            return _context.Boletos.ToList();
        }

        // POST: api/Boleto/pagar/5
        [HttpPost("PagarBoleto")]
        public IActionResult PagarBoleto(string cpf,decimal valor)
        {
            var boleto = _context.Boletos.Find(cpf);

            if (boleto == null)
            {
                return NotFound();
            }

            // Verifica se o boleto está dentro do prazo de pagamento
            var prazo = boleto.Inclusao.AddDays(2);
            if (DateTime.Now > prazo)
            {
                return BadRequest("Boleto fora do prazo de pagamento.");
            }

            // Processa o pagamento do boleto
            boleto.Valor -= valor;

            if (boleto.Valor <= 0)
            {
                boleto.Valor = 0;          
                _context.Entry(boleto).State = EntityState.Modified;
                _context.SaveChanges();
                return Ok("Boleto pago.");
            }
            else
            {
                _context.Entry(boleto).State = EntityState.Modified;
                _context.SaveChanges();

                return Ok("Pagamento processado. Valor restante do boleto: " + boleto.Valor.ToString("C"));
            }
        }


        [HttpPost]
        [Route("Import")]
        public ActionResult ImportarBoletos(string filePath)
        {
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            // Abre o arquivo da planilha
            var fileInfo = new FileInfo(filePath);
            using var package = new ExcelPackage(fileInfo);

            // Obtém a planilha "Boletos"
            var worksheet = package.Workbook.Worksheets["Boletos"];
            if (worksheet == null)
            {
                return BadRequest("A planilha não contém uma aba chamada 'Boletos'.");
            }

            // Lê os dados da planilha
            var boletos = new List<Boleto>();
            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var nome = worksheet.Cells[row, 1].GetValue<string>();
                var valor = worksheet.Cells[row, 2].GetValue<string>();
                var cpf = worksheet.Cells[row, 3].GetValue<string>();
                var matricula = worksheet.Cells[row, 4].GetValue<string>();
                var inclusao = worksheet.Cells[row, 5].GetValue<string>();

                if (!string.IsNullOrEmpty(cpf)) // Verifica se o CPF não é nulo ou vazio
                {
                    var existeiguais = _context.Boletos.Where(x => x.CPF == cpf).FirstOrDefault();
                    if (existeiguais != null)
                    {
                        return BadRequest("Existem CPFS iguais na Planilha");
                    }
                    valor = valor.Replace("R$", "");
                    var boleto = new Boleto
                    {
                        Nome = nome,
                        Valor = Convert.ToDecimal(valor),
                        CPF = cpf,
                        Matricula = Convert.ToDouble(matricula),
                        Inclusao = Convert.ToDateTime(inclusao)

                    };
                    boleto.Inclusao = DateTimeOffset.Now.ToOffset(TimeSpan.Zero);
                    boletos.Add(boleto);
                }


            }



            // Salva os boletos no banco de dados
            _context.Boletos.AddRangeAsync(boletos);
            _context.SaveChanges();

            return Ok("Os boletos foram importados com sucesso.");
        }


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

        //// DELETE: api/Boleto/5
        //[HttpDelete("{id}")]
        //public IActionResult DeleteBoleto(string id)
        //{
        //    var boleto = _context.Boletos.Find(id);

        //    if (boleto == null)
        //    {
        //        return NotFound();
        //    }

        //    _context.Boletos.Remove(boleto);
        //    _context.SaveChanges();

        //    return NoContent();
        //}
    }
}