using System.ComponentModel.DataAnnotations;

namespace proyecto_backend.Schemas
{
    public class ThermalPrinterGet
    {
        [Required(ErrorMessage = "El campo 'Id Comanda' es requerido")]
        public int CommandId { get; set; }
        public bool ShowPrice { get; set; } = true;
    }
}
