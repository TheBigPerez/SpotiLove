using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JsonDemo.Migrations
{
    /// <inheritdoc />
    public partial class ConvertUnitToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql(@"
ALTER TABLE ""Users""
    ALTER COLUMN ""Id"" TYPE uuid USING ""Id""::uuid;

ALTER TABLE ""MusicProfiles""
    ALTER COLUMN ""Id"" TYPE uuid USING ""Id""::uuid,
    ALTER COLUMN ""UserId"" TYPE uuid USING ""UserId""::uuid;

ALTER TABLE ""UserImages""
    ALTER COLUMN ""Id"" TYPE uuid USING ""Id""::uuid,
    ALTER COLUMN ""UserId"" TYPE uuid USING ""UserId""::uuid;

ALTER TABLE ""Likes""
    ALTER COLUMN ""FromUserId"" TYPE uuid USING ""FromUserId""::uuid,
    ALTER COLUMN ""ToUserId"" TYPE uuid USING ""ToUserId""::uuid;

ALTER TABLE ""UserSuggestionQueues""
    ALTER COLUMN ""UserId"" TYPE uuid USING ""UserId""::uuid,
    ALTER COLUMN ""SuggestedUserId"" TYPE uuid USING ""SuggestedUserId""::uuid;

ALTER TABLE ""Messages""
    ALTER COLUMN ""Id"" TYPE uuid USING ""Id""::uuid,
    ALTER COLUMN ""FromUserId"" TYPE uuid USING ""FromUserId""::uuid,
    ALTER COLUMN ""ToUserId"" TYPE uuid USING ""ToUserId""::uuid;
");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Likes");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "MusicProfiles");

            migrationBuilder.DropTable(
                name: "UserImages");

            migrationBuilder.DropTable(
                name: "UserSuggestionQueues");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
