using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AgentsAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SeedCronCrawlers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "LastRunTime",
                table: "CronCrawlers",
                type: "timestamp",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "CronCrawlers",
                columns: new[] { "Id", "CrawlerName", "CronExpression", "IsActive", "LastRunTime" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-0001-0000-0000-000000000001"), "Microsoft", "0 0 * * *", true, null },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000002"), "Amazon", "0 0 * * *", true, null },
                    { new Guid("a1b2c3d4-0003-0000-0000-000000000003"), "Google", "0 0 * * *", true, null },
                    { new Guid("a1b2c3d4-0004-0000-0000-000000000004"), "Fueled", "0 0 * * *", true, null },
                    { new Guid("a1b2c3d4-0005-0000-0000-000000000005"), "AshbyHQ", "0 0 * * *", true, null },
                    { new Guid("a1b2c3d4-0006-0000-0000-000000000006"), "Acquia", "0 0 * * *", true, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "CronCrawlers",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-0001-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "CronCrawlers",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-0002-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "CronCrawlers",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-0003-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "CronCrawlers",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-0004-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "CronCrawlers",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-0005-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "CronCrawlers",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-0006-0000-0000-000000000006"));

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastRunTime",
                table: "CronCrawlers",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp",
                oldNullable: true);
        }
    }
}
