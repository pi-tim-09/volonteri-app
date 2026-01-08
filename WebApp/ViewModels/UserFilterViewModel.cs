using WebApp.Models;

namespace WebApp.ViewModels
{
    public class UserFilterViewModel
    {
        // Pagination
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalUsers { get; set; }

        // Search
        public string? SearchTerm { get; set; }

        // Filters
        public UserRole? RoleFilter { get; set; }
        public bool? IsActiveFilter { get; set; }

        // Results
        public IEnumerable<User> Users { get; set; } = new List<User>();

        // Statistics
        public int TotalAdmins { get; set; }
        public int TotalOrganizations { get; set; }
        public int TotalVolunteers { get; set; }

        // Helper properties
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
