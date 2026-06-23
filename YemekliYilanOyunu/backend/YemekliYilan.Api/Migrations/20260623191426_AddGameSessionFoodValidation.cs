using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YemekliYilan.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGameSessionFoodValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastFoodAt",
                table: "GameSessions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastFoodAt",
                table: "GameSessions");
        }
    }
}
