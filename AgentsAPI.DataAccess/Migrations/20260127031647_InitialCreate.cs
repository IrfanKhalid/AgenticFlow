using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentsAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobDetails",
                columns: table => new
                {
                    ApplyUrl = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<List<string>>(type: "jsonb", nullable: false),
                    Responsibilities = table.Column<List<string>>(type: "jsonb", nullable: false),
                    Achievements = table.Column<List<string>>(type: "jsonb", nullable: false),
                    Requirements = table.Column<List<string>>(type: "jsonb", nullable: false),
                    Compensation = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobDetails", x => x.ApplyUrl);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobDetails");
        }
    }
}
