using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class MigrateToNewLocationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "Places");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "EventStatus");

            migrationBuilder.AddColumn<string>(
                name: "LocationId",
                table: "Places",
                type: "text",
                nullable: false,
                defaultValue: "salou");

            migrationBuilder.AddColumn<string>(
                name: "LocationId",
                table: "EventStatus",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "Places");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "EventStatus");

            migrationBuilder.AddColumn<int>(
                name: "Location",
                table: "Places",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Location",
                table: "EventStatus",
                type: "integer",
                nullable: true);
        }
    }
}
