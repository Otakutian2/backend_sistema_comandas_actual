using Mapster;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using proyecto_backend.Dto;
using proyecto_backend.Enums;
using proyecto_backend.Interfaces;
using proyecto_backend.Models;
using proyecto_backend.Schemas;
using proyecto_backend.Utils;

namespace proyecto_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReceiptController : Controller
    {
        private readonly IReceipt _receiptService;
        private readonly ICash _cashService;
        private readonly IReceiptType _receiptTypeService;
        private readonly ICommand _commandService;
        private readonly ITableRestaurant _tableService;
        private readonly IAuth _authService;
        private readonly IPaymentMethod _paymentMethodService;
        private readonly int IGV = 18;

        public ReceiptController(IReceipt receiptService, ICommand commandService, ITableRestaurant tableService, ICash cashService, IReceiptType receiptTypeService, IAuth authService, IPaymentMethod paymentMethodService)
        {
            _receiptService = receiptService;
            _commandService = commandService;
            _tableService = tableService;
            _cashService = cashService;
            _receiptTypeService = receiptTypeService;
            _authService = authService;
            _paymentMethodService = paymentMethodService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReceiptGet>>> GetReceipt()
        {
            List<ReceiptGet> listReceipt = (await _receiptService.GetAll()).Adapt<List<ReceiptGet>>();


            return Ok(listReceipt.OrderByDescending(x => x.CreatedAt));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ReceiptGet>> GetReceipt(int id)
        {
            var receipt = await _receiptService.GetById(id);

            if (receipt == null)
            {
                return NotFound("Comprobante no encontrado");
            }

            ReceiptGet receiptGet = receipt.Adapt<ReceiptGet>();

            return Ok(receiptGet);
        }

        [HttpPost]
        public async Task<ActionResult<ReceiptGet>> CreateReceipt([FromBody] ReceiptCreate receipt)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var command = await _commandService.GetById(receipt.CommandId); 

            if (command == null)
            {
                return NotFound("Comanda no encontrada");
            }

            if (command.CommandStateId != (int)CommandStateEnum.Prepared)
            {
                return BadRequest("No se puede generar el comprobante debido a que la comanda aún no está preparada");
            }

            var receiptTypeCount = await _receiptTypeService.Count(rt => rt.Id == receipt.ReceiptTypeId);

            if (receiptTypeCount == 0)
            {
                return NotFound("Tipo de comprobante no encontrado");
            }

            var cashCount = await _cashService.Count(c => c.Id == receipt.CashId);

            if (cashCount == 0)
            {
                return NotFound("Caja no encontrada");
            }

            var ids = receipt.ReceiptDetailsCollection.Select(cd => cd.PaymentMethodId);
            var idsCount = await _paymentMethodService.Count(p => ids.Contains(p.Id));

            if (idsCount != ids.Count())
            {
                return BadRequest("No se encontró al menos un método de pago en la lista o hay elementos repetidos");
            }

            receipt.CustomerId ??= 1;

            //var subtotal = command.CommandDetailsCollection.Sum(c => c.OrderPrice);
            var discount = command.Discount;
            var totalFinal = command.TotalOrderPrice; // Ya viene con descuento aplicado
            var totalPagado = receipt.ReceiptDetailsCollection.Sum(c => c.Amount);

    
            bool descuentoCubreTodo = totalFinal == 0;

            if (descuentoCubreTodo)
            {
                if (totalPagado > 0)
                {
                    return BadRequest("No deben existir pagos si el descuento cubre el 100% de la comanda.");
                }
            }
            else
            {
                if (totalPagado != totalFinal)
                {
                    return BadRequest("El monto total pagado debe coincidir con el total final.");
                }
            }

            var newReceipt = receipt.Adapt<Receipt>();

            newReceipt.EmployeeId = (await _authService.GetCurrentUser()).Id;
            //newReceipt.Igv = igv;
            newReceipt.Igv = 0;
            newReceipt.AdditionalAmount = receipt.AdditionalAmount;
            newReceipt.TotalPrice = totalPagado;

            await _receiptService.CreateReceipt(newReceipt);
            await _commandService.PayCommand(command);

            var table = command.TableRestaurant;

            if (table != null)
            {
                table.State = TableStateEnum.Free.GetEnumMemberValue();

                await _tableService.UpdateTable(table);
            }


            var getReceipt = (await _receiptService.GetById(newReceipt.Id)).Adapt<CommandGet>();

            return CreatedAtAction(nameof(GetReceipt), new { id = getReceipt.Id }, getReceipt);
        }

        [HttpGet("sales-data-per-date")]
        public async Task<ActionResult<IEnumerable<SalesDataPerDate>>> GetSalesDataPerDate()
        {
            return Ok(await _receiptService.GetSalesDataPerDate());
        }

    }
}
