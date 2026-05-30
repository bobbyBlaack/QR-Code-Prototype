using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QR_Code_Prototype.Migrations
{
    /// <inheritdoc />
    public partial class InitialApiModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Packages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PackageReference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeliveredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QrCodeRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PackageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Token = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QrCodeRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QrCodeRecords_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QrScanEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    QrCodeRecordId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Token = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ScannedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    WasSuccessful = table.Column<bool>(type: "INTEGER", nullable: false),
                    FailureReason = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QrScanEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QrScanEvents_QrCodeRecords_QrCodeRecordId",
                        column: x => x.QrCodeRecordId,
                        principalTable: "QrCodeRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_Email",
                table: "AppUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Packages_PackageReference",
                table: "Packages",
                column: "PackageReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QrCodeRecords_PackageId",
                table: "QrCodeRecords",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_QrCodeRecords_Token",
                table: "QrCodeRecords",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QrScanEvents_QrCodeRecordId",
                table: "QrScanEvents",
                column: "QrCodeRecordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropTable(
                name: "QrScanEvents");

            migrationBuilder.DropTable(
                name: "QrCodeRecords");

            migrationBuilder.DropTable(
                name: "Packages");
        }
    }
}
