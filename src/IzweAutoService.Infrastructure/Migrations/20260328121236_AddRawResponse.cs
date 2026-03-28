using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IzweAutoService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRawResponse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RawResponse",
                table: "sms_records",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RawResponse",
                table: "sms_records");
        }
    }
}
