using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IzweAutoService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexesLockAndBatchSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_sms_records_CreatedAt",
                table: "sms_records");

            migrationBuilder.DropIndex(
                name: "IX_sms_records_Status",
                table: "sms_records");

            migrationBuilder.CreateTable(
                name: "processing_locks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LockName = table.Column<string>(type: "TEXT", nullable: false),
                    HeldBy = table.Column<string>(type: "TEXT", nullable: true),
                    AcquiredAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processing_locks", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "app_settings",
                columns: new[] { "Id", "Category", "Key", "Value" },
                values: new object[] { 15, "General", "SmsBatchSize", "500" });

            migrationBuilder.InsertData(
                table: "processing_locks",
                columns: new[] { "Id", "AcquiredAt", "ExpiresAt", "HeldBy", "LockName" },
                values: new object[] { 1, null, null, null, "SmsProcessing" });

            migrationBuilder.CreateIndex(
                name: "IX_sms_records_ContractId",
                table: "sms_records",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_sms_records_ContractId_PhoneNumber_SourceFile",
                table: "sms_records",
                columns: new[] { "ContractId", "PhoneNumber", "SourceFile" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sms_records_PhoneNumber",
                table: "sms_records",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_sms_records_ProcessedAt",
                table: "sms_records",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_sms_records_Status_CreatedAt",
                table: "sms_records",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_app_settings_Category",
                table: "app_settings",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_processing_locks_LockName",
                table: "processing_locks",
                column: "LockName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "processing_locks");

            migrationBuilder.DropIndex(
                name: "IX_sms_records_ContractId",
                table: "sms_records");

            migrationBuilder.DropIndex(
                name: "IX_sms_records_ContractId_PhoneNumber_SourceFile",
                table: "sms_records");

            migrationBuilder.DropIndex(
                name: "IX_sms_records_PhoneNumber",
                table: "sms_records");

            migrationBuilder.DropIndex(
                name: "IX_sms_records_ProcessedAt",
                table: "sms_records");

            migrationBuilder.DropIndex(
                name: "IX_sms_records_Status_CreatedAt",
                table: "sms_records");

            migrationBuilder.DropIndex(
                name: "IX_app_settings_Category",
                table: "app_settings");

            migrationBuilder.DeleteData(
                table: "app_settings",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.CreateIndex(
                name: "IX_sms_records_CreatedAt",
                table: "sms_records",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_sms_records_Status",
                table: "sms_records",
                column: "Status");
        }
    }
}
