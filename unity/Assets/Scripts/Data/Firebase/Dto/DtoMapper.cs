// Navy.Data.Firebase.Dto
// Mapper between Core domain models and Firebase DTOs

using System;
using System.Collections.Generic;
using Navy.Core.Models;

namespace Navy.Data.Firebase.Dto
{
    public static class DtoMapper
    {
        // ─── MapType ──────────────────────────────────────────────────────────────

        public static string ToDto(MapType t) => t.ToString();

        public static MapType MapTypeFromDto(string s) =>
            (MapType)Enum.Parse(typeof(MapType), s);

        public static MapType? MapTypeFromDtoNullable(string? s) =>
            string.IsNullOrEmpty(s) ? (MapType?)null : (MapType)Enum.Parse(typeof(MapType), s);

        // ─── GamePhase ────────────────────────────────────────────────────────────

        public static string ToDto(GamePhase p) => p.ToString();

        public static GamePhase GamePhaseFromDto(string s) =>
            (GamePhase)Enum.Parse(typeof(GamePhase), s);

        // ─── ShotResult ───────────────────────────────────────────────────────────

        public static string ToDto(ShotResult r) => r.ToString();

        public static ShotResult ShotResultFromDto(string s) =>
            (ShotResult)Enum.Parse(typeof(ShotResult), s);

        // ─── Cell ─────────────────────────────────────────────────────────────────

        public static CellDto ToDto(Cell c) => new CellDto { x = c.X, y = c.Y };

        public static Cell FromDto(CellDto d) => new Cell(d.x, d.y);

        public static List<CellDto>? ToDtoList(IReadOnlyList<Cell>? cells)
        {
            if (cells == null) return null;
            var list = new List<CellDto>(cells.Count);
            foreach (var c in cells) list.Add(ToDto(c));
            return list;
        }

        public static List<Cell> FromDtoList(List<CellDto>? dtos)
        {
            if (dtos == null) return new List<Cell>();
            var list = new List<Cell>(dtos.Count);
            foreach (var d in dtos) list.Add(FromDto(d));
            return list;
        }

        // ─── ShotDto → ShotRecord ─────────────────────────────────────────────────

        public static ShotRecord FromDto(ShotDto dto) => new ShotRecord
        {
            ShooterUid        = dto.shooterUid ?? "",
            TargetUid         = dto.targetUid ?? "",
            Coordinate        = new Cell(dto.x, dto.y),
            Result            = ShotResultFromDto(dto.result ?? "Miss"),
            TimestampMs       = dto.timestampMs,
            SunkShipCells     = FromDtoList(dto.sunkShipCells),
            AdjacentMissCells = FromDtoList(dto.adjacentMissCells)
        };
    }
}
