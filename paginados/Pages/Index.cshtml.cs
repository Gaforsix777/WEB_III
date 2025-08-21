using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using WARazor.Models;

namespace WARazor.Pages
{
    // Conversor dd/MM/yyyy
    public class DateTimeConverterDdMMyyyy : JsonConverter<DateTime>
    {
        private static readonly string[] formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" };
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString();
            if (DateTime.TryParseExact(s, formats, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dt)) return dt;
            return DateTime.Parse(s!);
        }
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString("dd/MM/yyyy"));
    }

    public class IndexModel : PageModel
    {
        public List<Tarea> Tareas { get; set; } = new();
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
        public int TamanoPagina { get; set; } = 5;

        // filtros (solo lectura en GET para redisplay)
        public string? Buscar { get; set; }
        public string Orden { get; set; } = "az";

        // Para crear desde el modal
        [BindProperty]
        public Tarea Nueva { get; set; } = new();

        private readonly ILogger<IndexModel> _logger;
        public IndexModel(ILogger<IndexModel> logger) => _logger = logger;

        private string JsonPath =>
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "tareas.json");

        private static JsonSerializerOptions GetJsonOptions()
        {
            var o = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };
            o.Converters.Add(new DateTimeConverterDdMMyyyy());
            return o;
        }

        public void OnGet(int pagina = 1, string? q = null, string? order = "az", int? tam = null)
        {
            if (tam.HasValue && tam.Value > 0) TamanoPagina = tam.Value;
            Buscar = q;
            Orden = order ?? "az";

            var todas = LeerTodas();

            // Buscar
            if (!string.IsNullOrWhiteSpace(Buscar))
                todas = todas.Where(t => t.nombreTarea.Contains(Buscar, StringComparison.OrdinalIgnoreCase)).ToList();

            // Orden
            todas = Orden switch
            {
                "za" => todas.OrderByDescending(t => t.nombreTarea).ToList(),
                "fasc" => todas.OrderBy(t => t.fechaVencimiento).ToList(),
                "fdesc" => todas.OrderByDescending(t => t.fechaVencimiento).ToList(),
                _ => todas.OrderBy(t => t.nombreTarea).ToList(), // az
            };

            // Paginación
            PaginaActual = pagina < 1 ? 1 : pagina;
            TotalPaginas = Math.Max(1, (int)Math.Ceiling(todas.Count / (double)TamanoPagina));
            if (PaginaActual > TotalPaginas) PaginaActual = TotalPaginas;

            Tareas = todas.Skip((PaginaActual - 1) * TamanoPagina).Take(TamanoPagina).ToList();
        }

        public IActionResult OnPostCreate()
        {
            try
            {
                var lista = LeerTodas();

                // Validación mínima
                if (string.IsNullOrWhiteSpace(Nueva.nombreTarea))
                    ModelState.AddModelError("Nueva.nombreTarea", "El nombre es obligatorio.");
                if (!ModelState.IsValid)
                    return RedirectToPage("/Index");

                lista.Add(new Tarea
                {
                    nombreTarea = Nueva.nombreTarea.Trim(),
                    fechaVencimiento = Nueva.fechaVencimiento,
                    estado = string.IsNullOrWhiteSpace(Nueva.estado) ? "Pendiente" : Nueva.estado.Trim()
                });

                var json = JsonSerializer.Serialize(lista, GetJsonOptions());
                System.IO.File.WriteAllText(JsonPath, json);

                TempData["ok"] = "Tarea registrada.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear tarea");
                TempData["err"] = "No se pudo guardar la tarea.";
            }
            return RedirectToPage("/Index");
        }

        private List<Tarea> LeerTodas()
        {
            if (!System.IO.File.Exists(JsonPath)) return new();
            var content = System.IO.File.ReadAllText(JsonPath);
            return JsonSerializer.Deserialize<List<Tarea>>(content, GetJsonOptions()) ?? new();
        }
    }
}
