using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Clinic.DAL.Migrations
{
    /// <inheritdoc />
    public partial class HardenValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Safely backfill existing test rows to prevent migration failures on existing databases
            migrationBuilder.Sql("UPDATE [Patients] SET [PhoneNumber] = '01000000000' WHERE [PhoneNumber] IS NULL;");
            migrationBuilder.Sql("UPDATE [Patients] SET [PhoneNumber] = LEFT([PhoneNumber], 11) WHERE LEN([PhoneNumber]) > 11;");
            migrationBuilder.Sql("UPDATE [Patients] SET [DateOfBirth] = '2000-01-01' WHERE [DateOfBirth] IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Patients",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "DateOfBirth",
                table: "Patients",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "Doctors",
                columns: new[] { "Id", "ClinicId", "Name", "Specialty" },
                values: new object[] { 2, 1, "Dr. Sara", "Pediatrics" });

            migrationBuilder.InsertData(
                table: "DoctorSchedules",
                columns: new[] { "Id", "DayOfWeek", "DoctorId", "EndTime", "StartTime" },
                values: new object[,]
                {
                    { 7, 0, 2, new TimeOnly(14, 0, 0), new TimeOnly(10, 0, 0) },
                    { 8, 2, 2, new TimeOnly(14, 0, 0), new TimeOnly(10, 0, 0) },
                    { 9, 4, 2, new TimeOnly(14, 0, 0), new TimeOnly(10, 0, 0) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DoctorSchedules",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "DoctorSchedules",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "DoctorSchedules",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Patients",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(11)",
                oldMaxLength: 11);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "DateOfBirth",
                table: "Patients",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");
        }
    }
}
