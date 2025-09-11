using System.Numerics;

namespace ProjectDawnApi
{
    public static class DbInitializer
    {
        public static void Initialize(ProjectDawnDbContext context)
        {
            if (context.Farms.Any())
                return;

            // Players
            var player1 = new PlayerDataModel { Name = "Alice" };
            var player2 = new PlayerDataModel { Name = "Bob" };
            var player3 = new PlayerDataModel { Name = "Charlie" };
            context.Players.AddRange(player1, player2, player3);
            context.SaveChanges();

            // Farms
            var farm1 = new FarmDataModel { Name = "Sunny Meadow Farm", OwnerId = player1.Id };
            var farm2 = new FarmDataModel { Name = "Bob's Grand Orchard", OwnerId = player2.Id };
            context.Farms.AddRange(farm1, farm2);
            context.SaveChanges();

            // Objects on farm1
            context.PlacedObjects.AddRange(
                new PlacedObjectDataModel { FarmId = farm1.Id, Type = "Barn", PositionX = 10f, PositionY = 0f, PositionZ = -5f, RotationY = 90f },
                new PlacedObjectDataModel { FarmId = farm1.Id, Type = "Fence", PositionX = 15f, PositionY = 0f, PositionZ = 2f, RotationY = 0f }
            );

            // Objects on farm2
            context.PlacedObjects.AddRange(
                new PlacedObjectDataModel { FarmId = farm2.Id, Type = "AppleTree", PositionX = 3f, PositionY = 0f, PositionZ = 7f, RotationY = 45f },
                new PlacedObjectDataModel { FarmId = farm2.Id, Type = "Scarecrow", PositionX = -2f, PositionY = 0f, PositionZ = 4f, RotationY = 0f }
            );

            context.SaveChanges();

            // Seed initial visitors
            context.FarmVisitors.AddRange(
                new FarmVisitorDataModel { FarmId = farm1.Id, PlayerId = player3.Id, LastPositionX = 5f, LastPositionY = 0f, LastPositionZ = 5f },
                new FarmVisitorDataModel { FarmId = farm2.Id, PlayerId = player1.Id, LastPositionX = 1f, LastPositionY = 0f, LastPositionZ = 2f }
            );

            context.SaveChanges();
        }
    }
}
