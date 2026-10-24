using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MoviesCenter.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "genres",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_genres", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "source_providers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    base_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    embed_url_template = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    priority = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_source_providers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "titles",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tmdb_id = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    title_text = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    original_title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    overview = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    poster_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    backdrop_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    release_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    runtime_minutes = table.Column<int>(type: "int", nullable: true),
                    imdb_rating = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: true),
                    tmdb_rating = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: true),
                    content_rating = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    original_language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    total_seasons = table.Column<int>(type: "int", nullable: true),
                    total_episodes = table.Column<int>(type: "int", nullable: true),
                    is_featured = table.Column<bool>(type: "bit", nullable: false),
                    view_count = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_titles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    avatar_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    user_name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "bit", nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    security_stamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phone_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "bit", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "bit", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "bit", nullable: false),
                    access_failed_count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    role_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    claim_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    claim_value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "f_k_role_claims__asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "title_genres",
                columns: table => new
                {
                    title_id = table.Column<int>(type: "int", nullable: false),
                    genre_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_title_genres", x => new { x.title_id, x.genre_id });
                    table.ForeignKey(
                        name: "f_k_title_genres_genres_genre_id",
                        column: x => x.genre_id,
                        principalTable: "genres",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_title_genres_titles_title_id",
                        column: x => x.title_id,
                        principalTable: "titles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "title_sources",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title_id = table.Column<int>(type: "int", nullable: false),
                    source_provider_id = table.Column<int>(type: "int", nullable: false),
                    season_number = table.Column<int>(type: "int", nullable: true),
                    episode_number = table.Column<int>(type: "int", nullable: true),
                    embed_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    stream_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    quality = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    priority = table.Column<int>(type: "int", nullable: false),
                    is_auto_generated = table.Column<bool>(type: "bit", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    last_verified_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_title_sources", x => x.id);
                    table.ForeignKey(
                        name: "f_k_title_sources_source_providers_source_provider_id",
                        column: x => x.source_provider_id,
                        principalTable: "source_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_title_sources_titles_title_id",
                        column: x => x.title_id,
                        principalTable: "titles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    claim_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    claim_value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "f_k_user_claims__asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_logins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    provider_key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    provider_display_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "f_k_user_logins__asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    role_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "f_k_user_roles__asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_user_roles__asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_tokens",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    login_provider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "f_k_user_tokens__asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "genres",
                columns: new[] { "id", "created_at", "name", "slug" },
                values: new object[,]
                {
                    { 12, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Adventure", "adventure" },
                    { 14, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Fantasy", "fantasy" },
                    { 16, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Animation", "animation" },
                    { 18, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Drama", "drama" },
                    { 27, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Horror", "horror" },
                    { 28, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Action", "action" },
                    { 35, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Comedy", "comedy" },
                    { 36, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "History", "history" },
                    { 37, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Western", "western" },
                    { 53, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Thriller", "thriller" },
                    { 80, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Crime", "crime" },
                    { 99, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Documentary", "documentary" },
                    { 878, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Science Fiction", "science-fiction" },
                    { 9648, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mystery", "mystery" },
                    { 10402, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Music", "music" },
                    { 10749, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Romance", "romance" },
                    { 10751, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Family", "family" },
                    { 10752, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "War", "war" },
                    { 10759, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Action & Adventure", "action-adventure" },
                    { 10762, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kids", "kids" },
                    { 10763, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "News", "news" },
                    { 10764, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Reality", "reality" },
                    { 10765, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sci-Fi & Fantasy", "sci-fi-fantasy" },
                    { 10766, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Soap", "soap" },
                    { 10767, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Talk", "talk" },
                    { 10768, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "War & Politics", "war-politics" },
                    { 10770, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TV Movie", "tv-movie" }
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "concurrency_stamp", "created_at", "description", "name", "normalized_name" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "11111111-1111-1111-1111-111111111111", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System Administrator", "Admin", "ADMIN" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "22222222-2222-2222-2222-222222222222", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Registered User", "User", "USER" }
                });

            migrationBuilder.InsertData(
                table: "source_providers",
                columns: new[] { "id", "base_url", "created_at", "embed_url_template", "is_active", "name", "priority", "slug", "type", "updated_at" },
                values: new object[] { 1, "https://vidsrc.to", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "https://vidsrc.to/embed/movie/{tmdbId}", true, "VidSrc", 1, "vidsrc", "ThirdPartyEmbed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "id", "access_failed_count", "avatar_url", "concurrency_stamp", "created_at", "email", "email_confirmed", "is_active", "last_login_at", "lockout_enabled", "lockout_end", "normalized_email", "normalized_user_name", "password_hash", "phone_number", "phone_number_confirmed", "security_stamp", "two_factor_enabled", "updated_at", "user_name" },
                values: new object[] { new Guid("33333333-3333-3333-3333-333333333333"), 0, null, "33333333-3333-3333-3333-333333333333", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin.movies@gmail.com", true, true, null, false, null, "ADMIN.MOVIES@GMAIL.COM", "ADMIN.MOVIES@GMAIL.COM", "AQAAAAIAAYagAAAAEHyTExCf0a3qNoZP8kS2xFRsTuTaumN5encVKv+5LoqHYDp+Q/qRMjGyKVk5GuCQ0g==", null, false, "STATIC_SECURITY_STAMP_SEED_2026", false, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin.movies@gmail.com" });

            migrationBuilder.InsertData(
                table: "user_roles",
                columns: new[] { "role_id", "user_id" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), new Guid("33333333-3333-3333-3333-333333333333") });

            migrationBuilder.CreateIndex(
                name: "i_x_genres_slug",
                table: "genres",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_role_claims_role_id",
                table: "role_claims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "role_name_index",
                table: "roles",
                column: "normalized_name",
                unique: true,
                filter: "[normalized_name] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "i_x_source_providers_slug",
                table: "source_providers",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_title_genres_genre_id",
                table: "title_genres",
                column: "genre_id");

            migrationBuilder.CreateIndex(
                name: "i_x_title_sources_source_provider_id",
                table: "title_sources",
                column: "source_provider_id");

            migrationBuilder.CreateIndex(
                name: "i_x_title_sources_title_id_source_provider_id_season_number_episode_number",
                table: "title_sources",
                columns: new[] { "title_id", "source_provider_id", "season_number", "episode_number" });

            migrationBuilder.CreateIndex(
                name: "i_x_titles_is_featured",
                table: "titles",
                column: "is_featured");

            migrationBuilder.CreateIndex(
                name: "i_x_titles_tmdb_id",
                table: "titles",
                column: "tmdb_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_titles_type",
                table: "titles",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "i_x_user_claims_user_id",
                table: "user_claims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_user_logins_user_id",
                table: "user_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "email_index",
                table: "users",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "user_name_index",
                table: "users",
                column: "normalized_user_name",
                unique: true,
                filter: "[normalized_user_name] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "role_claims");

            migrationBuilder.DropTable(
                name: "title_genres");

            migrationBuilder.DropTable(
                name: "title_sources");

            migrationBuilder.DropTable(
                name: "user_claims");

            migrationBuilder.DropTable(
                name: "user_logins");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "user_tokens");

            migrationBuilder.DropTable(
                name: "genres");

            migrationBuilder.DropTable(
                name: "source_providers");

            migrationBuilder.DropTable(
                name: "titles");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
