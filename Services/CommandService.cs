using Mapster;
using Microsoft.EntityFrameworkCore;
using proyecto_backend.Data;
using proyecto_backend.Enums;
using proyecto_backend.Interfaces;
using proyecto_backend.Models;
using proyecto_backend.Schemas;
using proyecto_backend.Utils;
using System.Linq.Expressions;

namespace proyecto_backend.Services
{
    public class CommandService : ICommand
    {
        private readonly CommandContext _context;

        public CommandService(CommandContext context)
        {
            _context = context;
        }

        public async Task<List<Command>> GetAll()
        {
            List<Command> command = await _context.Command
                .Include(c => c.TableRestaurant)
                .Include(c => c.Employee.User)
                .Include(c => c.Employee.Role)
                .Include(c => c.CommandState)
                .Include(c => c.CommandDetailsCollection).ThenInclude(d => d.Dish).ThenInclude(ca => ca.Category)
                .Include(c => c.CommandDetailsCollection).ThenInclude(d => d.Extras).ThenInclude(e => e.ExtraDish)
                .ToListAsync();

            return command;
        }

        public async Task<Command> GetById(int id)
        {
            var command = await _context.Command
                .Include(c => c.TableRestaurant)
                .Include(c => c.Employee.User)
                .Include(c => c.Employee.Role)
                .Include(c => c.CommandState)
                .Include(c => c.CommandDetailsCollection).ThenInclude(d => d.Dish).ThenInclude(ca => ca.Category)
                .Include(c => c.CommandDetailsCollection).ThenInclude(d => d.Extras).ThenInclude(e => e.ExtraDish)
                .FirstOrDefaultAsync(c => c.Id == id);

            return command;
        }

        //public async Task<Command> CreateCommand(Command command)
        //{


        //    //using var transaction = _context.Database.BeginTransaction();
        //    //bool result = false;

        //    //try
        //    //{
        //    //    command.CommandStateId = (int)CommandStateEnum.Prepared;

        //    //    decimal totalOrderPrice = 0;

        //    //    var dishIds = command.CommandDetailsCollection.Select(c => c.DishId).ToList();
        //    //    var dishPrices = await _context.Dish.Where(d => dishIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.Price);

        //    //    foreach (var c in command.CommandDetailsCollection)
        //    //    {
        //    //        c.DishPrice = dishPrices[c.DishId];
        //    //        c.OrderPrice = c.DishPrice * c.DishQuantity;
        //    //        totalOrderPrice += c.OrderPrice;
        //    //    }

        //    //    command.TotalOrderPrice = totalOrderPrice;

        //    //    _context.Command.Add(command);
        //    //    await _context.SaveChangesAsync();

        //    //    transaction.Commit();

        //    //    result = true;
        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //    Console.WriteLine(ex);
        //    //    transaction.Rollback();
        //    //}

        //    //return result;
        //}


        public async Task<Command> CreateCommand(Command command, int? tableId, int? seatCount)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (tableId.HasValue)
                {
                    var table = await _context.TableRestaurant
                        .FirstOrDefaultAsync(t => t.Id == tableId.Value);

                    if (table == null)
                        throw new KeyNotFoundException("Mesa no encontrada");

                    if (table.State == TableStateEnum.Occupied.GetEnumMemberValue())
                        throw new InvalidOperationException("La mesa ya está ocupada.");

                    if (table.SeatCount < seatCount)
                        throw new InvalidOperationException("La cantidad de asientos excede el límite de la mesa.");

                    table.State = TableStateEnum.Occupied.GetEnumMemberValue();
                    _context.TableRestaurant.Update(table);
                }

                command.CommandStateId = (int)CommandStateEnum.Prepared;
                command.TableRestaurantId = tableId; 
                command.SeatCount = seatCount;

                var dishIds = command.CommandDetailsCollection.Select(c => c.DishId).ToList();

                var extraDishIds = command.CommandDetailsCollection
                    .Where(c => c.Extras != null)
                    .SelectMany(c => c.Extras)
                    .Select(e => e.ExtraDishId)
                    .ToList();

                var allProductIds = dishIds.Union(extraDishIds).Distinct().ToList();

                var priceDictionary = await _context.Dish
                    .Where(d => allProductIds.Contains(d.Id))
                    .AsNoTracking() 
                    .ToDictionaryAsync(d => d.Id, d => d.Price);

                decimal grandTotal = 0;

