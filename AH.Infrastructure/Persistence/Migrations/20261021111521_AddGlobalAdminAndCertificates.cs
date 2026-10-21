using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AH.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalAdminAndCertificates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMain",
                table: "workspaces",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsGlobalAdministrator",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "user_certificates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Thumbprint = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NotBefore = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NotAfter = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ObjectKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IssuedById = table.Column<int>(type: "integer", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_certificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_certificates_users_IssuedById",
                        column: x => x.IssuedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_certificates_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workspaces_IsMain",
                table: "workspaces",
                column: "IsMain",
                unique: true,
                filter: "\"IsMain\"");

            migrationBuilder.CreateIndex(
                name: "IX_users_IsGlobalAdministrator",
                table: "users",
                column: "IsGlobalAdministrator",
                unique: true,
                filter: "\"IsGlobalAdministrator\"");

            migrationBuilder.CreateIndex(
                name: "IX_user_certificates_IssuedById",
                table: "user_certificates",
                column: "IssuedById");

            migrationBuilder.CreateIndex(
                name: "IX_user_certificates_Thumbprint",
                table: "user_certificates",
                column: "Thumbprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_certificates_UserId",
                table: "user_certificates",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_certificates");

            migrationBuilder.DropIndex(
                name: "IX_workspaces_IsMain",
                table: "workspaces");

            migrationBuilder.DropIndex(
                name: "IX_users_IsGlobalAdministrator",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IsMain",
                table: "workspaces");

            migrationBuilder.DropColumn(
                name: "IsGlobalAdministrator",
                table: "users");
        }
    }
}
