using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyecto_backend.Schemas;
using proyecto_backend.Services;

namespace proyecto_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ThermalPrinterController:ControllerBase
    {
        private ThermalPrinterManager _manager;

        public ThermalPrinterController(ThermalPrinterManager thermalPrinterManager)
        {
            _manager = thermalPrinterManager;
        }

        
        [HttpPost]
        public  ActionResult GenerateTicket([FromBody] ThermalPrinterGet printerGet)
        {
            FileCheckStatus fileCheckStatus = new FileCheckStatus();
            try
            {
                fileCheckStatus = _manager.GeneratePrinterCommand(printerGet.CommandId,printerGet.ShowPrice).Result;

                if (fileCheckStatus.status.Equals("Error"))
                {
                  return  BadRequest(fileCheckStatus);
                }

                return Ok(fileCheckStatus);
            }
            catch (Exception ex)
            {
                Dictionary<string, object> status = new Dictionary<string, object>();
                status.Add("status", "error");
                status.Add("message",ex.Message);

                return StatusCode(500,status);
            }
        }
    }
}
