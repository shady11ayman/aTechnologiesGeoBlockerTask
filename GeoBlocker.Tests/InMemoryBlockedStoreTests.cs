using System;
using System.Linq;
using GeoBlocker.Infrastructure.Repositories;
using GeoBlocker.Application.Interfaces;
using FluentAssertions; 
using Xunit;

namespace GeoBlocker.Tests
{
    public class InMemoryBlockedStoreTests
    {
        private readonly IBlockedStore _store;

        public InMemoryBlockedStoreTests()
        {
            _store = new InMemoryBlockedStore();
        }

        [Fact]
        public void AddPermanent_ShouldAddCountry_WhenNotAlreadyBlocked()
        {
            // Arrange
            var code = "US";
            var name = "United States";

            // Act
            var added = _store.AddPermanent(code, name);

            // Assert
            added.Should().BeTrue("it should add the country if not already present");
            _store.IsBlocked(code).Should().BeTrue("the country should now be blocked");
        }

        [Fact]
        public void AddPermanent_ShouldFail_WhenAlreadyPermanentBlocked()
        {
            // Arrange
            var code = "US";
            var name = "United States";
            _store.AddPermanent(code, name);

            // Act
            var secondAttempt = _store.AddPermanent(code, name);

            // Assert
            secondAttempt.Should().BeFalse("it should not add a duplicate block");
        }

        [Fact]
        public void RemoveBlocked_ShouldRemoveCountry_FromPermanentBlock()
        {
            // Arrange
            var code = "US";
            var name = "United States";
            _store.AddPermanent(code, name);

            // Act
            var removed = _store.RemoveBlocked(code);

            // Assert
            removed.Should().BeTrue("we should be able to remove a permanently blocked country");
            _store.IsBlocked(code).Should().BeFalse("the country was removed");
        }

        [Fact]
        public void RemoveBlocked_ShouldReturnFalse_WhenNothingToRemove()
        {
            // Arrange
            var code = "XX";

            // Act
            var result = _store.RemoveBlocked(code);

            // Assert
            result.Should().BeFalse("the country was never blocked");
        }

        [Fact]
        public void AddTemporal_ShouldBlockCountry_WhenNotBlockedYet()
        {
            // Arrange
            var code = "CA";
            var name = "Canada";
            var durationMinutes = 10;

            // Act
            var success = _store.AddTemporal(code, name, durationMinutes, out var error);

            // Assert
            success.Should().BeTrue();
            error.Should().BeEmpty();
            _store.IsBlocked(code).Should().BeTrue();
        }

        [Fact]
        public void AddTemporal_ShouldFail_WhenCountryIsPermanentBlocked()
        {
            // Arrange
            var code = "CA";
            var name = "Canada";
            _store.AddPermanent(code, name);

            // Act
            var success = _store.AddTemporal(code, name, 30, out var error);

            // Assert
            success.Should().BeFalse();
            error.Should().Contain("already permanently blocked");
        }

        [Fact]
        public void AddTemporal_ShouldFail_WhenCountryIsAlreadyTemporallyBlocked()
        {
            // Arrange
            var code = "CA";
            var name = "Canada";
            _store.AddTemporal(code, name, 30, out _);

            // Act
            var attempt2 = _store.AddTemporal(code, name, 30, out var error);

            // Assert
            attempt2.Should().BeFalse();
            error.Should().Contain("already temporally blocked");
        }

        [Fact]
        public void RemoveExpiredTemporal_ShouldUnblock_WhenExpired()
        {
            // Arrange
            var code = "CA";
            var name = "Canada";
            // 0 minutes => effectively expired right away
            _store.AddTemporal(code, name, 0, out _);

            // Act
            _store.RemoveExpiredTemporal();

            // Assert
            _store.IsBlocked(code).Should().BeFalse("the block should have expired immediately");
        }

        [Fact]
        public void GetAllCurrentlyBlockedDetails_ShouldIncludePermanentAndActiveTemp()
        {
            // Arrange
            // Permanent
            _store.AddPermanent("US", "United States");
            _store.AddPermanent("GB", "United Kingdom");
            // Temporal
            _store.AddTemporal("EG", "Egypt", 60, out _);
            _store.AddTemporal("JP", "Japan", 60, out _);

            // Act
            var blocked = _store.GetAllCurrentlyBlockedDetails().ToList();

            // Assert
            blocked.Should().HaveCount(4);
            blocked.Should().Contain(x => x.CountryCode == "US" && !x.IsTemporary);
            blocked.Should().Contain(x => x.CountryCode == "EG" && x.IsTemporary);
        }
    }
}
