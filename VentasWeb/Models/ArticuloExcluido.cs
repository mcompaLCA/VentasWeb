using Microsoft.EntityFrameworkCore;

namespace VentasWeb.Models;

public class ArticuloExcluido
{
    public string SKU { get; set; } = string.Empty;

    public async Task Guardar(AppDBContext db, string[] lineas)
    {
        db.ArticulosExcluidos.RemoveRange(db.ArticulosExcluidos.AsNoTracking());

        var skusUnicos = lineas
            .Select(l => l.Split(';').FirstOrDefault()?.Trim()) // tomar el primer campo (SKU)
            .Where(sku => !string.IsNullOrEmpty(sku))           // descartar vacíos
            .Distinct(StringComparer.OrdinalIgnoreCase)         // eliminar duplicados (case-insensitive)
            .ToList();


        foreach (var sku in skusUnicos)
        {
            await db.ArticulosExcluidos.AddAsync(new ArticuloExcluido { SKU = sku! });
        }
        await db.SaveChangesAsync();
    }

}