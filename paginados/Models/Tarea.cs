namespace WARazor.Models
{
    public class Tarea
    {
        public int Id { get; set; }                 // << Id para poder actualizar
        public string nombreTarea { get; set; } = "";
        public DateTime fechaVencimiento { get; set; }
        public string estado { get; set; } = "";
    }
}
