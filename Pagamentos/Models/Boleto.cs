using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Pagamentos.Models
{
    [Table("Boletos")]
    public class Boleto
    {
        public string Nome { get; set; }
        public decimal Valor { get; set; }
        [Key]
        public string CPF { get; set; }
        public double Matricula { get; set; }
        public DateTime Inclusao { get; set; }
        public bool? Valido { get; set; }

    }
}
