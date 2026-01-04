using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectDawnApi.Migrations
{
    /// <inheritdoc />
    public partial class dwdedwe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_FarmVisitors",
                table: "FarmVisitors");

            migrationBuilder.DropIndex(
                name: "IX_FarmVisitors_PlayerId",
                table: "FarmVisitors");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "FarmVisitors",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_FarmVisitors",
                table: "FarmVisitors",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_FarmVisitors_FarmId",
                table: "FarmVisitors",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmVisitors_PlayerId",
                table: "FarmVisitors",
                column: "PlayerId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_FarmVisitors",
                table: "FarmVisitors");

            migrationBuilder.DropIndex(
                name: "IX_FarmVisitors_FarmId",
                table: "FarmVisitors");

            migrationBuilder.DropIndex(
                name: "IX_FarmVisitors_PlayerId",
                table: "FarmVisitors");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "FarmVisitors",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_FarmVisitors",
                table: "FarmVisitors",
                columns: new[] { "FarmId", "PlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_FarmVisitors_PlayerId",
                table: "FarmVisitors",
                column: "PlayerId");
        }
    }
}
