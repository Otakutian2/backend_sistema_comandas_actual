using Mapster;
using Microsoft.AspNetCore.Mvc;
using proyecto_backend.Enums;
using proyecto_backend.Interfaces;
using proyecto_backend.Models;
using proyecto_backend.Schemas;
using proyecto_backend.Utils;

namespace proyecto_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommandController : ControllerBase
    {
        private readonly ICommand _commandService;
        private readonly ITableRestaurant _tableService;
        private readonly IEmployee _employeeService;
        private readonly IDish _dishService;
        private readonly IAuth _authService;

        public CommandController(ICommand commandService, ITableRestaurant tableService, IDish dishService, IEmployee employeeService, IAuth authService)
        {
            _commandService = commandService;
            _employeeService = employeeService;
            _tableService = tableService;
            _dishService = dishService;
            _authService = authService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CommandGet>>> GetCommand()
        {
            return Ok((await _commandService.GetAll()).Adapt<List<CommandGet>>());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CommandGet>> GetCommand(int id)
        {
            var command = await _commandService.GetById(id);

            if (command == null)
            {
                return NotFound("Comanda no encontrada");
            }

            CommandGet commandGet = command.Adapt<CommandGet>();

            return Ok(commandGet);
        }

        [HttpPost]
        public async Task<ActionResult<CommandGet>> CreateCommand([FromBody] CommandCreate commandDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var commandEntity = commandDto.Adapt<Command>();
                commandEntity.EmployeeId = _authService.GetCurrentUserId();

                var createdCommand = await _commandService.CreateCommand(
                    commandEntity,
                    commandDto.TableRestaurantId,
                    commandDto.SeatCount
                );

                var response = createdCommand.Adapt<CommandGet>();

                return CreatedAtAction("GetCommand", new { id = response.Id }, response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ocurrió un error al procesar la comanda.");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CommandGet>> UpdateCommand(int id, [FromBody] CommandUpdate commandDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var updatedCommand = await _commandService.UpdateCommand(id,commandDto);

                var response = updatedCommand.Adapt<CommandGet>();

                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error interno al actualizar la comanda.");
            }
        }

        //[HttpPost]
        //public async Task<ActionResult<CommandGet>> CreateCommand([FromBody] CommandCreate command)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    TableRestaurant table = null;

        //    if (command.TableRestaurantId != null)
        //    {
        //        table = await _tableService.GetById(command.TableRestaurantId.Value);

        //        if (table == null)
        //        {
        //            return NotFound("Mesa no encontrada");
        //        }

        //        if (table.State == TableStateEnum.Occupied.GetEnumMemberValue())
        //        {
        //            return BadRequest("Mesa ocupada, eliga otra");
        //        }

        //        if (table.SeatCount < command.SeatCount)
        //        {
        //            return BadRequest("La cantidad de asientos en la comanda no puede exceder la cantidad permitida de la mesa");
        //        }
        //    }
        //    else
        //    {
        //        command.SeatCount = null;
        //    }

        //    //var ids = command.CommandDetailsCollection.Select(cd => cd.DishId);
        //    //var idsCount = await _dishService.Count(d => ids.Contains(d.Id));

        //    //if (idsCount != ids.Count())
        //    //{
        //    //    return BadRequest("No se encontró al menos un plato en la lista o hay elementos repetidos");
        //    //}

        //    var inputCommand = command.Adapt<Command>();

        //    inputCommand.EmployeeId = _authService.GetCurrentUserId();

        //    var createdCommand = await _commandService.CreateCommand(inputCommand);

        //    if (table != null && createdCommand.Id != 0)
        //    {
        //        table.State = TableStateEnum.Occupied.GetEnumMemberValue();
        //        await _tableService.UpdateTable(table);
        //    }

        //    var getCommand = (await _commandService.GetById(createdCommand.Id)).Adapt<CommandGet>();

        //    return CreatedAtAction(nameof(GetCommand), new { id = getCommand.Id }, getCommand);
        //}

        //[HttpPut("{id}")]
        //public async Task<ActionResult<CommandGet>> UpdateCommand(int id, [FromBody] CommandUpdate command)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    var commandUpdate = await _commandService.GetById(id);

        //    if (commandUpdate == null)
        //    {
        //        return NotFound("Comanda no encontrada");
        //    }

        //    if (commandUpdate.CommandStateId == (int)CommandStateEnum.Paid)
        //    {
        //        return BadRequest("No se puedes actualizar una comanda ya pagada");
        //    }
        //    if (commandUpdate.TableRestaurantId != null)
        //    {
        //        if (commandUpdate.TableRestaurant.SeatCount < command.SeatCount)
        //        {
        //            return BadRequest("La cantidad de asientos en la comanda no puede exceder la cantidad permitida de la mesa");
        //        }
        //    }
        //    else
        //    {
        //        command.SeatCount = null;
        //    }

        //    //var ids = command.CommandDetailsCollection.Select(cd => cd.DishId);
        //    //var idsCount = await _dishService.Count(d => ids.Contains(d.Id));

        //    //if (idsCount != ids.Count())
        //    //{
        //    //    return BadRequest("No se encontró al menos un plato de la lista o hay platos repetidos");
        //    //}

        //    //if (command.SeatCount != null)
        //    //{
        //    //    commandUpdate.SeatCount = command.SeatCount.Value;
        //    //}

        //    //commandUpdate.CommandDetailsCollection = command.CommandDetailsCollection.Adapt<List<CommandDetails>>();
        //    //commandUpdate.CustomerAnonymous = command?.CustomerAnonymous;
        //    await _commandService.UpdateCommand(command, id);

        //    var getCommand = (await _commandService.GetById(commandUpdate.Id)).Adapt<CommandGet>();

        //    return Ok(getCommand);
        //}

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCommand(int id)
        {
            var command = await _commandService.GetById(id);

            if (command == null)
            {
                return NotFound("Comanda no encontrada");
            }

            if (command.CommandStateId == (int)CommandStateEnum.Paid)
            {
                return BadRequest("No se puedes eliminar una comanda ya pagada");
            }

            await _commandService.DeleteCommand(command);

            TableRestaurant table = command.TableRestaurant;

            if (table != null)
            {
                table.State = TableStateEnum.Free.GetEnumMemberValue();
                await _tableService.UpdateTable(table);
            }

            return NoContent();
        }

        [HttpPut("prepare-command/{id}")]
        public async Task<ActionResult> PrepareCommand(int id)
        {
            var command = await _commandService.GetById(id);

            if (command == null)
            {
                return NotFound("Comanda no encontrada");
            }

            if (command.CommandStateId != (int)CommandStateEnum.Generated)
            {
                return BadRequest("No se puede preparar una comanda que no tenga un estado de 'Generado'");
            }

            await _commandService.PrepareCommand(command);

            return Ok("La comanda se está preparando");
        }
    }
}