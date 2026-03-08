namespace proyecto_backend.Dto
{
    public class DishSoldData
    {
        public string DishId { get; set; }
        public string DishCategoryId { get; set; }
        public string DishName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
        public List<DishPaymentMethodTotal> PaymentMethodTotals { get; set; } = new List<DishPaymentMethodTotal>();
    }

    public class DishPaymentMethodTotal
    {
        public int PaymentMethodId { get; set; }
        public string PaymentMethodName { get; set; }
        public decimal Amount { get; set; }
    }
}
