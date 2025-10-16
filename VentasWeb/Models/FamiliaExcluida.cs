using Microsoft.EntityFrameworkCore;

namespace VentasWeb.Models;

public class FamiliaExcluida
{
    public string Codigo { get; set; } = string.Empty;


    public async Task Guardar(AppDBContext db, string[] lineas)
    {
        db.FamiliasExcluidas.RemoveRange(db.FamiliasExcluidas.AsNoTracking());

        var codFamilias = lineas
            .Select(l => l.Split(';').FirstOrDefault()?.Trim()) // tomar el primer campo
            .Where(cod => !string.IsNullOrEmpty(cod))           // descartar vacíos
            .Distinct(StringComparer.OrdinalIgnoreCase)         // eliminar duplicados (case-insensitive)
            .ToList();

        foreach (var cod in codFamilias)
        {
            await db.FamiliasExcluidas.AddAsync(new FamiliaExcluida() { Codigo = cod!});
        }
        await db.SaveChangesAsync();
    }
}
