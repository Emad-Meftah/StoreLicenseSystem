using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreLicense.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllowDevicePublicKeyReuseForRevokedDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_subscriber_devices_DevicePublicKey",
                table: "subscriber_devices");

            migrationBuilder.CreateIndex(
                name: "IX_subscriber_devices_DevicePublicKey",
                table: "subscriber_devices",
                column: "DevicePublicKey",
                unique: true,
                filter: "\"Status\" = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_subscriber_devices_DevicePublicKey",
                table: "subscriber_devices");

            migrationBuilder.CreateIndex(
                name: "IX_subscriber_devices_DevicePublicKey",
                table: "subscriber_devices",
                column: "DevicePublicKey",
                unique: true);
        }
    }
}
