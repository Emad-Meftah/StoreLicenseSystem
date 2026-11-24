using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreLicense.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "subscribers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OwnerName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscribers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "subscription_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DurationDays = table.Column<int>(type: "integer", nullable: true),
                    IsLifetime = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultMaxDevices = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_plans", x => x.Id);
                    table.CheckConstraint("CK_subscription_plans_duration", "(\"IsLifetime\" = TRUE AND \"DurationDays\" IS NULL)\r\nOR\r\n(\"IsLifetime\" = FALSE AND \"DurationDays\" > 0)");
                    table.CheckConstraint("CK_subscription_plans_max_devices", "\"DefaultMaxDevices\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriberId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsLifetime = table.Column<bool>(type: "boolean", nullable: false),
                    MaxDevices = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.Id);
                    table.UniqueConstraint("AK_subscriptions_Id_SubscriberId", x => new { x.Id, x.SubscriberId });
                    table.CheckConstraint("CK_subscriptions_dates", "(\"IsLifetime\" = TRUE AND \"EndDate\" IS NULL)\r\nOR\r\n(\"IsLifetime\" = FALSE AND \"EndDate\" IS NOT NULL\r\nAND \"EndDate\" > \"StartDate\")");
                    table.CheckConstraint("CK_subscriptions_max_devices", "\"MaxDevices\" > 0");
                    table.ForeignKey(
                        name: "FK_subscriptions_subscribers_SubscriberId",
                        column: x => x.SubscriberId,
                        principalTable: "subscribers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subscriptions_subscription_plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "subscription_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "activation_codes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RedeemedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activation_codes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_activation_codes_subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "license_audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriberId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Details = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_license_audit_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_license_audit_logs_subscribers_SubscriberId",
                        column: x => x.SubscriberId,
                        principalTable: "subscribers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_license_audit_logs_subscriptions_SubscriptionId_SubscriberId",
                        columns: x => new { x.SubscriptionId, x.SubscriberId },
                        principalTable: "subscriptions",
                        principalColumns: new[] { "Id", "SubscriberId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscriber_devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriberId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DevicePublicKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DeviceIdentifier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriber_devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_subscriber_devices_subscribers_SubscriberId",
                        column: x => x.SubscriberId,
                        principalTable: "subscribers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subscriber_devices_subscriptions_SubscriptionId_SubscriberId",
                        columns: x => new { x.SubscriptionId, x.SubscriberId },
                        principalTable: "subscriptions",
                        principalColumns: new[] { "Id", "SubscriberId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscription_renewals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NewEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RenewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_renewals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_subscription_renewals_subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_activation_codes_CodeHash",
                table: "activation_codes",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_activation_codes_Status",
                table: "activation_codes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_activation_codes_SubscriptionId",
                table: "activation_codes",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_license_audit_logs_CreatedAt",
                table: "license_audit_logs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_license_audit_logs_SubscriberId",
                table: "license_audit_logs",
                column: "SubscriberId");

            migrationBuilder.CreateIndex(
                name: "IX_license_audit_logs_SubscriptionId",
                table: "license_audit_logs",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_license_audit_logs_SubscriptionId_SubscriberId",
                table: "license_audit_logs",
                columns: new[] { "SubscriptionId", "SubscriberId" });

            migrationBuilder.CreateIndex(
                name: "IX_subscriber_devices_DevicePublicKey",
                table: "subscriber_devices",
                column: "DevicePublicKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscriber_devices_SubscriberId",
                table: "subscriber_devices",
                column: "SubscriberId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriber_devices_SubscriptionId_DeviceIdentifier",
                table: "subscriber_devices",
                columns: new[] { "SubscriptionId", "DeviceIdentifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscriber_devices_SubscriptionId_Status",
                table: "subscriber_devices",
                columns: new[] { "SubscriptionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_subscriber_devices_SubscriptionId_SubscriberId",
                table: "subscriber_devices",
                columns: new[] { "SubscriptionId", "SubscriberId" });

            migrationBuilder.CreateIndex(
                name: "IX_subscribers_Phone",
                table: "subscribers",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_subscribers_Status",
                table: "subscribers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_IsActive",
                table: "subscription_plans",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_Name",
                table: "subscription_plans",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_renewals_RenewedAt",
                table: "subscription_renewals",
                column: "RenewedAt");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_renewals_SubscriptionId",
                table: "subscription_renewals",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_PlanId",
                table: "subscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_Status",
                table: "subscriptions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_Status_EndDate",
                table: "subscriptions",
                columns: new[] { "Status", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_SubscriberId",
                table: "subscriptions",
                column: "SubscriberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activation_codes");

            migrationBuilder.DropTable(
                name: "license_audit_logs");

            migrationBuilder.DropTable(
                name: "subscriber_devices");

            migrationBuilder.DropTable(
                name: "subscription_renewals");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.DropTable(
                name: "subscribers");

            migrationBuilder.DropTable(
                name: "subscription_plans");
        }
    }
}
