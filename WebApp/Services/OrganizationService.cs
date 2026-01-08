using WebApp.Interfaces;
using WebApp.Interfaces.Services;
using WebApp.Models;

namespace WebApp.Services
{
    /// <summary>
    /// Service implementation for Organization-related business logic
    /// Follows Single Responsibility Principle - handles only organization-related business operations
    /// Delegates data access to repositories
    /// </summary>
    public class OrganizationService : IOrganizationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrganizationService> _logger;

        public OrganizationService(IUnitOfWork unitOfWork, ILogger<OrganizationService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // CRUD Operations with Business Logic
        public async Task<Organization> CreateOrganizationAsync(Organization organization)
        {
            try
            {
                if (organization == null)
                    throw new ArgumentNullException(nameof(organization));

                // Business rules
                organization.CreatedAt = DateTime.UtcNow;
                organization.IsActive = true;
                organization.IsVerified = false; // Business rule: new organizations start unverified

                // Delegate to repository
                var created = await _unitOfWork.Organizations.AddAsync(organization);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Created new organization: {OrganizationName}", organization.OrganizationName);
                return created;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating organization: {OrganizationName}", organization?.OrganizationName);
                throw;
            }
        }

        public async Task<bool> UpdateOrganizationAsync(int id, Organization organization)
        {
            try
            {
                // Delegate to repository
                var existingOrg = await _unitOfWork.Organizations.GetByIdAsync(id);
                if (existingOrg == null)
                    return false;

                // Business logic: update allowed fields
                existingOrg.OrganizationName = organization.OrganizationName;
                existingOrg.Email = organization.Email;
                existingOrg.FirstName = organization.FirstName;
                existingOrg.LastName = organization.LastName;
                existingOrg.PhoneNumber = organization.PhoneNumber;
                existingOrg.City = organization.City;
                existingOrg.Address = organization.Address;
                existingOrg.Description = organization.Description;
                existingOrg.IsActive = organization.IsActive;
                // Note: IsVerified is NOT updated here - use VerifyOrganizationAsync instead

                _unitOfWork.Organizations.Update(existingOrg);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Updated organization: {OrganizationId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating organization: {OrganizationId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteOrganizationAsync(int id)
        {
            try
            {
                // Business validation
                if (!await CanDeleteOrganizationAsync(id))
                {
                    throw new InvalidOperationException("Organization cannot be deleted - it has active projects.");
                }

                // Delegate to repository
                var organization = await _unitOfWork.Organizations.GetByIdAsync(id);
                if (organization == null)
                    return false;

                _unitOfWork.Organizations.Remove(organization);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Deleted organization: {OrganizationId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting organization: {OrganizationId}", id);
                throw;
            }
        }

        public async Task<Organization?> GetOrganizationByIdAsync(int id)
        {
            try
            {
                // Delegate to repository
                return await _unitOfWork.Organizations.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving organization by ID: {OrganizationId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Organization>> GetAllOrganizationsAsync()
        {
            try
            {
                // Delegate to repository
                return await _unitOfWork.Organizations.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all organizations");
                throw;
            }
        }

        // Business Logic Operations
        public async Task<bool> VerifyOrganizationAsync(int id)
        {
            try
            {
                // Delegate to repository
                var organization = await _unitOfWork.Organizations.GetByIdAsync(id);
                if (organization == null)
                    return false;

                // Business rule: check if already verified
                if (organization.IsVerified)
                {
                    _logger.LogWarning("Organization {OrganizationId} is already verified", id);
                    return false;
                }

                // Use repository's verify method
                var verified = await _unitOfWork.Organizations.VerifyOrganizationAsync(id);
                if (verified)
                {
                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("Verified organization: {OrganizationId}", id);
                }

                return verified;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying organization: {OrganizationId}", id);
                throw;
            }
        }

        public async Task<bool> UnverifyOrganizationAsync(int id)
        {
            try
            {
                // Delegate to repository
                var organization = await _unitOfWork.Organizations.GetByIdAsync(id);
                if (organization == null)
                    return false;

                // Business rule
                organization.IsVerified = false;
                organization.VerifiedAt = null;

                _unitOfWork.Organizations.Update(organization);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Unverified organization: {OrganizationId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unverifying organization: {OrganizationId}", id);
                throw;
            }
        }

        public async Task<bool> CanCreateProjectAsync(int organizationId)
        {
            try
            {
                // Delegate to repository
                var organization = await _unitOfWork.Organizations.GetByIdAsync(organizationId);
                if (organization == null)
                    return false;

                // Business rule: Organization must be verified and active to create projects
                return organization.IsVerified && organization.IsActive;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if organization can create project: {OrganizationId}", organizationId);
                throw;
            }
        }

        // Validation & Business Rules
        public async Task<bool> OrganizationExistsAsync(int id)
        {
            try
            {
                // Delegate to repository
                return await _unitOfWork.Organizations.AnyAsync(o => o.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if organization exists: {OrganizationId}", id);
                throw;
            }
        }

        public async Task<bool> CanDeleteOrganizationAsync(int id)
        {
            try
            {
                // Business rule: Can't delete organization with active projects
                var projectCount = await _unitOfWork.Projects.CountAsync(p => p.OrganizationId == id);
                return projectCount == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if organization can be deleted: {OrganizationId}", id);
                throw;
            }
        }
    }
}
