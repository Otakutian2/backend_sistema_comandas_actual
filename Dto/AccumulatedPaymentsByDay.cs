namespace proyecto_backend.Dto
{
    public class AccumulatedPaymentsByDay
    {
        public int PaymentMethodId { get; set; }
        public string PaymentMethodName { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
