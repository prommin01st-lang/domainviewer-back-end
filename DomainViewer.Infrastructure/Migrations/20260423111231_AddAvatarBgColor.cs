using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DomainViewer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAvatarBgColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarBgColor",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarBgColor",
                table: "Users");
        }
    }
}
