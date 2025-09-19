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
        }
    }
}
