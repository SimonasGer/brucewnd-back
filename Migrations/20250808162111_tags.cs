using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Brucewnd.Migrations
{
    /// <inheritdoc />
    public partial class tags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comics_Users_AuthorId",
                table: "Comics");

            migrationBuilder.DropTable(
                name: "ComicComicTag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ComicTag",
                table: "ComicTag");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ComicTag");

            migrationBuilder.RenameTable(
                name: "ComicTag",
                newName: "ComicTags");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ComicTags",
                newName: "TagId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Comics",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW() AT TIME ZONE 'UTC'",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<int>(
                name: "TagId",
                table: "ComicTags",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "ComicId",
                table: "ComicTags",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ComicTags",
                table: "ComicTags",
                columns: new[] { "ComicId", "TagId" });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComicTags_ComicId_TagId",
                table: "ComicTags",
                columns: new[] { "ComicId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComicTags_TagId",
                table: "ComicTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Comics_Users_AuthorId",
                table: "Comics",
                column: "AuthorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ComicTags_Comics_ComicId",
                table: "ComicTags",
                column: "ComicId",
                principalTable: "Comics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ComicTags_Tags_TagId",
                table: "ComicTags",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comics_Users_AuthorId",
                table: "Comics");

            migrationBuilder.DropForeignKey(
                name: "FK_ComicTags_Comics_ComicId",
                table: "ComicTags");

            migrationBuilder.DropForeignKey(
                name: "FK_ComicTags_Tags_TagId",
                table: "ComicTags");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Roles_Name",
                table: "Roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ComicTags",
                table: "ComicTags");

            migrationBuilder.DropIndex(
                name: "IX_ComicTags_ComicId_TagId",
                table: "ComicTags");

            migrationBuilder.DropIndex(
                name: "IX_ComicTags_TagId",
                table: "ComicTags");

            migrationBuilder.DropColumn(
                name: "ComicId",
                table: "ComicTags");

            migrationBuilder.RenameTable(
                name: "ComicTags",
                newName: "ComicTag");

            migrationBuilder.RenameColumn(
                name: "TagId",
                table: "ComicTag",
                newName: "Id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Comics",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW() AT TIME ZONE 'UTC'");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ComicTag",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ComicTag",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ComicTag",
                table: "ComicTag",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ComicComicTag",
                columns: table => new
                {
                    ComicsId = table.Column<int>(type: "integer", nullable: false),
                    TagsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComicComicTag", x => new { x.ComicsId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_ComicComicTag_ComicTag_TagsId",
                        column: x => x.TagsId,
                        principalTable: "ComicTag",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComicComicTag_Comics_ComicsId",
                        column: x => x.ComicsId,
                        principalTable: "Comics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComicComicTag_TagsId",
                table: "ComicComicTag",
                column: "TagsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comics_Users_AuthorId",
                table: "Comics",
                column: "AuthorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
