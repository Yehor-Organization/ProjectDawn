using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectDawnApi.Migrations
{
    /// <inheritdoc />
    public partial class dwedwe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ConnectionId",
                table: "FarmVisitors",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "FarmVisitors",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "JoinedAtUtc",
                table: "FarmVisitors",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Id",
                table: "FarmVisitors");

            migrationBuilder.DropColumn(
                name: "JoinedAtUtc",
                table: "FarmVisitors");

            migrationBuilder.AlterColumn<string>(
                name: "ConnectionId",
                table: "FarmVisitors",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }
    }
}
