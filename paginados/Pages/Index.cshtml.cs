using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using WARazor.Models;

namespace WARazor.Pages
{
    // Conversor dd/MM/yyyy (acepta yyyy-MM-dd del <input type="date">)
    public class DateTimeConverterDdMMyyyy : JsonConverter<DateTime>
    {
        private static readonly string[] formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" };
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString();
            if (string.IsNullOrWhiteSpace(s)) return default;
            if (DateTime.TryParseExact(s, formats, System.Globalization.CultureInfo.InvariantCulture,
                                       System.Globalization.DateTimeStyles.None, out var dt)) return dt;
            return DateTime.Parse(s);
        }
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString("dd/MM/yyyy"));
    }

    public class IndexModel : PageModel
    {
        // Datos para la vista
        public List<Tarea> Tareas { get; set; } = new();
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
        public int TamanoPagina { get; set; } = 5;

        // Filtros
        public string? Buscar { get; set; }
        public string Orden { get; set; } = "az";
        public List<string> EstadosSeleccionados { get; set; } = new();

        // Para el modal de creación
        [BindProperty] public Tarea Nueva { get; set; } = new();

        private readonly ILogger<IndexModel> _logger;
        public IndexModel(ILogger<IndexModel> logger) => _logger = logger;

        private string JsonPath => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "tareas.json");

        private static JsonSerializerOptions GetJsonOptions()
        {
            var o = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };
            o.Converters.Add(new DateTimeConverterDdMMyyyy());
            return o;
        }

        // ---------- GET ----------
        public void OnGet(
            int pagina = 1,
            string? q = null,
            string? order = "az",
            int? tam = null,
            string? estado = null,         // atajo desde menú (Finalizado/Cancelado)
            string[]? estados = null       // checkboxes múltiples (Pendiente/En curso)
        )
        {
            if (tam.HasValue && tam.Value > 0) TamanoPagina = tam.Value;
            Buscar = q;
            Orden = order ?? "az";

            var todas = LeerTodasConIds();

            // Estados a mostrar
            IEnumerable<string> estadosFiltro =
                !string.IsNullOrWhiteSpace(estado) ? new[] { estado } :
                (estados != null && estados.Length > 0) ? estados :
                new[] { "Pendiente", "En curso" }; // por defecto
            EstadosSeleccionados = estadosFiltro.ToList();

            // Filtrado
            todas = todas.Where(t => EstadosSeleccionados.Contains(t.estado, StringComparer.OrdinalIgnoreCase)).ToList();

            // Buscar
            if (!string.IsNullOrWhiteSpace(Buscar))
                todas = todas.Where(t => t.nombreTarea.Contains(Buscar, StringComparison.OrdinalIgnoreCase)).ToList();

            // Orden
            todas = Orden switch
            {
                "za" => todas.OrderByDescending(t => t.nombreTarea).ToList(),
                "fasc" => todas.OrderBy(t => t.fechaVencimiento).ToList(),
                "fdesc" => todas.OrderByDescending(t => t.fechaVencimiento).ToList(),
                _ => todas.OrderBy(t => t.nombreTarea).ToList(),
            };

            // Paginado
            PaginaActual = pagina < 1 ? 1 : pagina;
            TotalPaginas = Math.Max(1, (int)Math.Ceiling(todas.Count / (double)TamanoPagina));
            if (PaginaActual > TotalPaginas) PaginaActual = TotalPaginas;

            Tareas = todas.Skip((PaginaActual - 1) * TamanoPagina).Take(TamanoPagina).ToList();
        }

        // ---------- POST: crear ----------
        public IActionResult OnPostCreate()
        {
            try
            {
                var lista = LeerTodasConIds();
                if (string.IsNullOrWhiteSpace(Nueva?.nombreTarea))
                {
                    TempData["err"] = "El nombre de la tarea es obligatorio.";
                    return RedirectToPage("/Index");
                }

                var nuevoId = (lista.Count == 0) ? 1 : lista.Max(x => x.Id) + 1;
                lista.Add(new Tarea
                {
                    Id = nuevoId,
                    nombreTarea = Nueva.nombreTarea.Trim(),
                    fechaVencimiento = Nueva.fechaVencimiento,
                    estado = string.IsNullOrWhiteSpace(Nueva.estado) ? "Pendiente" : Nueva.estado.Trim()
                });

                Guardar(lista);
                TempData["ok"] = "Tarea registrada.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear tarea");
                TempData["err"] = "No se pudo guardar la tarea.";
            }
            return RedirectToPage("/Index");
        }

        // ---------- POST: cambiar estado (Finalizar / Cancelar) ----------
        public IActionResult OnPostEstado(int id, string estado, int pagina, string? q, string? order, int? tam, string[]? estados)
        {
            try
            {
                var lista = LeerTodasConIds();
                var tarea = lista.FirstOrDefault(x => x.Id == id);
                if (tarea == null) { TempData["err"] = "Tarea no encontrada."; return RedirectToPage("/Index"); }

                tarea.estado = estado; // "Finalizado" o "Cancelado"
                Guardar(lista);
                TempData["ok"] = $"Tarea actualizada a {estado}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado");
                TempData["err"] = "No se pudo actualizar la tarea.";
            }

            // volver preservando filtros / paginado
            return RedirectToPage("/Index", new { pagina, q, order, tam, estados });
        }

        // ---------- Helpers ----------
        private List<Tarea> LeerTodasConIds()
        {
            var res = LeerTodas();
            int next = 1;
            foreach (var t in res)
            {
                if (t.Id == 0) t.Id = next++;
                else next = Math.Max(next, t.Id + 1);
            }
            return res;
        }

        private List<Tarea> LeerTodas()
        {
            try
            {
                if (!System.IO.File.Exists(JsonPath)) return new();
                var content = System.IO.File.ReadAllText(JsonPath);
                return JsonSerializer.Deserialize<List<Tarea>>(content, GetJsonOptions()) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leyendo {path}", JsonPath);
                return new();
            }
        }

        private void Guardar(List<Tarea> lista)
        {
            var json = JsonSerializer.Serialize(lista, GetJsonOptions());
            System.IO.File.WriteAllText(JsonPath, json);
        }
    }
}
