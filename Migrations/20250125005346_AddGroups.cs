using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class AddGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "With",
                table: "EventStatus");

            migrationBuilder.AddColumn<int>(
                name: "EventGroupId",
                table: "EventStatus",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EventGroupInvitationId",
                table: "EventStatus",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Members = table.Column<List<string>>(type: "text[]", nullable: false),
                    AwaitingInvite = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropColumn(
                name: "EventGroupId",
                table: "EventStatus");

            migrationBuilder.DropColumn(
                name: "EventGroupInvitationId",
                table: "EventStatus");

            migrationBuilder.AddColumn<List<string>>(
                name: "With",
                table: "EventStatus",
                type: "text[]",
                nullable: true);
        }
    }
}
