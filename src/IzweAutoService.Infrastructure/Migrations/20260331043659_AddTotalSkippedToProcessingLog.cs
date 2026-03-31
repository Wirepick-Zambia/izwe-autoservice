using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IzweAutoService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTotalSkippedToProcessingLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalSkipped",
                table: "processing_logs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalSkipped",
                table: "processing_logs");
        }
    }
}
