using Microsoft.EntityFrameworkCore.Migrations;

namespace ConsoleLogSearch.Migrations
{
    public partial class AddUnique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_HelixConsoleLogs_ConsoleLogUri",
                table: "HelixConsoleLogs",
                column: "ConsoleLogUri",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HelixConsoleLogs_ConsoleLogUri",
                table: "HelixConsoleLogs");
        }
    }
}
