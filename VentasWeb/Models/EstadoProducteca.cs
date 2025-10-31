using Microsoft.EntityFrameworkCore;
using VentasWeb.Services;
namespace VentasWeb.Models;

public class EstadoProducteca
{
    
    public async Task<List<string>> Procesar(HttpClientService http, string[] lineas)
    {
        ///estado_producteca/Escribir_Envio_Desde_Depo?IdProducteca=1234&Estado=xxxx

        List<string> resultados = [];
        foreach (var linea in lineas)
        {
            var datos = linea.Split(';');
            if (datos.Length < 2) resultados.Add("Linea mal formada: " + linea);

            var IdProducteca = datos[0].Trim();
            var LetraEstado = datos[1].Trim();

            string Estado = LetraEstado switch
            {
                "E" => "fin",
                "C" => "cancelado",
                "F" => "no_entregado",
                _ => "no es ninguno"
            };
            //F no lo conoce l a api, pero por default es no entregado, asi que lo mapeo igual

            var resp = await http.PostAsJsonAsync(
                "Escribir_Envio_Desde_Depo",
                new
                {
                    IdProducteca,
                    Estado
                },
                "ProductecaLCA"
            );
            var estadoCode = resp?.RootElement.GetProperty("success").GetBoolean();
            var Mensaje = resp?.RootElement.GetProperty("message").GetString();
            if (!estadoCode ?? true)
            {
                resultados.Add("La api a producteca informo: " + Mensaje);
            }
        }
        return resultados;
        
    }

}