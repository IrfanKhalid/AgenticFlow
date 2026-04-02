using System;
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
            migrationBuilder.DropPrimaryKey(
                name: "PK_ProcessingJobs",
                table: "ProcessingJobs");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ProcessingJobs");

            migrationBuilder.RenameColumn(
                name: "JobsIds",
                table: "ProcessingJobs",
                newName: "ContentHash");

            migrationBuilder.AddColumn<bool>(
                name: "IsProcessd",
                table: "ProcessingJobs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "JobDetails",
                type: "text",
                nullable: false,
                computedColumnSql: "md5(coalesce(\"Title\", '') || '|' || coalesce(\"Description\", '') || '|' || coalesce(\"ApplyUrl\", ''))",
                stored: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProcessingJobs",
                table: "ProcessingJobs",
                column: "ContentHash");

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

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProcessingJobs",
                table: "ProcessingJobs");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "JobDetails");

            migrationBuilder.DropColumn(
                name: "IsProcessd",
                table: "ProcessingJobs");

            migrationBuilder.RenameColumn(
                name: "ContentHash",
                table: "ProcessingJobs",
                newName: "JobsIds");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "ProcessingJobs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProcessingJobs",
                table: "ProcessingJobs",
                columns: new[] { "JobsIds", "ApplyUrl" });
        }
    }
}
