using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MyApp.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DetectedObjectDetails",
                columns: table => new
                {
                    ObjectDetailsId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Image = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetectedObjectDetails", x => x.ObjectDetailsId);
                });

            migrationBuilder.CreateTable(
                name: "DetectedObjects",
                columns: table => new
                {
                    DetectedObjectId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    X1 = table.Column<int>(type: "INTEGER", nullable: false),
                    Y1 = table.Column<int>(type: "INTEGER", nullable: false),
                    X2 = table.Column<int>(type: "INTEGER", nullable: false),
                    Y2 = table.Column<int>(type: "INTEGER", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: true),
                    DetailsObjectDetailsId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetectedObjects", x => x.DetectedObjectId);
                    table.ForeignKey(
                        name: "FK_DetectedObjects_DetectedObjectDetails_DetailsObjectDetailsId",
                        column: x => x.DetailsObjectDetailsId,
                        principalTable: "DetectedObjectDetails",
                        principalColumn: "ObjectDetailsId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetectedObjects_DetailsObjectDetailsId",
                table: "DetectedObjects",
                column: "DetailsObjectDetailsId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetectedObjects");

            migrationBuilder.DropTable(
                name: "DetectedObjectDetails");
        }
    }
}
