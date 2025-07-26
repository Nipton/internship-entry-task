namespace TicTacToeAPI.Exceptions
{
    public class PlayerValidationException : Exception
    {
        public PlayerValidationException(string message) : base(message) {}
    }
}
