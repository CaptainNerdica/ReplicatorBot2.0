using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ReplicatorBot.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildInfo",
                columns: table => new
                {
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Prefix = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    TargetUserId = table.Column<long>(type: "INTEGER", nullable: true),
                    GuildMessageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetMessageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Probability = table.Column<double>(type: "REAL", nullable: false),
                    AutoUpdateProbability = table.Column<bool>(type: "INTEGER", nullable: false),
                    AutoUpdateMessages = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanMention = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanEmbed = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastUpdate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildInfo", x => x.GuildId);
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
                        name: "FK_ChannelPermissions_GuildInfo_GuildId",
                        column: x => x.GuildId,
                        principalTable: "GuildInfo",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DisabledSubstrings",
                columns: table => new
                {
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    Substring = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisabledSubstrings", x => new { x.GuildId, x.Index });
                    table.ForeignKey(
                        name: "FK_DisabledSubstrings_GuildInfo_GuildId",
                        column: x => x.GuildId,
                        principalTable: "GuildInfo",
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
                        name: "FK_DisabledUsers_GuildInfo_GuildId",
                        column: x => x.GuildId,
                        principalTable: "GuildInfo",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    MessageId = table.Column<long>(type: "INTEGER", nullable: false),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.MessageId);
                    table.ForeignKey(
                        name: "FK_Messages_GuildInfo_GuildId",
                        column: x => x.GuildId,
                        principalTable: "GuildInfo",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Message_Guild_Index",
                table: "Messages",
                columns: new[] { "GuildId", "Index" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelPermissions");

            migrationBuilder.DropTable(
                name: "DisabledSubstrings");

            migrationBuilder.DropTable(
                name: "DisabledUsers");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "GuildInfo");
        }
    }
}
