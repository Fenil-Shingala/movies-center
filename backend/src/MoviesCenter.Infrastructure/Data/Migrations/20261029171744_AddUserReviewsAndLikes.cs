using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoviesCenter.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserReviewsAndLikes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    title_id = table.Column<int>(type: "int", nullable: false),
                    content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    rating_score = table.Column<int>(type: "int", nullable: true),
                    contains_spoilers = table.Column<bool>(type: "bit", nullable: false),
                    likes_count = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_reviews", x => x.id);
                    table.ForeignKey(
                        name: "f_k_reviews__titles_title_id",
                        column: x => x.title_id,
                        principalTable: "titles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_reviews_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review_likes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    review_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_review_likes", x => x.id);
                    table.ForeignKey(
                        name: "f_k_review_likes_reviews_review_id",
                        column: x => x.review_id,
                        principalTable: "reviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_review_likes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "password_hash",
                value: "AQAAAAIAAYagAAAAELJhU5PwpVyo9U/WpazmWgjCqEHcaVqW6xI+E3juvRVJd6tDVXI2RQJNcYuQx9jY3g==");

            migrationBuilder.CreateIndex(
                name: "i_x_review_likes_review_id_user_id",
                table: "review_likes",
                columns: new[] { "review_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_review_likes_user_id",
                table: "review_likes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_reviews_title_id",
                table: "reviews",
                column: "title_id");

            migrationBuilder.CreateIndex(
                name: "i_x_reviews_title_id_created_at",
                table: "reviews",
                columns: new[] { "title_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "i_x_reviews_user_id",
                table: "reviews",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "review_likes");

            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "password_hash",
                value: "AQAAAAIAAYagAAAAEImfWmhqjIyll9jVnnAxNEIsClpQ9EJ5hdD1GPrK233H7oXjHSE/r5I5h/FcnDDz/Q==");
        }
    }
}
