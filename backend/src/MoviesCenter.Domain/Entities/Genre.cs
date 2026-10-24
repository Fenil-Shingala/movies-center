namespace MoviesCenter.Domain.Entities;

public class Genre
{
    public int Id { get; set; } // TMDB Genre ID (e.g. 28 = Action)
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TitleGenre> TitleGenres { get; set; } = new List<TitleGenre>();
}
