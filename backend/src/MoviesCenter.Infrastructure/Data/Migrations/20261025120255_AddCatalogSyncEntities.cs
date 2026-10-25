using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoviesCenter.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogSyncEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "first_air_date",
                table: "titles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "titles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_tmdb_sync_at",
                table: "titles",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "popularity",
                table: "titles",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "tagline",
                table: "titles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tmdb_list_category",
                table: "titles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "vote_count",
                table: "titles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "people",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tmdb_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    profile_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_people", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "providers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tmdb_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    logo_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_providers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "seasons",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title_id = table.Column<int>(type: "int", nullable: false),
                    tmdb_id = table.Column<int>(type: "int", nullable: false),
                    season_number = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    overview = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    poster_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    air_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    episode_count = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_seasons", x => x.id);
                    table.ForeignKey(
                        name: "f_k_seasons__titles_title_id",
                        column: x => x.title_id,
                        principalTable: "titles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tmdb_sync_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    started_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    items_synced = table.Column<int>(type: "int", nullable: false),
                    errors = table.Column<int>(type: "int", nullable: false),
                    triggered_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    error_message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_tmdb_sync_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "videos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title_id = table.Column<int>(type: "int", nullable: false),
                    tmdb_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    site = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_videos", x => x.id);
                    table.ForeignKey(
                        name: "f_k_videos_titles_title_id",
                        column: x => x.title_id,
                        principalTable: "titles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "title_casts",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title_id = table.Column<int>(type: "int", nullable: false),
                    person_id = table.Column<int>(type: "int", nullable: false),
                    character = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_title_casts", x => x.id);
                    table.ForeignKey(
                        name: "f_k_title_casts_people_person_id",
                        column: x => x.person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_title_casts_titles_title_id",
                        column: x => x.title_id,
                        principalTable: "titles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "title_providers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title_id = table.Column<int>(type: "int", nullable: false),
                    provider_id = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    country = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_title_providers", x => x.id);
                    table.ForeignKey(
                        name: "f_k_title_providers_providers_provider_id",
                        column: x => x.provider_id,
                        principalTable: "providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_title_providers_titles_title_id",
                        column: x => x.title_id,
                        principalTable: "titles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "episodes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    season_id = table.Column<int>(type: "int", nullable: false),
                    tmdb_id = table.Column<int>(type: "int", nullable: false),
                    episode_number = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    overview = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    still_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    air_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    runtime_minutes = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_episodes", x => x.id);
                    table.ForeignKey(
                        name: "f_k_episodes__seasons_season_id",
                        column: x => x.season_id,
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "password_hash",
                value: "AQAAAAIAAYagAAAAEEJ7J969P+HcTqhCi+a6XFHWxBRT2GFmP4qeD0teG8cs4L9zBX2KEyaFpJ/Tzjmamg==");

            migrationBuilder.CreateIndex(
                name: "i_x_titles_popularity",
                table: "titles",
                column: "popularity");

            migrationBuilder.CreateIndex(
                name: "i_x_titles_release_date",
                table: "titles",
                column: "release_date");

            migrationBuilder.CreateIndex(
                name: "i_x_episodes_season_id_episode_number",
                table: "episodes",
                columns: new[] { "season_id", "episode_number" });

            migrationBuilder.CreateIndex(
                name: "i_x_people_tmdb_id",
                table: "people",
                column: "tmdb_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_providers_tmdb_id",
                table: "providers",
                column: "tmdb_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_seasons_title_id_season_number",
                table: "seasons",
                columns: new[] { "title_id", "season_number" });

            migrationBuilder.CreateIndex(
                name: "i_x_title_casts_person_id",
                table: "title_casts",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "i_x_title_casts_title_id_person_id",
                table: "title_casts",
                columns: new[] { "title_id", "person_id" });

            migrationBuilder.CreateIndex(
                name: "i_x_title_providers_provider_id",
                table: "title_providers",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "i_x_title_providers_title_id_provider_id",
                table: "title_providers",
                columns: new[] { "title_id", "provider_id" });

            migrationBuilder.CreateIndex(
                name: "i_x_tmdb_sync_logs_started_at",
                table: "tmdb_sync_logs",
                column: "started_at");

            migrationBuilder.CreateIndex(
                name: "i_x_videos_title_id",
                table: "videos",
                column: "title_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "episodes");

            migrationBuilder.DropTable(
                name: "title_casts");

            migrationBuilder.DropTable(
                name: "title_providers");

            migrationBuilder.DropTable(
                name: "tmdb_sync_logs");

            migrationBuilder.DropTable(
                name: "videos");

            migrationBuilder.DropTable(
                name: "seasons");

            migrationBuilder.DropTable(
                name: "people");

            migrationBuilder.DropTable(
                name: "providers");

            migrationBuilder.DropIndex(
                name: "i_x_titles_popularity",
                table: "titles");

            migrationBuilder.DropIndex(
                name: "i_x_titles_release_date",
                table: "titles");

            migrationBuilder.DropColumn(
                name: "first_air_date",
                table: "titles");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "titles");

            migrationBuilder.DropColumn(
                name: "last_tmdb_sync_at",
                table: "titles");

            migrationBuilder.DropColumn(
                name: "popularity",
                table: "titles");

            migrationBuilder.DropColumn(
                name: "tagline",
                table: "titles");

            migrationBuilder.DropColumn(
                name: "tmdb_list_category",
                table: "titles");

            migrationBuilder.DropColumn(
                name: "vote_count",
                table: "titles");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "password_hash",
                value: "AQAAAAIAAYagAAAAEHyTExCf0a3qNoZP8kS2xFRsTuTaumN5encVKv+5LoqHYDp+Q/qRMjGyKVk5GuCQ0g==");
        }
    }
}
