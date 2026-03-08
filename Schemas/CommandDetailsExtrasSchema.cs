namespace proyecto_backend.Schemas
{
    public class CommandDetailsExtrasPrincipal
    {
        public int Quantity { get; set; }
        public string ExtraDishId { get; set; }
    }
    public class CommandDetailsExtrasGet : CommandDetailsExtrasPrincipal
    {
        public DishGet ExtraDish { get; set; }
    }
   
}
