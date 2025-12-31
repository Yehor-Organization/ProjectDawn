namespace ProjectDawnApi
{
    public static class DbInitializer
    {
        public static void Initialize(ProjectDawnDbContext context)
        {
            if (!context.Players.Any())
            {
                var players = new[]
                {
            new PlayerDM { Name = "Alice" },
            new PlayerDM { Name = "Bob" },
            new PlayerDM { Name = "Charlie" },
            new PlayerDM { Name = "David" },
            new PlayerDM { Name = "Eve" },
            new PlayerDM { Name = "Frank" },
            new PlayerDM { Name = "Grace" },
            new PlayerDM { Name = "Hank" },
            new PlayerDM { Name = "Ivy" },
            new PlayerDM { Name = "Jack" }
        };

                context.Players.AddRange(players);
                context.SaveChanges();
            }

            if (!context.Farms.Any())
            {
                var player1 = context.Players.First(p => p.Name == "Alice");
                var player2 = context.Players.First(p => p.Name == "Bob");

                context.Farms.AddRange(
                    new FarmDM { Name = "Sunny Meadow Farm", OwnerId = player1.Id },
                    new FarmDM { Name = "Bob's Grand Orchard", OwnerId = player2.Id }
                );

                context.SaveChanges();
            }
        }

    }
}
