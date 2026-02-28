using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentsAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddIsProcessedToJobDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsProcessed",
                table: "JobDetails",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsProcessed",
                table: "JobDetails");
        }
    }
}
