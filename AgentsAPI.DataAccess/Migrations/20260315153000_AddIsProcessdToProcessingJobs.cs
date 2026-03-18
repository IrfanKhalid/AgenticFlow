using AgentsAPI.DataAccess.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentsAPI.DataAccess.Migrations
{
    [DbContext(typeof(AgentsDbContext))]
    [Migration("20260315153000_AddIsProcessdToProcessingJobs")]
    public partial class AddIsProcessdToProcessingJobs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsProcessd",
                table: "ProcessingJobs",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsProcessd",
                table: "ProcessingJobs");
        }
    }
}
