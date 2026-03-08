using Microsoft.EntityFrameworkCore;
using proyecto_backend.Data;
using proyecto_backend.Dto;
using proyecto_backend.Interfaces;
using proyecto_backend.Models;
using System.Linq.Expressions;

namespace proyecto_backend.Services
{
    public class ReceiptService : IReceipt
    {
        private readonly CommandContext _context;

        public ReceiptService(CommandContext context)
        {
            _context = context;
        }

        public async Task<List<Receipt>> GetAll()
        {
            return await _context.Receipt
                .Include(x => x.Employee.User)
                .Include(x => x.Employee.Role)
                .Include(x => x.Cash)
                .ThenInclude(x => x.Establishment)
                .Include(x => x.Customer)
                .Include(x => x.ReceiptType)
                .Include(x => x.ReceiptDetailsCollection)
                .ThenInclude(x => x.PaymentMethod)
                .Include(x => x.Command)
                .Include(x => x.Command.TableRestaurant)
                .Include(x => x.Command.Employee)
                .Include(x => x.Command.Employee.User)
                .Include(x => x.Command.Employee.Role)
                .Include(x => x.Command.CommandState)
                .Include(x => x.Command.CommandDetailsCollection)
                .ThenInclude(x => x.Dish)
                .Include(x => x.Command.CommandDetailsCollection)
                .ThenInclude(x => x.Dish.Category)
                .Include(x => x.Command.CommandDetailsCollection)
                .ThenInclude(x => x.Extras)
                .ThenInclude(x => x.ExtraDish)
                .ToListAsync();
        }

