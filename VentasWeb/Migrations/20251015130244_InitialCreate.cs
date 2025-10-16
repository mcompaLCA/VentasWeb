using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VentasWeb.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Web_ArticulosExcluidos",
                schema: "dbo",
                columns: table => new
                {
                    SKU = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Web_ArticulosExcluidos", x => x.SKU);
                });

            migrationBuilder.CreateTable(
                name: "Web_FamiliasExcluidas",
                schema: "dbo",
                columns: table => new
                {
                    Codigo = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Web_FamiliasExcluidas", x => x.Codigo);
                });

            migrationBuilder.CreateTable(
                name: "Web_PreciosForzados",
                schema: "dbo",
                columns: table => new
                {
                    Sku = table.Column<string>(type: "TEXT", nullable: false),
                    PrecioLista = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    PrecioVenta = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    FranjaMkp = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Web_PreciosForzados", x => x.Sku);
                });

            migrationBuilder.CreateTable(
                name: "Web_SucursalesExcluidas",
                schema: "dbo",
                columns: table => new
                {
                    Numero = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Web_SucursalesExcluidas", x => x.Numero);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Web_ArticulosExcluidos",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Web_FamiliasExcluidas",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Web_PreciosForzados",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Web_SucursalesExcluidas",
                schema: "dbo");
        }
    }
}
