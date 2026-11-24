using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreLicense.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEncryptedCodeToActivationCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EncryptedCode",
                table: "activation_codes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedCode",
                table: "activation_codes");
        }
    }
}
