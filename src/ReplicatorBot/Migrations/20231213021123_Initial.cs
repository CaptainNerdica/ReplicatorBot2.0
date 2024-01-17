using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReplicatorBot.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildConfig",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    TargetUserId = table.Column<long>(type: "INTEGER", nullable: true),
                    GuildMessageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetMessageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Probability = table.Column<double>(type: "REAL", nullable: false),
                    AutoUpdateProbability = table.Column<bool>(type: "INTEGER", nullable: false),
                    AutoUpdateMessages = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanMention = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastUpdate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildConfig", x => x.GuildId);
                });

            migrationBuilder.CreateTable(
                name: "ChannelPermissions",
                columns: table => new
                {
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<long>(type: "INTEGER", nullable: false),
                    Permissions = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelPermissions", x => new { x.GuildId, x.ChannelId });
                    table.ForeignKey(
                        name: "FK_ChannelPermissions_GuildConfig_GuildId",
                        column: x => x.GuildId,
                        principalTable: "GuildConfig",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DisabledUsers",
                columns: table => new
                {
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisabledUsers", x => new { x.GuildId, x.UserId });
                    table.ForeignKey(
                        name: "FK_DisabledUsers_GuildConfig_GuildId",
                        column: x => x.GuildId,
                        principalTable: "GuildConfig",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Guild",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guild", x => x.GuildId);
                    table.ForeignKey(
                        name: "FK_Guild_GuildConfig_GuildId",
                        column: x => x.GuildId,
                        principalTable: "GuildConfig",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    MessageId = table.Column<long>(type: "INTEGER", nullable: false),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 6144, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.MessageId);
                    table.ForeignKey(
                        name: "FK_Messages_GuildConfig_GuildId",
                        column: x => x.GuildId,
                        principalTable: "GuildConfig",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Message_Guild_Index",
                table: "Messages",
                columns: new[] { "GuildId", "Index" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelPermissions");

            migrationBuilder.DropTable(
                name: "DisabledUsers");

            migrationBuilder.DropTable(
                name: "Guild");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "GuildConfig");
        }
    }
}
