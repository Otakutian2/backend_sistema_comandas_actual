using System.ComponentModel.DataAnnotations.Schema;

namespace proyecto_backend.Models
{
    [Table("command_details_extras")]
    public class CommandDetailsExtras
    {
        public int Id { get; set; }
        public int Quantity { get; set; }

        [Column("extra_dish_id")]
        public string ExtraDishId { get; set; }
        public Dish ExtraDish { get; set; }

        [Column("command_detail_id")]
        public int CommandDetailsId { get; set; }
        public CommandDetails CommandDetails { get; set; }
    }
}
