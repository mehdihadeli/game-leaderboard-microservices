using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderBoard.DbMigrator.Migrations.LeaderBoard
{
    /// <inheritdoc />
    public partial class InitialLeaderboardMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "player_score_read_model",
                columns: table => new
                {
                    player_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    score = table.Column<double>(type: "double precision", nullable: false),
                    leader_board_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    first_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_processed_position = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_score_read_model", x => x.player_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_score_read_model");
        }
    }
}
