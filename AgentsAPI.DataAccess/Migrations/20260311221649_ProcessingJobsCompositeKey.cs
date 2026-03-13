using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentsAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ProcessingJobsCompositeKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ProcessingJobs",
                table: "ProcessingJobs");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProcessingJobs",
                table: "ProcessingJobs",
                columns: new[] { "JobsIds", "ApplyUrl" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ProcessingJobs",
                table: "ProcessingJobs");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProcessingJobs",
                table: "ProcessingJobs",
                column: "Id");
        }
    }
}
