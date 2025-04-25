using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class AddMultipleDatesToExit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "Exits");

            migrationBuilder.DropColumn(
                name: "Time",
                table: "Exits");

            migrationBuilder.AddColumn<List<DateTimeOffset>>(
                name: "Dates",
                table: "Exits",
                type: "timestamp with time zone[]",
                nullable: false,
                defaultValue: new List<DateTimeOffset>());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Dates",
                table: "Exits");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndTime",
                table: "Exits",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Time",
                table: "Exits",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
