namespace TicTacToeAPI.Exceptions
{
    public abstract class GameException : Exception
    {
        protected GameException(string message) : base(message) { }
    }
    public class GameNotFoundException : GameException
    {
        public GameNotFoundException(Guid gameId) : base($"Игра с идентификатором {gameId} не найдена.") { }
    }
    public class InvalidCoordinatesException : GameException
    {
        public InvalidCoordinatesException(int row, int col) : base($"Переданы некорректные значения координат. Координаты [{row}, {col}] выходят за пределы игрового поля.") { }
    }
    public class CellAlreadyTakenException : GameException
    {
        public CellAlreadyTakenException(int row, int col) : base($"Ячейка [{row}, {col}] уже занята.") { }
    }
    public class WrongTurnException : GameException
    {
        public WrongTurnException(string playerName) : base($"Сейчас не очередь игрока {playerName}.") { }
    }
    public class GameAlreadyFinishedException : GameException
    {
        public GameAlreadyFinishedException() : base("Игра уже завершена. Ход невозможен.") { }
    }
    public class GameConflictException : GameException
    {
        public GameConflictException(string message) : base( message) { }
    }
    public class GameValidationException : GameException
    {
        public GameValidationException(string message) : base(message) { }
    }
}
