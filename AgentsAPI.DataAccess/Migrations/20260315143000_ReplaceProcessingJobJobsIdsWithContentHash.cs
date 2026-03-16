using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentsAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceProcessingJobJobsIdsWithContentHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ProcessingJobs",
                table: "ProcessingJobs");

            migrationBuilder.RenameColumn(
                name: "JobsIds",
                table: "ProcessingJobs",
                newName: "ContentHash");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProcessingJobs",
                table: "ProcessingJobs",
                column: "ContentHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ProcessingJobs",
                table: "ProcessingJobs");

            migrationBuilder.RenameColumn(
                name: "ContentHash",
                table: "ProcessingJobs",
                newName: "JobsIds");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProcessingJobs",
                table: "ProcessingJobs",
                columns: new[] { "JobsIds", "ApplyUrl" });
        }
    }
}
