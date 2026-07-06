namespace RadianciaKS.Application.DTOs
{
    public class BaseQueryParameters
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public string? SearchTerm { get; set; }

        public string? SortBy { get; set; }
        public bool IsDescending { get; set; } = false;
    }
}