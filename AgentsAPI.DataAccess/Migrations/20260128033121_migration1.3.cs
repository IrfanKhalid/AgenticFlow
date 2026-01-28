using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentsAPI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class migration13 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Responsibilities",
                table: "JobDetails",
                type: "text",
                nullable: true,
                oldClrType: typeof(List<string>),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "Requirements",
                table: "JobDetails",
                type: "text",
                nullable: true,
                oldClrType: typeof(List<string>),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "JobDetails",
                type: "text",
                nullable: true,
                oldClrType: typeof(List<string>),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "Achievements",
                table: "JobDetails",
                type: "text",
                nullable: true,
                oldClrType: typeof(List<string>),
                oldType: "jsonb");

            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "JobDetails",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "JobDetails",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Active",
                table: "JobDetails");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "JobDetails");

            migrationBuilder.AlterColumn<List<string>>(
                name: "Responsibilities",
                table: "JobDetails",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<List<string>>(
                name: "Requirements",
                table: "JobDetails",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<List<string>>(
                name: "Description",
                table: "JobDetails",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<List<string>>(
                name: "Achievements",
                table: "JobDetails",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
