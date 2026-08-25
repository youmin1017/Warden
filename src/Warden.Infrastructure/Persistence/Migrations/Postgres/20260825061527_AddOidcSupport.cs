using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warden.Infrastructure.Persistence.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddOidcSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "AuthProvider",
                table: "Users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSubject",
                table: "Users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OidcHandoffCodes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TokenPairJson = table.Column<string>(type: "text", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OidcHandoffCodes", x => x.Code);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_AuthProvider_ExternalSubject",
                table: "Users",
                columns: new[] { "AuthProvider", "ExternalSubject" },
                unique: true,
                filter: "\"AuthProvider\" IS NOT NULL AND \"ExternalSubject\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OidcHandoffCodes");

            migrationBuilder.DropIndex(
                name: "IX_Users_AuthProvider_ExternalSubject",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AuthProvider",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExternalSubject",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
