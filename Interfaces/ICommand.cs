using proyecto_backend.Models;
using proyecto_backend.Schemas;
using System.Linq.Expressions;

namespace proyecto_backend.Interfaces
{
    public interface ICommand
    {
        public Task<List<Command>> GetAll();
        public Task<Command> GetById(int id);
        public Task<Command> CreateCommand(Command command, int? tableId, int? seatCount);
        public Task<Command> UpdateCommand(int id, CommandUpdate commandDto);
        public Task<bool> DeleteCommand(Command command);
        public Task<bool> PrepareCommand(Command command);
        public Task<bool> PayCommand(Command command);
        public Task<int> Count(Expression<Func<Command, bool>> predicate = null);
        public Task<int> CommandDetailsCount(Expression<Func<CommandDetails, bool>> predicate = null);
        public Task<List<TableRestaurantWithCommand>> GetCommandCollectionWithoutTable(string role);
    }
}