                foreach (var detail in command.CommandDetailsCollection)
                {
                    if (!priceDictionary.TryGetValue(detail.DishId, out decimal dishPrice))
                        throw new Exception($"El plato con ID {detail.DishId} no existe o no tiene precio.");

                    detail.DishPrice = dishPrice;

                    decimal detailSubtotal = detail.DishPrice * detail.DishQuantity;

                    if (detail.Extras != null && detail.Extras.Any())
                    {
                        foreach (var extra in detail.Extras)
                        {
                            if (priceDictionary.TryGetValue(extra.ExtraDishId, out decimal extraPrice))
                            {
                                detailSubtotal += (extraPrice * extra.Quantity);
                                extra.ExtraDish = null;
                            }
                        }
                    }

                    detail.OrderPrice = detailSubtotal;

                    grandTotal += detailSubtotal;
                }

                // 5. Aplicar Descuentos
                if (command.Discount > 0)
                {
                    var discountAmount = GlobalUtils.CalculateDiscountedPrice(grandTotal, command.Discount, command.DiscountType);
                    grandTotal = discountAmount;
                }

                command.TotalOrderPrice = grandTotal;
                command.CreatedAt = GlobalUtils.GetPeruTime();
                // 6. Guardar
                _context.Command.Add(command);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return command;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw; // Re-lanzamos la excepción para que el Controller la maneje o el Middleware de errores
            }
        }

        public async Task<Command> UpdateCommand(int id, CommandUpdate commandDto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var existingCommand = await _context.Command
                    .Include(c => c.CommandDetailsCollection)
                        .ThenInclude(cd => cd.Extras)
                    .Include(c => c.TableRestaurant) 
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (existingCommand == null)
                    throw new KeyNotFoundException("Comanda no encontrada");

                if (existingCommand.CommandStateId == (int)CommandStateEnum.Paid)
                    throw new InvalidOperationException("No se puede actualizar una comanda que ya ha sido pagada.");

                if (existingCommand.TableRestaurant != null && commandDto.SeatCount.HasValue)
                {
                    if (existingCommand.TableRestaurant.SeatCount < commandDto.SeatCount.Value)
                        throw new InvalidOperationException("La cantidad de asientos excede la capacidad de la mesa.");
                }
                 
                if (existingCommand.CommandDetailsCollection.Any())
                {
                    _context.CommandDetailsExtras.RemoveRange(
                        existingCommand.CommandDetailsCollection.SelectMany(cd => cd.Extras));

                    _context.CommandDetails.RemoveRange(existingCommand.CommandDetailsCollection);
                }

                var dishIds = commandDto.CommandDetailsCollection.Select(c => c.DishId);
                var extraIds = commandDto.CommandDetailsCollection
                    .Where(c => c.Extras != null)
                    .SelectMany(c => c.Extras.Select(e => e.ExtraDishId));

                var allProductIds = dishIds.Union(extraIds).Distinct().ToList();

                var prices = await _context.Dish
                    .Where(d => allProductIds.Contains(d.Id))
                    .AsNoTracking() 
                    .ToDictionaryAsync(d => d.Id, d => d.Price);

                decimal grandTotal = 0;

                foreach (var itemDto in commandDto.CommandDetailsCollection)
                {
                    if (!prices.TryGetValue(itemDto.DishId, out decimal dishPrice))
                        throw new KeyNotFoundException($"El plato {itemDto.DishId} no existe.");

                    Guid uniqueId;

                    if (!Guid.TryParse(itemDto.UniqueId, out uniqueId))
                    {
                        uniqueId = Guid.NewGuid();
                    }

                    // Crear nuevo detalle
                    var newDetail = new CommandDetails
                    {
                        DishId = itemDto.DishId,
                        DishQuantity = itemDto.DishQuantity,
                        DishPrice = dishPrice, 
                        UniqueId = uniqueId,
                        Extras = new List<CommandDetailsExtras>(),
                        Observation = itemDto.Observation,
                    };

                    decimal detailSubtotal = dishPrice * itemDto.DishQuantity;

                    if (itemDto.Extras != null)
                    {
                        foreach (var extraDto in itemDto.Extras)
                        {
                            if (prices.TryGetValue(extraDto.ExtraDishId, out decimal extraPrice))
                            {
                                var newExtra = new CommandDetailsExtras
                                {
                                    ExtraDishId = extraDto.ExtraDishId,
                                    Quantity = extraDto.Quantity,
                                };
                                newDetail.Extras.Add(newExtra);

                                detailSubtotal += (extraPrice * extraDto.Quantity);
                            }
                        }
                    }

                    newDetail.OrderPrice = detailSubtotal;

                    grandTotal += detailSubtotal;

                    existingCommand.CommandDetailsCollection.Add(newDetail);
                }
                decimal newTotal = 0;
                if (commandDto.Discount > 0)
                {
                    var discountVal = GlobalUtils.CalculateDiscountedPrice(grandTotal, commandDto.Discount, commandDto.DiscountType);
                    newTotal = discountVal;
                }
                else
                {
                    newTotal = grandTotal;
                }

                    existingCommand.Discount = commandDto.Discount;
                existingCommand.DiscountType = commandDto.DiscountType;
                existingCommand.TotalOrderPrice = newTotal;
                existingCommand.CustomerAnonymous = commandDto.CustomerAnonymous;

                if (existingCommand.TableRestaurantId == null)
                    existingCommand.SeatCount = null;
                else if (commandDto.SeatCount.HasValue)
                    existingCommand.SeatCount = commandDto.SeatCount;


                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return existingCommand;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }

            //using var transaction = _context.Database.BeginTransaction();
            //bool result = false;

            //try
            //{
            //    decimal totalOrderPrice = 0;

            //    var dishIds = command.CommandDetailsCollection.Select(c => c.DishId).ToList();
            //    var dishPrices = await _context.Dish.Where(d => dishIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.Price);

            //    foreach (var c in command.CommandDetailsCollection)
            //    {
            //        c.DishPrice = dishPrices[c.DishId];
            //        c.OrderPrice = c.DishPrice * c.DishQuantity;
            //        totalOrderPrice += c.OrderPrice;
            //    }

            //    command.TotalOrderPrice = totalOrderPrice;

            //    _context.Entry(command).State = EntityState.Modified;
            //    await _context.SaveChangesAsync();

            //    transaction.Commit();

            //    result = true;
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex);
            //    transaction.Rollback();
            //}

            //return result;
        }

        public async Task<bool> DeleteCommand(Command command)
        {
            bool result = false;

            try
            {
                _context.Command.Remove(command);
                await _context.SaveChangesAsync();

                result = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return result;
        }

        public async Task<bool> PrepareCommand(Command command)
        {
            bool result = false;

            try
            {
                command.CommandStateId = (int)CommandStateEnum.Prepared;
                _context.Entry(command).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                result = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return result;
        }

        public async Task<bool> PayCommand(Command command)
        {
            bool result = false;

            try
            {
                command.CommandStateId = (int)CommandStateEnum.Paid;
                _context.Entry(command).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                result = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return result;
        }
        public async Task<int> Count(Expression<Func<Command, bool>> predicate = null)
        {
            return await (predicate != null ? _context.Command.CountAsync(predicate) : _context.Command.CountAsync());
        }

        public async Task<int> CommandDetailsCount(Expression<Func<CommandDetails, bool>> predicate = null)
        {
            return await (predicate != null ? _context.CommandDetails.CountAsync(predicate) : _context.CommandDetails.CountAsync());
        }

        public async Task<List<TableRestaurantWithCommand>> GetCommandCollectionWithoutTable(string role)
        {
            List<TableRestaurantWithCommand> collection = new();
            var commands = await _context.Command.Where(c => c.TableRestaurantId == null && c.CommandStateId != (int)CommandStateEnum.Paid)
                .Include(c => c.Employee.User)
                .Include(c => c.Employee.Role)
                .Include(c => c.CommandState)
                .Include(c => c.CommandDetailsCollection).ToListAsync();
            string[] roles = { "Cajero", "Cocinero" };

            foreach (var command in commands)
            {
                TableRestaurantWithCommand tableWithCommand = new();
                tableWithCommand.Table = null;

                if (command != null)
                {
                    if (role == "Cajero" && command.CommandStateId != (int)CommandStateEnum.Prepared)
                    {
                        continue;
                    }

                    tableWithCommand.Command = command.Adapt<CommandForTable>();
                    tableWithCommand.Command.QuantityOfDish = command.CommandDetailsCollection.Sum(cd => cd.DishQuantity);
                }
                else
                {
                    if (roles.Contains(role))
                    {
                        continue;
                    }
                }

                collection.Add(tableWithCommand);
            }

            return collection;
        }
    }
}
