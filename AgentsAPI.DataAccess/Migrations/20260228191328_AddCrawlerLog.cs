using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentsAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCrawlerLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CrawlerLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CrawlerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    JobsCrawled = table.Column<int>(type: "integer", nullable: false),
                    JobsSaved = table.Column<int>(type: "integer", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    StackTrace = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrawlerLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrawlerLogs");
        }
    }
}
