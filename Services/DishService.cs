using Microsoft.EntityFrameworkCore;
using proyecto_backend.Data;
using proyecto_backend.Dto;
using proyecto_backend.Interfaces;
using proyecto_backend.Models;
using System.Linq.Expressions;

namespace proyecto_backend.Services
{
    public class DishService : IDish
    {
        private readonly CommandContext _context;
        private string imageNotFoundDefault = "https://placehold.co/400";

        public DishService(CommandContext context)
        {
            _context = context;
        }

        public async Task<bool> CreateDish(Dish Dish)
        {
            bool result = false;

            try
            {
                var listDish = await _context.Dish.ToListAsync();

                Dish.Id = Dish.GenerateId(listDish);
                _context.Dish.Add(Dish);
                await _context.SaveChangesAsync();

                result = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return result;
        }

        public async Task<bool> DeteleDish(Dish Dish)
        {
            bool result = false;

            try
            {
                _context.Dish.Remove(Dish);
                await _context.SaveChangesAsync();

                result = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return result;
        }

        public async Task<List<Dish>> GetAll()
        {
            List<Dish> listDish = await _context.Dish
                .Include(d => d.Category)
                .ToListAsync();



            foreach (var item in listDish)
            {
                if (string.IsNullOrEmpty(item.Image))
                {
                    item.Image = imageNotFoundDefault;
                }
            }


            return listDish;
        }

        public async Task<Dish> GetById(string id)
        {
            var dish = await _context.Dish
                    .Include(d => d.Category)
                   .FirstOrDefaultAsync(d => d.Id == id);

            if (dish != null)
            {
                if (string.IsNullOrEmpty(dish.Image))
                {
                    dish.Image = imageNotFoundDefault;
                }
            }

            return dish;
        }

        public async Task<List<Dish>> GetExtras()
        {
            var dish = await _context.Dish
                .Where(x => x.CategoryId == "C-003" && x.Active == true)
                .ToListAsync();


            foreach (var item in dish)
            {
                if (string.IsNullOrEmpty(item.Image))
                {
                    item.Image = imageNotFoundDefault;
                }
            }

            return dish;
        }

        public async Task<bool> UpdateDish(Dish Dish)
        {
            bool result = false;

            try
            {
                _context.Entry(Dish).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                result = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return result;
        }

        public async Task<List<Dish>> GetDishByIdCategory(string id)
        {
            List<Dish> listDish = await _context.Dish
                //.Where(x => x.Active == true)
               .Include(d => d.Category)
               //.Where(d => d.CategoryId == id)
               .ToListAsync();

            if (!string.IsNullOrEmpty(id) && !id.Equals("todos"))
            {
                listDish = listDish.Where(d => d.CategoryId == id).ToList();
            }

            foreach (var item in listDish)
            {
                if (string.IsNullOrEmpty(item.Image))
                {
                    item.Image = imageNotFoundDefault;
                }
            }

            return listDish;
        }

        public async Task<int> Count(Expression<Func<Dish, bool>> predicate = null)
        {
            return await (predicate != null ? _context.Dish.CountAsync(predicate) : _context.Dish.CountAsync());
        }

        public async Task<List<DishOrderStatistics>> GetDishOrderStatistics()
        {
            var regularDishes = from dc in _context.CommandDetails
                                join c in _context.Command on dc.CommandId equals c.Id
                                join d in _context.Dish on dc.DishId equals d.Id
                                //join ct in _context.Category on d.CategoryId equals ct.Id
                                where c.CommandStateId == 3 && d.Active == true && d.CategoryId != "C-003"
                                select new 
                                { 
                                    DishId = dc.DishId, 
                                    //DishName = d.Name, 
                                    //DishImage = d.Image, 
                                    //CategoryName = ct.Name, 
                                    Quantity = dc.DishQuantity,
                                    TotalPrice =  (double)(d.Price * dc.DishQuantity)
                                };

            var extraDishes = from de in _context.CommandDetailsExtras
                              join dc in _context.CommandDetails on de.CommandDetailsId equals dc.Id
                              join c in _context.Command on dc.CommandId equals c.Id
                              join d in _context.Dish on de.ExtraDishId equals d.Id
                              //join ct in _context.Category on d.CategoryId equals ct.Id
                              where c.CommandStateId == 3 && d.Active == true && d.CategoryId == "C-003"
                              select new 
                              { 
                                  DishId = de.ExtraDishId, 
                                  //DishName = d.Name, 
                                  //DishImage = d.Image, 
                                  //CategoryName = ct.Name, 
                                  Quantity = de.Quantity,
                                  TotalPrice = (double)(d.Price * de.Quantity)
                              };

            var combinedQuery = regularDishes.Concat(extraDishes);

            var groupedQuery = from item in combinedQuery
                               group item by new { item.DishId } into g
                               orderby g.Sum(x => x.TotalPrice) descending
                               select new DishOrderStatistics
                               {
                                   DishId = g.Key.DishId,
                                   //Name = g.Key.DishName,
                                   //ImgDish = g.Key.DishImage,
                                   //Category = g.Key.CategoryName,
                                   TotalSales = (decimal)g.Sum(x => x.TotalPrice),
                                   QuantityOfDishesSold = g.Sum(x => x.Quantity)
                               };

            var result = await groupedQuery.ToListAsync();

            var category = await _context.Category.ToListAsync();
            var dish = await _context.Dish.ToListAsync();

            foreach (var item in result)
            {
                var dishItem = dish.FirstOrDefault(x => x.Id == item.DishId);
                if (dishItem != null)
                {
                    item.Name = dishItem.Name;
                    item.ImgDish = dishItem.Image;
                    item.Category = category.FirstOrDefault(x => x.Id == dishItem.CategoryId)?.Name;
                }
            }
            



            return result;
        }

        public async Task<bool> IsNameUnique(string name, string dishId = null)
        {
            if (dishId != null)
            {
                return await _context.Dish.AllAsync(e => e.Name != name || e.Id == dishId);
            }

            return await _context.Dish.AllAsync(e => e.Name != name);
        }
    }
}