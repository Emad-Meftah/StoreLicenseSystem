using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreLicense.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLicenseValidationNonces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "license_validation_nonces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nonce = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_license_validation_nonces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_license_validation_nonces_subscriber_devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "subscriber_devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_license_validation_nonces_DeviceId_Nonce",
                table: "license_validation_nonces",
                columns: new[] { "DeviceId", "Nonce" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_license_validation_nonces_ExpiresAt",
                table: "license_validation_nonces",
                column: "ExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "license_validation_nonces");
        }
    }
}
