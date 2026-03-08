using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace proyecto_backend.Models
{
    public class Command
    {
        public int Id { get; set; }

        [Column("seat_count")]
        public int? SeatCount { get; set; }

        [Column("total_order_price")]
        [Precision(6, 2)]
        public decimal TotalOrderPrice { get; set; }

        [Column("customer_anonymous")]
        [MaxLength(150)]
        public string CustomerAnonymous { get; set; }

        [Column("created_at")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("table_restaurant_id")]
        public int? TableRestaurantId { get; set; }
        public TableRestaurant TableRestaurant { get; set; }

        [Column("command_state_id")]
        public int CommandStateId { get; set; }
        public CommandState CommandState { get; set; }

        public Receipt Receipt { get; set; }

        [Column("employee_id")]
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }

        public decimal Discount { get; set; }
        public string DiscountType { get; set; }

        public List<CommandDetails> CommandDetailsCollection { get; set; } = new List<CommandDetails>();
    }
}
