using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectDawnApi.Migrations
{
    /// <inheritdoc />
    public partial class AddFarmOwners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Farms_Players_OwnerId",
                table: "Farms");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_InventoryId",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_Farms_OwnerId",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Farms");

            migrationBuilder.CreateTable(
                name: "FarmOwners",
                columns: table => new
                {
                    FarmsId = table.Column<int>(type: "INTEGER", nullable: false),
                    OwnersId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmOwners", x => new { x.FarmsId, x.OwnersId });
                    table.ForeignKey(
                        name: "FK_FarmOwners_Farms_FarmsId",
                        column: x => x.FarmsId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FarmOwners_Players_OwnersId",
                        column: x => x.OwnersId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_InventoryId_ItemType",
                table: "InventoryItems",
                columns: new[] { "InventoryId", "ItemType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FarmOwners_OwnersId",
                table: "FarmOwners",
                column: "OwnersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FarmOwners");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_InventoryId_ItemType",
                table: "InventoryItems");

            migrationBuilder.AddColumn<int>(
                name: "OwnerId",
                table: "Farms",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_InventoryId",
                table: "InventoryItems",
                column: "InventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Farms_OwnerId",
                table: "Farms",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Farms_Players_OwnerId",
                table: "Farms",
                column: "OwnerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
