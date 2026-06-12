using Navy.Core.Engine;
using Navy.Core.Models;

namespace Navy.Core.TestKit.Builders
{
    /// <summary>
    /// Fluent builder for Board with ships.
    /// </summary>
    public sealed class BoardBuilder
    {
        private readonly MapConfig _config;
        private readonly Board _board;

        public BoardBuilder(MapType mapType)
        {
            _config = GameRules.GetConfig(mapType);
            _board = new Board(_config.BoardSize);
        }

        public BoardBuilder(int size)
        {
            _config = null!;
            _board = new Board(size);
        }

        public BoardBuilder WithShip(int decks, int x, int y, ShipOrientation orientation = ShipOrientation.Horizontal)
        {
            var ship = new Ship(decks, orientation, new Cell(x, y));
            _board.AddShip(ship);
            foreach (var cell in ship.GetCells())
                _board.SetCell(cell, CellState.Ship);
            return this;
        }

        public Board Build() => _board;
    }
}
