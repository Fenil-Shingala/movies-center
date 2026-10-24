namespace MoviesCenter.Domain.Entities;

public class TitleGenre
{
    public int TitleId { get; set; }
    public Title Title { get; set; } = null!;

    public int GenreId { get; set; }
    public Genre Genre { get; set; } = null!;
}
