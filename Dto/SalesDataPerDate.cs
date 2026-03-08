namespace proyecto_backend.Dto
{
    public class SalesDataPerDate
    {
        public DateTime CreatedAt { get; set; }
        public decimal AccumulatedSales { get; set; }
        public int NumberOfGeneratedReceipts { get; set; }
        public int QuantityOfDishSales { get; set; }
        public int QuantityOfExtrasSold { get; set; }
        public string BestSellingDish { get; set; }

        public List<AccumulatedPaymentsByDay> accumulatedPaymentsByDays { get; set; } = new List<AccumulatedPaymentsByDay>();
        public List<DishSoldData> SoldDishes { get; set; } = new List<DishSoldData>();
        public List<ExtraSoldData> SoldExtras { get; set; } = new List<ExtraSoldData>();

    }
}
