using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoviesCenter.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsDailyStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "analytics_daily_stats",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    metric_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    metric_value = table.Column<long>(type: "bigint", nullable: false),
                    dimension = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_analytics_daily_stats", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    new_episode_in_app = table.Column<bool>(type: "bit", nullable: false),
                    new_episode_email = table.Column<bool>(type: "bit", nullable: false),
                    new_episode_push = table.Column<bool>(type: "bit", nullable: false),
                    continue_watching_in_app = table.Column<bool>(type: "bit", nullable: false),
                    continue_watching_email = table.Column<bool>(type: "bit", nullable: false),
                    continue_watching_push = table.Column<bool>(type: "bit", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_notification_settings", x => x.id);
                    table.ForeignKey(
                        name: "f_k_notification_settings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    body = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    data = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_read = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    read_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_notifications", x => x.id);
                    table.ForeignKey(
                        name: "f_k_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "push_subscriptions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    endpoint = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    p256dh = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    auth = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    user_agent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_push_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "f_k_push_subscriptions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ratings",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    title_id = table.Column<int>(type: "int", nullable: false),
                    score = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_ratings", x => x.id);
                    table.ForeignKey(
                        name: "f_k_ratings__titles_title_id",
                        column: x => x.title_id,
                        principalTable: "titles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_ratings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    replaced_by_token = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_by_ip = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    revoked_by_ip = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "f_k_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "source_reports",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    source_id = table.Column<int>(type: "int", nullable: false),
                    title_id = table.Column<int>(type: "int", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    details = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    season = table.Column<int>(type: "int", nullable: true),
                    episode = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_source_reports", x => x.id);
                    table.ForeignKey(
                        name: "f_k_source_reports__title_sources_source_id",
                        column: x => x.source_id,
                        principalTable: "title_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_source_reports__titles_title_id",
                        column: x => x.title_id,
                        principalTable: "titles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "watch_history_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    title_id = table.Column<int>(type: "int", nullable: false),
                    episode_id = table.Column<int>(type: "int", nullable: true),
                    season_number = table.Column<int>(type: "int", nullable: true),
                    episode_number = table.Column<int>(type: "int", nullable: true),
                    progress_seconds = table.Column<int>(type: "int", nullable: false),
                    duration_seconds = table.Column<int>(type: "int", nullable: true),
                    is_completed = table.Column<bool>(type: "bit", nullable: false),
                    watched_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_watch_history_items", x => x.id);
                    table.ForeignKey(
                        name: "f_k_watch_history_items_episodes_episode_id",
                        column: x => x.episode_id,
                        principalTable: "episodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "f_k_watch_history_items_titles_title_id",
                        column: x => x.title_id,
                        principalTable: "titles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_watch_history_items_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "watchlist_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    title_id = table.Column<int>(type: "int", nullable: false),
                    added_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_watchlist_items", x => x.id);
                    table.ForeignKey(
                        name: "f_k_watchlist_items_titles_title_id",
                        column: x => x.title_id,
                        principalTable: "titles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_watchlist_items_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "display_name", "password_hash" },
                values: new object[] { null, "AQAAAAIAAYagAAAAEImfWmhqjIyll9jVnnAxNEIsClpQ9EJ5hdD1GPrK233H7oXjHSE/r5I5h/FcnDDz/Q==" });

            migrationBuilder.CreateIndex(
                name: "IX_analytics_daily_stats_date",
                table: "analytics_daily_stats",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "IX_analytics_daily_stats_date_metric_name",
                table: "analytics_daily_stats",
                columns: new[] { "date", "metric_name" });

            migrationBuilder.CreateIndex(
                name: "i_x_notification_settings_user_id",
                table: "notification_settings",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_notifications_user_id_is_read_created_at",
                table: "notifications",
                columns: new[] { "user_id", "is_read", "created_at" });

            migrationBuilder.CreateIndex(
                name: "i_x_push_subscriptions_user_id",
                table: "push_subscriptions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_ratings_title_id",
                table: "ratings",
                column: "title_id");

            migrationBuilder.CreateIndex(
                name: "i_x_ratings_user_id_title_id",
                table: "ratings",
                columns: new[] { "user_id", "title_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_refresh_tokens_token",
                table: "refresh_tokens",
                column: "token");

            migrationBuilder.CreateIndex(
                name: "i_x_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_source_reports_created_at",
                table: "source_reports",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "i_x_source_reports_source_id",
                table: "source_reports",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "i_x_source_reports_status",
                table: "source_reports",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "i_x_source_reports_title_id",
                table: "source_reports",
                column: "title_id");

            migrationBuilder.CreateIndex(
                name: "i_x_watch_history_items_episode_id",
                table: "watch_history_items",
                column: "episode_id");

            migrationBuilder.CreateIndex(
                name: "i_x_watch_history_items_title_id",
                table: "watch_history_items",
                column: "title_id");

            migrationBuilder.CreateIndex(
                name: "i_x_watch_history_items_user_id_title_id_episode_id",
                table: "watch_history_items",
                columns: new[] { "user_id", "title_id", "episode_id" });

            migrationBuilder.CreateIndex(
                name: "i_x_watchlist_items_title_id",
                table: "watchlist_items",
                column: "title_id");

            migrationBuilder.CreateIndex(
                name: "i_x_watchlist_items_user_id_title_id",
                table: "watchlist_items",
                columns: new[] { "user_id", "title_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analytics_daily_stats");

            migrationBuilder.DropTable(
                name: "notification_settings");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "push_subscriptions");

            migrationBuilder.DropTable(
                name: "ratings");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "source_reports");

            migrationBuilder.DropTable(
                name: "watch_history_items");

            migrationBuilder.DropTable(
                name: "watchlist_items");

            migrationBuilder.DropColumn(
                name: "display_name",
                table: "users");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "password_hash",
                value: "AQAAAAIAAYagAAAAEEJ7J969P+HcTqhCi+a6XFHWxBRT2GFmP4qeD0teG8cs4L9zBX2KEyaFpJ/Tzjmamg==");
        }
    }
}
