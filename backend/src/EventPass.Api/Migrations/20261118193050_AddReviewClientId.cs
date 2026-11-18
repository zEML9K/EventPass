using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventPass.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewClientId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "EventReviews",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ClientIp",
                table: "EventReviews",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "EventReviews");

            migrationBuilder.DropColumn(
                name: "ClientIp",
                table: "EventReviews");
        }
    }
}
