using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Brucewnd.Migrations
{
    /// <inheritdoc />
    public partial class roles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chapter_Comics_ComicId",
                table: "Chapter");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Chapter",
                table: "Chapter");

            migrationBuilder.DropIndex(
                name: "IX_Chapter_ComicId",
                table: "Chapter");

            migrationBuilder.RenameTable(
                name: "Chapter",
                newName: "Chapters");

            migrationBuilder.AlterColumn<bool>(
                name: "IsPublished",
                table: "Chapters",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Chapters",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW() AT TIME ZONE 'UTC'",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Chapters",
                table: "Chapters",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Comics_Name",
                table: "Comics",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_ComicId_ChapterNumber",
                table: "Chapters",
                columns: new[] { "ComicId", "ChapterNumber" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Chapter_ChapterNumber_Positive",
                table: "Chapters",
                sql: "\"ChapterNumber\" > 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Chapters_Comics_ComicId",
                table: "Chapters",
                column: "ComicId",
                principalTable: "Comics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chapters_Comics_ComicId",
                table: "Chapters");

            migrationBuilder.DropIndex(
                name: "IX_Comics_Name",
                table: "Comics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Chapters",
                table: "Chapters");

            migrationBuilder.DropIndex(
                name: "IX_Chapters_ComicId_ChapterNumber",
                table: "Chapters");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Chapter_ChapterNumber_Positive",
                table: "Chapters");

            migrationBuilder.RenameTable(
                name: "Chapters",
                newName: "Chapter");

            migrationBuilder.AlterColumn<bool>(
                name: "IsPublished",
                table: "Chapter",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Chapter",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW() AT TIME ZONE 'UTC'");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Chapter",
                table: "Chapter",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Chapter_ComicId",
                table: "Chapter",
                column: "ComicId");

            migrationBuilder.AddForeignKey(
                name: "FK_Chapter_Comics_ComicId",
                table: "Chapter",
                column: "ComicId",
                principalTable: "Comics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
