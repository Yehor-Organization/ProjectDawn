using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectDawnApi.Migrations
{
    /// <inheritdoc />
    public partial class dddddddddddd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "Transformation_serverTime",
                table: "PlacedObjects",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Transformation_serverTime",
                table: "FarmVisitors",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Transformation_serverTime",
                table: "PlacedObjects");

            migrationBuilder.DropColumn(
                name: "Transformation_serverTime",
                table: "FarmVisitors");
        }
    }
}
