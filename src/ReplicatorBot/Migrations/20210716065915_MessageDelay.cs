using Microsoft.EntityFrameworkCore.Migrations;

namespace ReplicatorBot.Migrations
{
    public partial class MessageDelay : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Delay",
                table: "GuildInfo",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "FixedDelay",
                table: "GuildInfo",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Delay",
                table: "GuildInfo");

            migrationBuilder.DropColumn(
                name: "FixedDelay",
                table: "GuildInfo");
        }
    }
}
