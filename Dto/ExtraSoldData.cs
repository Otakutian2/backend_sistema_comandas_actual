namespace proyecto_backend.Dto
{
    public class ExtraSoldData
    {
        public string ExtraId { get; set; }
        public string ExtraCategoryId { get; set; }
        public string ExtraName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
        public List<DishPaymentMethodTotal> PaymentMethodTotals { get; set; } = new List<DishPaymentMethodTotal>();
    }
}
