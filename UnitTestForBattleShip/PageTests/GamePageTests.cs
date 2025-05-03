using Emne9_Prosjekt.Hubs.Interfaces;
using Moq;
using UnitTestForBattleShip.PageTests.Components;

namespace UnitTestForBattleShip.PageTests;

public class GamePageTests
{
     [Fact]
        public async Task CheckGameOver_SendsGameOverTrue_WhenAllMyShipsAreSunk()
        {
            // Arrange
            var mockGameHub = new Mock<IGameHubConnection>();

            // Mock SendGameOverAsync
            mockGameHub.Setup(x => x.SendGameOverAsync(true))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var gamePage = new TestableGamePage
            {
                GameHubConnection = mockGameHub.Object,
                _playerBoard = new Dictionary<string, int>
                {
                    { "A1", -1 }, { "A2", -1 }
                },
                _opponentShipStatus = new Dictionary<string, bool>
                {
                    { "Ship1", false }
                },
                GetPlacedShips = () => new Dictionary<string, List<string>>
                {
                    { "Ship1", new List<string> { "A1", "A2" } }
                }
            };

            // Act
            await gamePage.CheckGameOver();

            // Assert
            mockGameHub.Verify(x => x.SendGameOverAsync(true), Times.Once);
            Assert.Equal(TestableGamePage.GameState.GameOver, gamePage._gameState);
            Assert.False(gamePage._isWinner);
            Assert.Equal("Du tapte! Alle dine skip er sunket.", gamePage._gameOverMessage);
        }

        [Fact]
        public async Task CheckGameOver_SendsGameOverFalse_WhenAllOpponentShipsAreSunk()
        {
            // Arrange
            var mockGameHub = new Mock<IGameHubConnection>();

            mockGameHub.Setup(x => x.SendGameOverAsync(false))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var gamePage = new TestableGamePage
            {
                GameHubConnection = mockGameHub.Object,
                _playerBoard = new Dictionary<string, int>
                {
                    { "B1", 1 }, { "B2", 1 }
                },
                _opponentShipStatus = new Dictionary<string, bool>
                {
                    { "OpponentShip1", true },
                    { "OpponentShip2", true }
                },
                GetPlacedShips = () => new Dictionary<string, List<string>>
                {
                    { "MyShip", new List<string> { "B1", "B2" } }
                }
            };

            // Act
            await gamePage.CheckGameOver();

            // Assert
            mockGameHub.Verify(x => x.SendGameOverAsync(false), Times.Once);
            Assert.Equal(TestableGamePage.GameState.GameOver, gamePage._gameState);
            Assert.True(gamePage._isWinner);
            Assert.Equal("Du vant! Alle motstanderens skip er sunket.", gamePage._gameOverMessage);
        }
}