        public async Task<Receipt> GetById(int id)
        {
            return await _context.Receipt
                .Include(x => x.Employee.User)
                .Include(x => x.Employee.Role)
                .Include(x => x.Cash)
                .ThenInclude(x => x.Establishment)
                .Include(x => x.Customer)
                .Include(x => x.ReceiptType)
                .Include(x => x.ReceiptDetailsCollection)
                .ThenInclude(x => x.PaymentMethod)
                .Include(x => x.Command)
                .Include(x => x.Command.TableRestaurant)
                .Include(x => x.Command.Employee)
                .Include(x => x.Command.Employee.User)
                .Include(x => x.Command.Employee.Role)
                .Include(x => x.Command.CommandState)
                .Include(x => x.Command.CommandDetailsCollection)
                .ThenInclude(x => x.Dish)
                .Include(x => x.Command.CommandDetailsCollection)
                .ThenInclude(x => x.Dish.Category)
                .Include(x => x.Command.CommandDetailsCollection)
                .ThenInclude(x => x.Extras)
                .ThenInclude(x => x.ExtraDish)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<bool> CreateReceipt(Receipt receipt)
        {
            using var transaction = _context.Database.BeginTransaction();
            bool result = false;

            try
            {
                _context.Receipt.Add(receipt);
                await _context.SaveChangesAsync();

                transaction.Commit();

                result = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                transaction.Rollback();
            }

            return result;
        }

        public async Task<int> Count(Expression<Func<Receipt, bool>> predicate = null)
        {
            return await (predicate != null ? _context.Receipt.CountAsync(predicate) : _context.Receipt.CountAsync());
        }

        public async Task<int> ReceiptDetailsCount(Expression<Func<ReceiptDetails, bool>> predicate = null)
        {
            return await (predicate != null ? _context.ReceiptDetails.CountAsync(predicate) : _context.ReceiptDetails.CountAsync());
        }

        public async Task<List<SalesDataPerDate>> GetSalesDataPerDate()
        {
            var receiptsAndDishes = await (from r in _context.Receipt
                                           select new
                                           {
                                               Date = r.CreatedAt.Date,
                                               r.Id,
                                               r.TotalPrice
                                           }).ToListAsync();

            var receiptIds = receiptsAndDishes.Select(r => r.Id).Distinct().ToList();

            var paymentMethods = await (from r in _context.Receipt
                                        where receiptIds.Contains(r.Id)
                                        join rd in _context.ReceiptDetails on r.Id equals rd.ReceiptId
                                        join pm in _context.PaymentMethod on rd.PaymentMethodId equals pm.Id
                                        select new
                                        {
                                            Date = r.CreatedAt.Date,
                                            ReceiptId = r.Id,
                                            PaymentMethodId = pm.Id,
                                            PaymentMethodName = pm.Name,
                                            Amount = rd.Amount
                                        }).ToListAsync();

            var dishesInfo = await (from r in _context.Receipt
                                    where receiptIds.Contains(r.Id)
                                    join c in _context.Command on r.CommandId equals c.Id
                                    join cd in _context.CommandDetails on c.Id equals cd.CommandId
                                    join d in _context.Dish on cd.DishId equals d.Id
                                    select new
                                    {
                                        Date = r.CreatedAt.Date,
                                        ReceiptId = r.Id,
                                        DishId = d.Id,
                                        DishName = d.Name,
                                        DishCategoryId = d.CategoryId,
                                        cd.DishQuantity,
                                        cd.DishPrice,
                                        cd.OrderPrice,
                                        c.TotalOrderPrice
                                    }).ToListAsync();

            var extrasInfo = await (from r in _context.Receipt
                                    where receiptIds.Contains(r.Id)
                                    join c in _context.Command on r.CommandId equals c.Id
                                    join cd in _context.CommandDetails on c.Id equals cd.CommandId
                                    join e in _context.CommandDetailsExtras on cd.Id equals e.CommandDetailsId
                                    join d in _context.Dish on e.ExtraDishId equals d.Id
                                    select new 
                                    {
                                        Date = r.CreatedAt.Date,
                                        ReceiptId = r.Id,
                                        ExtraId = d.Id,
                                        ExtraCategoryId = d.CategoryId,
                                        ExtraName = d.Name,
                                        ExtraQuantity = e.Quantity,
                                        ExtraPrice = d.Price,
                                        ExtraOrderPrice = d.Price * e.Quantity,
                                        c.TotalOrderPrice
                                    }).ToListAsync();

            var dailySalesAnalytics = receiptsAndDishes
                .GroupBy(x => x.Date)
                .Select(dailyGroup =>
                {
                    var dayPayments = paymentMethods
                        .Where(p => p.Date == dailyGroup.Key)
                        .ToList();

                    var dayPaymentsAgg = dayPayments
                        .GroupBy(p => p.PaymentMethodId)
                        .Select(pg => new AccumulatedPaymentsByDay
                        {
                            PaymentMethodId = pg.Key,
                            TotalAmount = pg.Sum(p => p.Amount)
                        })
                        .ToList();

                    var dayDishes = dishesInfo
                        .Where(d => d.Date == dailyGroup.Key)
                        .ToList();

                    var bestSellingDish = dayDishes
                        .GroupBy(d => d.DishName)
                        .OrderByDescending(g => g.Sum(x => x.DishQuantity))
                        .Select(g => g.Key)
                        .FirstOrDefault();

                    var soldDishesGroup = dayDishes
                        .GroupBy(d => d.DishId)
                        .Select(dg =>
                        {
                            var firstDish = dg.First();
                            var dishId = firstDish.DishId;
                            var dishName = firstDish.DishName;
                            var dishPrice = firstDish.DishPrice;
                            var dishCategoryId = firstDish.DishCategoryId;
                            int totalQty = dg.Sum(x => x.DishQuantity);
                            decimal totalAmt = dg.Sum(x => x.OrderPrice);

                            var paymentBreakdown = new Dictionary<int, DishPaymentMethodTotal>();
                            foreach (var dishOrder in dg)
                            {
                                var receiptPayments = dayPayments.Where(p => p.ReceiptId == dishOrder.ReceiptId).ToList();
                                foreach (var rp in receiptPayments)
                                {
                                    decimal proportion = dishOrder.TotalOrderPrice > 0 ? (dishOrder.OrderPrice / dishOrder.TotalOrderPrice) : 0;
                                    decimal amountForDish = rp.Amount * proportion;

                                    if (!paymentBreakdown.ContainsKey(rp.PaymentMethodId))
                                    {
                                        paymentBreakdown[rp.PaymentMethodId] = new DishPaymentMethodTotal
                                        {
                                            PaymentMethodId = rp.PaymentMethodId,
                                            PaymentMethodName = rp.PaymentMethodName,
                                            Amount = 0
                                        };
                                    }
                                    paymentBreakdown[rp.PaymentMethodId].Amount += amountForDish;
                                }
                            }

                            return new DishSoldData
                            {
                                DishId = dishId,
                                DishName = dishName,
                                Quantity = totalQty,
                                UnitPrice = dishPrice,
                                TotalAmount = totalAmt,
                                DishCategoryId = dishCategoryId,
                                PaymentMethodTotals = paymentBreakdown.Values.Select(v => new DishPaymentMethodTotal
                                {
                                    PaymentMethodId = v.PaymentMethodId,
                                    PaymentMethodName = v.PaymentMethodName,
                                    Amount = Math.Round(v.Amount, 2)
                                }).ToList()
                            };
                        }).ToList();

                    var soldExtrasGroup = extrasInfo
                        .Where(e => e.Date == dailyGroup.Key)
                        .GroupBy(e => e.ExtraId)
                        .Select(eg =>
                        {
                            var firstExtra = eg.First();
                            int totalQty = eg.Sum(x => x.ExtraQuantity);
                            decimal totalAmt = eg.Sum(x => x.ExtraOrderPrice);

                            var paymentBreakdown = new Dictionary<int, DishPaymentMethodTotal>();
                            foreach (var extraOrder in eg)
                            {
                                var receiptPayments = dayPayments.Where(p => p.ReceiptId == extraOrder.ReceiptId).ToList();
                                foreach (var rp in receiptPayments)
                                {
                                    decimal proportion = extraOrder.TotalOrderPrice > 0 ? (extraOrder.ExtraOrderPrice / extraOrder.TotalOrderPrice) : 0;
                                    decimal amountForExtra = rp.Amount * proportion;

                                    if (!paymentBreakdown.ContainsKey(rp.PaymentMethodId))
                                    {
                                        paymentBreakdown[rp.PaymentMethodId] = new DishPaymentMethodTotal
                                        {
                                            PaymentMethodId = rp.PaymentMethodId,
                                            PaymentMethodName = rp.PaymentMethodName,
                                            Amount = 0
                                        };
                                    }
                                    paymentBreakdown[rp.PaymentMethodId].Amount += amountForExtra;
                                }
                            }

                            return new ExtraSoldData
                            {
                                ExtraId = firstExtra.ExtraId,
                                ExtraCategoryId = firstExtra.ExtraCategoryId,
                                ExtraName = firstExtra.ExtraName,
                                Quantity = totalQty,
                                UnitPrice = firstExtra.ExtraPrice,
                                TotalAmount = totalAmt,
                                PaymentMethodTotals = paymentBreakdown.Values.Select(v => new DishPaymentMethodTotal
                                {
                                    PaymentMethodId = v.PaymentMethodId,
                                    PaymentMethodName = v.PaymentMethodName,
                                    Amount = Math.Round(v.Amount, 2)
                                }).ToList()
                            };
                        }).ToList();

                    int quantityOfDishSales = dayDishes.Sum(d => d.DishQuantity);

                    return new SalesDataPerDate
                    {
                        CreatedAt = dailyGroup.Key,
                        AccumulatedSales = dailyGroup.Sum(x => x.TotalPrice),
                        NumberOfGeneratedReceipts = dailyGroup.Select(x => x.Id).Distinct().Count(),
                        QuantityOfDishSales = quantityOfDishSales,
                        BestSellingDish = bestSellingDish,
                        accumulatedPaymentsByDays = dayPaymentsAgg,
                        SoldDishes = soldDishesGroup.OrderByDescending(x => x.Quantity).ToList(),
                        SoldExtras = soldExtrasGroup.OrderByDescending(x => x.Quantity).ToList()
                    };
                })
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            return dailySalesAnalytics;
        }
    }
}
