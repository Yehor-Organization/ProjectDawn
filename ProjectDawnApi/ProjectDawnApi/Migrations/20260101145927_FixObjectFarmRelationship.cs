using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectDawnApi.Migrations
{
    /// <inheritdoc />
    public partial class FixObjectFarmRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlacedObjects_Farms_FarmId1",
                table: "PlacedObjects");

            migrationBuilder.DropIndex(
                name: "IX_PlacedObjects_FarmId1",
                table: "PlacedObjects");

            migrationBuilder.DropColumn(
                name: "FarmDMId",
                table: "PlacedObjects");

            migrationBuilder.DropColumn(
                name: "FarmId1",
                table: "PlacedObjects");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FarmDMId",
                table: "PlacedObjects",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FarmId1",
                table: "PlacedObjects",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PlacedObjects_FarmId1",
                table: "PlacedObjects",
                column: "FarmId1");

            migrationBuilder.AddForeignKey(
                name: "FK_PlacedObjects_Farms_FarmId1",
                table: "PlacedObjects",
                column: "FarmId1",
                principalTable: "Farms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
