namespace TaskManagerApi.Dtos
{
    public record TaskItemCreateDto(string Title, string? Description, bool IsCompleted);
    public record TaskItemUpdateDto(string? Title, string? Description, bool? IsCompleted);
}
