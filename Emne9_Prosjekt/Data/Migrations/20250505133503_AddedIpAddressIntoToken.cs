using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emne9_Prosjekt.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedIpAddressIntoToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "MemberRefreshToken",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "MemberRefreshToken");
        }
    }
}
