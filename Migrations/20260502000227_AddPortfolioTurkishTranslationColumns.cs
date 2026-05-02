using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackEnd.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioTurkishTranslationColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTranslated",
                table: "Portfolios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OriginalLanguage",
                table: "Portfolios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "en");

            migrationBuilder.AddColumn<string>(
                name: "TrDescription",
                table: "Portfolios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TrTitle",
                table: "Portfolios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTranslated",
                table: "Portfolios");

            migrationBuilder.DropColumn(
                name: "OriginalLanguage",
                table: "Portfolios");

            migrationBuilder.DropColumn(
                name: "TrDescription",
                table: "Portfolios");

            migrationBuilder.DropColumn(
                name: "TrTitle",
                table: "Portfolios");
        }
    }
}
