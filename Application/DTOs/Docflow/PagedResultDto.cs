namespace WsUtaSystem.Application.DTOs.Docflow
{
    public sealed class PagedResultDto<T>
    {
        public required IReadOnlyList<T> Items { get; init; }
        public required int Page { get; init; }
        public required int PageSize { get; init; }
        public required long Total { get; init; }
    }
}
