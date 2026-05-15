using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobApplicationAssistant.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAIFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BaseResumeText",
                table: "UserProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawJobDescription",
                table: "JobApplications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TailoredCoverLetter",
                table: "JobApplications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TailoredResumePath",
                table: "JobApplications",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseResumeText",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "RawJobDescription",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "TailoredCoverLetter",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "TailoredResumePath",
                table: "JobApplications");
        }
    }
}
