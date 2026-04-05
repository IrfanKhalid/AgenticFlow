using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentsAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddJobFeaturesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobFeatures",
                columns: table => new
                {
                    content_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    required_years = table.Column<int>(type: "integer", nullable: true),
                    skills = table.Column<string>(type: "text", nullable: true),
                    tools = table.Column<string>(type: "text", nullable: true),
                    cloud_demand = table.Column<string>(type: "text", nullable: true),
                    ai_demand = table.Column<string>(type: "text", nullable: true),
                    salary = table.Column<string>(type: "text", nullable: true),
                    has_ai = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    has_cloud = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobFeatures", x => x.content_hash);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobFeatures");
        }
    }
}
