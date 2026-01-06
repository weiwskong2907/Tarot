using Microsoft.EntityFrameworkCore;
using Tarot.Core.Entities;
using Tarot.Infrastructure.Data;
using Xunit;

namespace Tarot.Tests;

public class DbInitializerTests
{
    [Fact]
    public async Task ReloadCardsAsync_ShouldLoadFromJson()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        
        // Create temp config dir and json file
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var configDir = Path.Combine(baseDir, "config");
        if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);
        
        var jsonPath = Path.Combine(configDir, "cards.json");
        var csvPath = Path.Combine(configDir, "cards.csv");
        
        // Backup existing if any
        if (File.Exists(jsonPath)) File.Move(jsonPath, jsonPath + ".bak");
        if (File.Exists(csvPath)) File.Move(csvPath, csvPath + ".bak");

        try 
        {
            var jsonContent = @"[
                { ""name"": ""Test Card JSON"", ""suit"": ""Wands"", ""arcanaType"": ""Minor"", ""meaningUpright"": ""Test"" }
            ]";
            await File.WriteAllTextAsync(jsonPath, jsonContent);

            // Act
            var result = await DbInitializer.ReloadCardsAsync(context);

            // Assert
            Assert.True(result);
            var card = await context.Cards.FirstOrDefaultAsync(c => c.Name == "Test Card JSON");
            Assert.NotNull(card);
            Assert.Equal("Wands", card.Suit.ToString());
        }
        finally
        {
            // Cleanup and restore
            if (File.Exists(jsonPath)) File.Delete(jsonPath);
            if (File.Exists(jsonPath + ".bak")) File.Move(jsonPath + ".bak", jsonPath);
            if (File.Exists(csvPath + ".bak")) File.Move(csvPath + ".bak", csvPath);
        }
    }

    [Fact]
    public async Task ReloadCardsAsync_ShouldLoadFromCsv_IfJsonMissing()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var configDir = Path.Combine(baseDir, "config");
        if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);
        
        var jsonPath = Path.Combine(configDir, "cards.json");
        var csvPath = Path.Combine(configDir, "cards.csv");

        // Backup existing
        if (File.Exists(jsonPath)) File.Move(jsonPath, jsonPath + ".bak");
        if (File.Exists(csvPath)) File.Move(csvPath, csvPath + ".bak");

        try
        {
            // Ensure no JSON
            // Create CSV
            var csvContent = "Name,Suit,ArcanaType,MeaningUpright,MeaningReversed,ImageUrl,Keywords\n" +
                             "Test Card CSV,Cups,Minor,Test Meaning,,,\n";
            await File.WriteAllTextAsync(csvPath, csvContent);

            // Act
            var result = await DbInitializer.ReloadCardsAsync(context);

            // Assert
            Assert.True(result);
            var card = await context.Cards.FirstOrDefaultAsync(c => c.Name == "Test Card CSV");
            Assert.NotNull(card);
            Assert.Equal("Cups", card.Suit.ToString());
        }
        finally
        {
            // Cleanup
            if (File.Exists(csvPath)) File.Delete(csvPath);
            if (File.Exists(jsonPath + ".bak")) File.Move(jsonPath + ".bak", jsonPath);
            if (File.Exists(csvPath + ".bak")) File.Move(csvPath + ".bak", csvPath);
        }
    }
}
