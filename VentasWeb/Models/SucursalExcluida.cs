using Microsoft.EntityFrameworkCore;

namespace VentasWeb.Models;

public class SucursalExcluida
{
    public int Numero { get; set; }

    public async Task Guardar(AppDBContext db, string[] lineas)
    {
        db.SucursalesExcluidas.RemoveRange(db.SucursalesExcluidas.AsNoTracking());

        var codSucs = lineas
            .Select(l => l.Split(';').FirstOrDefault()?.Trim()) // tomar el primer campo
            .Where(cod => !string.IsNullOrEmpty(cod))           // descartar vacíos
            .Distinct(StringComparer.OrdinalIgnoreCase)         // eliminar duplicados (case-insensitive)
            .ToList();
        foreach (var nro in codSucs)
        {
            if (!int.TryParse(nro, out var numero))
                continue;
            var nuevo = new SucursalExcluida
            {
                Numero = numero
            };
            await db.SucursalesExcluidas.AddAsync(nuevo);
        }
        await db.SaveChangesAsync();
    }

}
