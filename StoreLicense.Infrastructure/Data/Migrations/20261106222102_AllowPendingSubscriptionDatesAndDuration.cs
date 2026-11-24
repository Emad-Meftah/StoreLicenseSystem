using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreLicense.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllowPendingSubscriptionDatesAndDuration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_subscriptions_dates",
                table: "subscriptions");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "subscriptions",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<int>(
                name: "DurationDays",
                table: "subscriptions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_subscriptions_dates",
                table: "subscriptions",
                sql: "(\r\n    \"IsLifetime\" = TRUE\r\n    AND \"DurationDays\" IS NULL\r\n    AND \"EndDate\" IS NULL\r\n)\r\nOR\r\n(\r\n    \"IsLifetime\" = FALSE\r\n    AND \"DurationDays\" > 0\r\n    AND\r\n    (\r\n        (\r\n            \"StartDate\" IS NULL\r\n            AND \"EndDate\" IS NULL\r\n        )\r\n        OR\r\n        (\r\n            \"StartDate\" IS NOT NULL\r\n            AND \"EndDate\" IS NOT NULL\r\n            AND \"EndDate\" > \"StartDate\"\r\n        )\r\n    )\r\n)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_subscriptions_dates",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "DurationDays",
                table: "subscriptions");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_subscriptions_dates",
                table: "subscriptions",
                sql: "(\"IsLifetime\" = TRUE AND \"EndDate\" IS NULL)\r\nOR\r\n(\"IsLifetime\" = FALSE AND \"EndDate\" IS NOT NULL\r\nAND \"EndDate\" > \"StartDate\")");
        }
    }
}
