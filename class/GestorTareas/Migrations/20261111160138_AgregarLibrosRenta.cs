using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorTareas.Migrations
{
    /// <inheritdoc />
    public partial class AgregarLibrosRenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Libros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Autor = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Genero = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Rentado = table.Column<bool>(type: "INTEGER", nullable: false),
                    RentadoPor = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FechaRenta = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaDevolucion = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Libros", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Libros");
        }
    }
}
