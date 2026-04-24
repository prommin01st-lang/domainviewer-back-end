using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DomainViewer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRetryFieldsToNotificationLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAt",
                table: "NotificationLogs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "NotificationLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextRetryAt",
                table: "NotificationLogs");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "NotificationLogs");
        }
    }
}
