using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackEnd.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePortfolioMultilingualFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Portfolios",
                newName: "EnTitle");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Portfolios",
                newName: "EnDescription");

            migrationBuilder.AddColumn<string>(
                name: "ArDescription",
                table: "Portfolios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ArTitle",
                table: "Portfolios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArDescription",
                table: "Portfolios");

            migrationBuilder.DropColumn(
                name: "ArTitle",
                table: "Portfolios");

            migrationBuilder.RenameColumn(
                name: "EnTitle",
                table: "Portfolios",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "EnDescription",
                table: "Portfolios",
                newName: "Description");
        }
    }
}
