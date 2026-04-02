using AgentsAPI.DataAccess.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentsAPI.DataAccess.Migrations
{
    [DbContext(typeof(AgentsDbContext))]
    [Migration("20260315130000_MoveContentHashToJobDetails")]
    public partial class MoveContentHashToJobDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_name = 'ProcessingJobs'
          AND column_name = 'ContentHash') THEN
        ALTER TABLE ""ProcessingJobs"" DROP COLUMN ""ContentHash"";
    END IF;
END $$;");

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "JobDetails",
                type: "text",
                nullable: false,
                computedColumnSql: "md5(concat_ws('|', coalesce(\"Title\", ''), coalesce(\"Description\", ''), coalesce(\"ApplyUrl\", '')))",
                stored: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "JobDetails");
        }
    }
}
