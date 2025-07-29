using Amazon.DynamoDBv2.DataModel;
using Compartilhado.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Compartilhado.Model
{
    
    [DynamoDBTable("Pedidos")]
    public class Pedido
    {

        public Guid Id { get; set; }


        public decimal ValorTotal { get; set; }


        public DateTime DataDeCriacao { get; set; }

        public List<Produto> Produtos { get; set; }

        public Cliente Cliente { get; set; }

        public Pagamento Pagamento { get; set; }


        public string JustificativaDeCancelamento { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public StatusDoPedido Status { get; set; }

        public bool Cancelado { get; set; }
    }

}
