// Navy.Core.Engine
// Pure C# - NO UnityEngine dependency

using System;
using System.Collections.Generic;
using Navy.Core.Models;

namespace Navy.Core.Engine
{
    /// <summary>
    /// Static game rules: map configuration factory, map conflict resolution, win condition.
    /// </summary>
    public static class GameRules
    {
        public const int TurnTimerSeconds = 300;        // FR-GP-05: 5 minutes
        public const int TurnWarningYellowSec = 60;     // FR-GP-06
        public const int TurnWarningRedSec = 30;
        public const int TurnWarningBlinkSec = 10;

        // Single shared RNG; constructing `new Random()` in quick succession
        // produces identical seeds and identical results.
        private static readonly Random _rng = new Random();

        // ─── Map configs ─────────────────────────────────────────────────────────

        public static MapConfig GetConfig(MapType mapType)
        {
            return mapType switch
            {
                MapType.Small  => SmallConfig(),
                MapType.Medium => MediumConfig(),
                MapType.Large  => LargeConfig(),
                _ => throw new ArgumentOutOfRangeException(nameof(mapType))
            };
        }

        private static MapConfig SmallConfig() => new MapConfig
        {
            Type       = MapType.Small,
            BoardSize  = 8,
            Ships      = new List<ShipGroup>
            {
                new ShipGroup { Decks = 3, Count = 1 },
                new ShipGroup { Decks = 2, Count = 2 },
                new ShipGroup { Decks = 1, Count = 3 }
            },
            TotalShips = 6,
            TotalCells = 10
        };

        private static MapConfig MediumConfig() => new MapConfig
        {
            Type       = MapType.Medium,
            BoardSize  = 10,
            Ships      = new List<ShipGroup>
            {
                new ShipGroup { Decks = 4, Count = 1 },
                new ShipGroup { Decks = 3, Count = 2 },
                new ShipGroup { Decks = 2, Count = 3 },
                new ShipGroup { Decks = 1, Count = 4 }
            },
            TotalShips = 10,
            TotalCells = 20
        };

        private static MapConfig LargeConfig() => new MapConfig
        {
            Type       = MapType.Large,
            BoardSize  = 12,
            Ships      = new List<ShipGroup>
            {
                new ShipGroup { Decks = 5, Count = 1 },
                new ShipGroup { Decks = 4, Count = 2 },
                new ShipGroup { Decks = 3, Count = 3 },
                new ShipGroup { Decks = 2, Count = 4 },
                new ShipGroup { Decks = 1, Count = 5 }
            },
            TotalShips = 15,
            TotalCells = 30
        };

        // ─── Map conflict resolution (FR-MP) ─────────────────────────────────────

        /// <summary>
        /// When both players chose the same map type, that one is used.
        /// Otherwise, one of the two choices is picked randomly.
        /// </summary>
        public static MapType ResolveMapConflict(MapType hostChoice, MapType guestChoice)
        {
            if (hostChoice == guestChoice) return hostChoice;
            return (_rng.Next(2) == 0) ? hostChoice : guestChoice;
        }

        /// <summary>
        /// Picks the first player to move at random (FR-GP-01).
        /// </summary>
        public static string PickFirstTurnUid(string hostUid, string guestUid)
        {
            return _rng.Next(2) == 0 ? hostUid : guestUid;
        }

        // ─── Win condition (FR-GP-09, FR-GP-10) ──────────────────────────────────

        /// <summary>
        /// Checks normal win condition: all ships of a player sunk.
        /// Returns the winner's UID, or null if the game is still ongoing.
        /// </summary>
        public static string? CheckWinCondition(GameState state)
        {
            if (state.Host.Board.AliveShipsCount() == 0) return state.Guest.Uid;
            if (state.Guest.Board.AliveShipsCount() == 0) return state.Host.Uid;
            return null;
        }

        /// <summary>
        /// Determines winner by hit count for early exit (FR-GP-10).
        /// Returns (winnerUid, isDraw).
        /// </summary>
        public static (string? winnerUid, bool isDraw) DetermineWinnerByHits(GameState state)
        {
            if (state.Host.Hits > state.Guest.Hits)
                return (state.Host.Uid, false);
            if (state.Guest.Hits > state.Host.Hits)
                return (state.Guest.Uid, false);
            return (null, true);
        }
    }
}
