using FakeNewsScamDetector.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FakeNewsScamDetector.Data.SeedData;

public static class ScamPatternSeed
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScamPattern>().HasData(
            new ScamPattern { Id = 1, Pattern = "wire transfer", Description = "Requests wire transfer payment", WeightScore = 0.25 },
            new ScamPattern { Id = 2, Pattern = "gift card", Description = "Requests gift card payment", WeightScore = 0.25 },
            new ScamPattern { Id = 3, Pattern = "lottery", Description = "Unsolicited lottery/prize win", WeightScore = 0.30 },
            new ScamPattern { Id = 4, Pattern = "inheritance", Description = "Unsolicited inheritance claim", WeightScore = 0.30 }
        );
    }
}
