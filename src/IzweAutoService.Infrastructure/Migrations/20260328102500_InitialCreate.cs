using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace IzweAutoService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "processing_logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TotalFound = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalSent = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalFailed = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processing_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sms_records",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContractId = table.Column<string>(type: "TEXT", nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: false),
                    MessageContent = table.Column<string>(type: "TEXT", nullable: false),
                    MessageTimestamp = table.Column<string>(type: "TEXT", nullable: true),
                    Country = table.Column<string>(type: "TEXT", nullable: false),
                    SenderId = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    SourceFile = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ApiMessageId = table.Column<string>(type: "TEXT", nullable: true),
                    ApiStatus = table.Column<string>(type: "TEXT", nullable: true),
                    ApiCost = table.Column<decimal>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sms_records", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "app_settings",
                columns: new[] { "Id", "Category", "Key", "Value" },
                values: new object[,]
                {
                    { 1, "General", "BaseFolderPath", "/data/sms" },
                    { 2, "Sms", "SenderId", "IzweLoans" },
                    { 3, "Sms", "ClientId", "WireGhana" },
                    { 4, "Sms", "ApiUrl", "https://api.wirepick.com/httpsms/send" },
                    { 5, "Sms", "ApiPassword", "" },
                    { 6, "General", "CronIntervalMinutes", "5" },
                    { 7, "Smtp", "SmtpHost", "" },
                    { 8, "Smtp", "SmtpPort", "587" },
                    { 9, "Smtp", "SmtpUsername", "" },
                    { 10, "Smtp", "SmtpPassword", "" },
                    { 11, "Smtp", "SmtpUseSsl", "true" },
                    { 12, "Alerts", "AlertEmailFrom", "" },
                    { 13, "Alerts", "AlertEmailTo", "" },
                    { 14, "Alerts", "AlertsEnabled", "false" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_app_settings_Key",
                table: "app_settings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_processing_logs_StartedAt",
                table: "processing_logs",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_sms_records_Country",
                table: "sms_records",
                column: "Country");

            migrationBuilder.CreateIndex(
                name: "IX_sms_records_CreatedAt",
                table: "sms_records",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_sms_records_Status",
                table: "sms_records",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_settings");

            migrationBuilder.DropTable(
                name: "processing_logs");

            migrationBuilder.DropTable(
                name: "sms_records");
        }
    }
}
