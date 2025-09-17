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
            var player4 = new PlayerDataModel { Name = "David" };
            var player5 = new PlayerDataModel { Name = "Eve" };
            var player6 = new PlayerDataModel { Name = "Frank" };
            var player7 = new PlayerDataModel { Name = "Grace" };
            var player8 = new PlayerDataModel { Name = "Hank" };
            var player9 = new PlayerDataModel { Name = "Ivy" };
            var player10 = new PlayerDataModel { Name = "Jack" };

            context.Players.AddRange(
                player1, player2, player3, player4, player5,
                player6, player7, player8, player9, player10
            );
            context.SaveChanges();


            // Farms
            var farm1 = new FarmDataModel { Name = "Sunny Meadow Farm", OwnerId = player1.Id };
            var farm2 = new FarmDataModel { Name = "Bob's Grand Orchard", OwnerId = player2.Id };
            context.Farms.AddRange(farm1, farm2);
            context.SaveChanges();

            // Objects on farm1
            context.PlacedObjects.AddRange(
                new PlacedObjectDataModel
                {
                    FarmId = farm1.Id,
                    Type = "Barn",
                    Transformation = new TransformationDataModel
                    {
                        positionX = 10f,
                        positionY = 0f,
                        positionZ = -5f,
                        rotationX = 0f,
                        rotationY = 90f,
                        rotationZ = 0f
                    }
                },
                new PlacedObjectDataModel
                {
                    FarmId = farm1.Id,
                    Type = "Fence",
                    Transformation = new TransformationDataModel
                    {
                        positionX = 15f,
                        positionY = 0f,
                        positionZ = 2f,
                        rotationX = 0f,
                        rotationY = 0f,
                        rotationZ = 0f
                    }
                }
            );

            // Objects on farm2
            context.PlacedObjects.AddRange(
                new PlacedObjectDataModel
                {
                    FarmId = farm2.Id,
                    Type = "AppleTree",
                    Transformation = new TransformationDataModel
                    {
                        positionX = 3f,
                        positionY = 0f,
                        positionZ = 7f,
                        rotationX = 0f,
                        rotationY = 45f,
                        rotationZ = 0f
                    }
                },
                new PlacedObjectDataModel
                {
                    FarmId = farm2.Id,
                    Type = "Scarecrow",
                    Transformation = new TransformationDataModel
                    {
                        positionX = -2f,
                        positionY = 0f,
                        positionZ = 4f,
                        rotationX = 0f,
                        rotationY = 0f,
                        rotationZ = 0f
                    }
                }
            );

            context.SaveChanges();

            context.SaveChanges();
        }
    }
}
