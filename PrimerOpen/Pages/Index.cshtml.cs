using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PrimerOpen.Models;
using PrimerOpen.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PrimerOpen.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IServicioTareas _servicio;
        public IndexModel(IServicioTareas servicio) { _servicio = servicio; }

        [BindProperty(SupportsGet = true)]
        public string? Buscar { get; set; }

        [BindProperty(SupportsGet = true)]
        public int pagina { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int tamPagina { get; set; } = 10;

        public List<Tarea> Tareas { get; set; } = new();
        public PaginacionInfo Paginacion { get; set; } = new();

        public void OnGet()
        {
            var data = _servicio.ObtenerTodas()
                                .Where(t => t.Estado == EstadoTarea.Pendiente || t.Estado == EstadoTarea.EnProgreso);

            if (!string.IsNullOrWhiteSpace(Buscar))
            {
                var q = Buscar.Trim().ToLower();
                data = data.Where(t =>
                    (t.Titulo?.ToLower().Contains(q) ?? false) ||
                    (t.Responsable?.ToLower().Contains(q) ?? false));
            }

            // Ordenar antes de paginar
            var lista = data.OrderBy(t => t.FechaVencimiento).ToList();

            // Paginación
            if (tamPagina <= 0) tamPagina = 10;
            if (pagina <= 0) pagina = 1;

            Paginacion = new PaginacionInfo
            {
                PaginaActual = pagina,
                TamPagina = tamPagina,
                TotalRegistros = lista.Count
            };

            Tareas = lista.Skip((pagina - 1) * tamPagina).Take(tamPagina).ToList();
        }

        public IActionResult OnPostFinalizar(int id)
        {
            _servicio.Finalizar(id);
            return RedirectToPage(new { Buscar, pagina, tamPagina });
        }

        public IActionResult OnPostCancelar(int id, string? motivo)
        {
            _servicio.Cancelar(id, motivo);
            return RedirectToPage(new { Buscar, pagina, tamPagina });
        }
    }
}
