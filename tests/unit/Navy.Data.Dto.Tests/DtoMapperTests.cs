using System.Collections.Generic;
using FluentAssertions;
using Navy.Core.Models;
using Navy.Data.Firebase.Dto;
using Xunit;

namespace Navy.Data.Dto.Tests
{
    public sealed class DtoMapperTests
    {
        // ─── MapType ──────────────────────────────────────────────────────────────

        [Theory]
        [InlineData(MapType.Small, "Small")]
        [InlineData(MapType.Medium, "Medium")]
        [InlineData(MapType.Large, "Large")]
        public void MapType_ToDto_RoundTrip(MapType type, string expected)
        {
            DtoMapper.ToDto(type).Should().Be(expected);
            DtoMapper.MapTypeFromDto(expected).Should().Be(type);
        }

        [Fact]
        public void MapTypeFromDtoNullable_NullString_ReturnsNull()
        {
            DtoMapper.MapTypeFromDtoNullable(null).Should().BeNull();
        }

        [Fact]
        public void MapTypeFromDtoNullable_EmptyString_ReturnsNull()
        {
            DtoMapper.MapTypeFromDtoNullable("").Should().BeNull();
        }

        [Fact]
        public void MapTypeFromDtoNullable_ValidString_ReturnsEnum()
        {
            DtoMapper.MapTypeFromDtoNullable("Large").Should().Be(MapType.Large);
        }

        // ─── GamePhase ────────────────────────────────────────────────────────────

        [Theory]
        [InlineData(GamePhase.Lobby, "Lobby")]
        [InlineData(GamePhase.Setup, "Setup")]
        [InlineData(GamePhase.Playing, "Playing")]
        [InlineData(GamePhase.Finished, "Finished")]
        public void GamePhase_ToDto_RoundTrip(GamePhase phase, string expected)
        {
            DtoMapper.ToDto(phase).Should().Be(expected);
            DtoMapper.GamePhaseFromDto(expected).Should().Be(phase);
        }

        // ─── ShotResult ───────────────────────────────────────────────────────────

        [Theory]
        [InlineData(ShotResult.Miss, "Miss")]
        [InlineData(ShotResult.Hit, "Hit")]
        [InlineData(ShotResult.Sunk, "Sunk")]
        public void ShotResult_ToDto_RoundTrip(ShotResult result, string expected)
        {
            DtoMapper.ToDto(result).Should().Be(expected);
            DtoMapper.ShotResultFromDto(expected).Should().Be(result);
        }

        // ─── Cell / CellDto ───────────────────────────────────────────────────────

        [Fact]
        public void Cell_ToDto_RoundTrip()
        {
            var cell = new Cell(3, 7);
            var dto = DtoMapper.ToDto(cell);
            dto.x.Should().Be(3);
            dto.y.Should().Be(7);
            var back = DtoMapper.FromDto(dto);
            back.X.Should().Be(3);
            back.Y.Should().Be(7);
        }

        [Fact]
        public void CellList_ToDtoList_RoundTrip()
        {
            var cells = new List<Cell> { new Cell(1, 2), new Cell(3, 4) };
            var dtos = DtoMapper.ToDtoList(cells);
            dtos.Should().HaveCount(2);
            var back = DtoMapper.FromDtoList(dtos);
            back[0].X.Should().Be(1); back[0].Y.Should().Be(2);
            back[1].X.Should().Be(3); back[1].Y.Should().Be(4);
        }

        [Fact]
        public void ToDtoList_NullInput_ReturnsNull()
        {
            DtoMapper.ToDtoList(null).Should().BeNull();
        }

        [Fact]
        public void FromDtoList_NullInput_ReturnsEmptyList()
        {
            DtoMapper.FromDtoList(null).Should().BeEmpty();
        }

        // ─── ShotDto → ShotRecord ─────────────────────────────────────────────────

        [Fact]
        public void ShotDto_FromDto_MissRoundTrip()
        {
            var dto = new ShotDto
            {
                shooterUid = "host",
                targetUid = "guest",
                x = 3, y = 5,
                result = "Miss",
                timestampMs = 12345678L,
                sunkShipCells = null,
                adjacentMissCells = null
            };
            var record = DtoMapper.FromDto(dto);
            record.ShooterUid.Should().Be("host");
            record.TargetUid.Should().Be("guest");
            record.Coordinate.X.Should().Be(3);
            record.Coordinate.Y.Should().Be(5);
            record.Result.Should().Be(ShotResult.Miss);
            record.TimestampMs.Should().Be(12345678L);
            record.SunkShipCells.Should().BeEmpty();
            record.AdjacentMissCells.Should().BeEmpty();
        }

        [Fact]
        public void ShotDto_FromDto_SunkRoundTrip()
        {
            var dto = new ShotDto
            {
                shooterUid = "host",
                targetUid = "guest",
                x = 0, y = 0,
                result = "Sunk",
                timestampMs = 999L,
                sunkShipCells = new List<CellDto> { new CellDto { x = 0, y = 0 } },
                adjacentMissCells = new List<CellDto> { new CellDto { x = 1, y = 0 } }
            };
            var record = DtoMapper.FromDto(dto);
            record.Result.Should().Be(ShotResult.Sunk);
            record.SunkShipCells.Should().HaveCount(1);
            record.AdjacentMissCells.Should().HaveCount(1);
        }

        [Fact]
        public void ShotDto_FromDto_NullUids_DefaultToEmpty()
        {
            var dto = new ShotDto { shooterUid = null, targetUid = null, result = "Hit", x = 0, y = 0 };
            var record = DtoMapper.FromDto(dto);
            record.ShooterUid.Should().Be("");
            record.TargetUid.Should().Be("");
        }
    }
}
