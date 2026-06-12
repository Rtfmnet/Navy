using Navy.Core.Models;

namespace Navy.Core.TestKit.Builders
{
    /// <summary>
    /// Fluent builder for Ship.
    /// </summary>
    public sealed class ShipBuilder
    {
        private int _decks = 1;
        private ShipOrientation _orientation = ShipOrientation.Horizontal;
        private Cell _origin = new Cell(0, 0);

        public ShipBuilder WithDecks(int decks) { _decks = decks; return this; }
        public ShipBuilder At(int x, int y) { _origin = new Cell(x, y); return this; }
        public ShipBuilder Horizontal() { _orientation = ShipOrientation.Horizontal; return this; }
        public ShipBuilder Vertical() { _orientation = ShipOrientation.Vertical; return this; }

        public Ship Build() => new Ship(_decks, _orientation, _origin);
    }
}
