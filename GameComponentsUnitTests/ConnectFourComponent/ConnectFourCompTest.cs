using Emne9_Prosjekt.GameComponents;

namespace GameComponentsUnitTests.ConnectFourComponent;

public class ConnectFourCompTest
{
    [Fact]
    public void CombineHorizontal_ShouldReturnCorrectTurnValues()
    {
        // Arrange
        var game = new Connect4Components();
        game.CreateBoard();

        // Drop 4 brikker på samme rad (rad F), kolonne 1 to 4
        game.DropPiece("F1"); // lander på  F1
        game.DropPiece("F2"); // lander på F2
        game.DropPiece("F3"); // lander på F3
        game.DropPiece("F4"); // lander på F4
        
        // Act
        var result = game.CombineHorizontal(); 

        // Assert
        
        var expectedKeys = new[] { "F1", "F2", "F3", "F4" };
        foreach (var key in expectedKeys)
        {
            Assert.True(result.ContainsKey(key), $"Expected key '{key}' not found in result.");
        } 
        
        //Sjekker verdiene dersom turbasering er konsistent
        // Player 1 (turnOrder == true) førstemann - i page sammenheng vil det da være første connection, player 2 (turnOrder == false) andremann, andre connection.
        Assert.Equal(1, result["F1"]); // Player 1 (first drop)
        Assert.Equal(2, result["F2"]); // Player 2 (second drop)
        Assert.Equal(1, result["F3"]); // Player 1 (third drop)
        Assert.Equal(2, result["F4"]); // Player 2 (fourth drop)
    }
    
    
    [Fact]
    public void CombineHorizontal_CheckRowValues()
    {
        // Arrange
        var game = new Connect4Components();
        game.CreateBoard();

        // Drop 4 brikker på samme rad (rad F), kolonne 1 to 7
        game.DropPiece("F4"); // lander på  F4
        
        // Act
        var result = game.CombineHorizontal();

        // Assert
        var expectedKeys = new[] { "F1", "F2", "F3", "F4", "F5", "F6", "F7" };
        foreach (var key in expectedKeys)
        {
            Assert.True(result.ContainsKey(key), $"Expected key '{key}' not found in result.");
        }
        
        Assert.Equal(0, result["F1"]); 
        Assert.Equal(0, result["F2"]); 
        Assert.Equal(0, result["F3"]); 
        Assert.Equal(1, result["F4"]); //Player 1 (first drop)
        Assert.Equal(0, result["F5"]); 
        Assert.Equal(0, result["F6"]); 
        Assert.Equal(0, result["F7"]); 
    }
    
}