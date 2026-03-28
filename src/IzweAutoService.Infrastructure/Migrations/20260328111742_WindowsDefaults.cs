using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IzweAutoService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WindowsDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "app_settings",
                keyColumn: "Id",
                keyValue: 1,
                column: "Value",
                value: "C:\\SFTP\\SMS_Automation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "app_settings",
                keyColumn: "Id",
                keyValue: 1,
                column: "Value",
                value: "/data/sms");
        }
    }
}
