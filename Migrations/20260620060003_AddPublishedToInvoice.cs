using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace smart_hostel_management_system.Migrations
{
    /// <inheritdoc />
    public partial class AddPublishedToInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Invoices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "Invoices",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Invoices");
        }
    }
}
