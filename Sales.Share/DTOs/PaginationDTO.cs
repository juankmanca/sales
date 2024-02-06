namespace Sales.Share.DTOs
{
    public class PaginationDTO
    {
        public int Id { get; set; }

        public string? Filter { get; set; }

        public int Page { get; set; } = 1;

        public int RecordsNumber { get; set; } = 10;

        public string? CategoryFilter { get; set; }
    }

}
