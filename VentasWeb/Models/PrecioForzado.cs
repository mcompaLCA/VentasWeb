using Microsoft.EntityFrameworkCore;
namespace VentasWeb.Models;

public class PrecioForzado
{
    public string Sku { get; set; } = string.Empty;

    public decimal? PrecioLista { get; set; }

    public decimal? PrecioVenta { get; set; }

    public int? FranjaMkp { get; set; }

    public async Task Guardar(AppDBContext context, string[] lineas)
    {
        context.PreciosForzados.RemoveRange(context.PreciosForzados.AsNoTracking());
        var precios = lineas
            .Select(linea =>
                {
                    var datos = linea.Split(';');
                    if (datos.Length < 4) return null;

                    var sku = datos[0].Trim();
                    if (string.IsNullOrEmpty(sku)) return null;

                    return new PrecioForzado
                    {
                        Sku = sku,
                        PrecioLista = decimal.TryParse(datos[1].Trim(), out var pl) ? pl : null,
                        PrecioVenta = decimal.TryParse(datos[2].Trim(), out var pv) ? pv : null,
                        FranjaMkp = int.TryParse(datos[3].Trim(), out var fm) ? fm : null
                    };
                })
            .Where(x => x != null)
            .GroupBy(x => x!.Sku, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First()) // o g.Last() si querés conservar la última aparición
            .ToList();

        // Insertar los SKUs únicos
        await context.PreciosForzados.AddRangeAsync(precios!);

        await context.SaveChangesAsync();
    }

}