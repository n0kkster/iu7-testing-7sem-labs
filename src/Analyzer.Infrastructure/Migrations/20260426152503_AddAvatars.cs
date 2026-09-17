using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Analyzer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAvatars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "avatar_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "avatars",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data = table.Column<byte[]>(type: "bytea", nullable: false),
                    hash = table.Column<string>(type: "text", nullable: false),
                    content_type = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_avatars", x => x.id);
                    table.ForeignKey(
                        name: "fk_avatars_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_users_avatar_id",
                table: "users",
                column: "avatar_id");

            migrationBuilder.CreateIndex(
                name: "ix_avatars_user_id",
                table: "avatars",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_avatars_user_id_hash",
                table: "avatars",
                columns: new[] { "user_id", "hash" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_users_avatars_avatar_id",
                table: "users",
                column: "avatar_id",
                principalTable: "avatars",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_users_avatars_avatar_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "avatars");

            migrationBuilder.DropIndex(
                name: "ix_users_avatar_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "avatar_id",
                table: "users");
        }
    }
}
