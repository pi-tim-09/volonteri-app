using WebApp.Interfaces;
using WebApp.Interfaces.Services;
using WebApp.Models;
using WebApp.Patterns.Behavioral;
using WebApp.Patterns.Creational;
using WebApp.Patterns.Structural;
using WebApp.ViewModels;

namespace WebApp.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserFactoryProvider _userFactoryProvider;
        private readonly INotificationService _notificationService;
        private readonly IApplicationStateContextFactory _stateContextFactory;
        private readonly ILogger<UserService> _logger;
        private readonly IVolunteerProfileService _volunteerProfileService;
        private readonly IVolunteerEventPublisher _volunteerEventPublisher;

        public UserService(
            IUnitOfWork unitOfWork,
            IUserFactoryProvider userFactoryProvider,
            INotificationService notificationService,
            IApplicationStateContextFactory stateContextFactory,
            ILogger<UserService> logger,
            IVolunteerProfileService volunteerProfileService,
            IVolunteerEventPublisher volunteerEventPublisher)
        {
            _unitOfWork = unitOfWork;
            _userFactoryProvider = userFactoryProvider;
            _notificationService = notificationService;
            _stateContextFactory = stateContextFactory;
            _logger = logger;
            _volunteerProfileService = volunteerProfileService;
            _volunteerEventPublisher = volunteerEventPublisher;
        }

       //Factory
        public async Task<User> CreateUserAsync(UserVM userVm)
        {
            if (await EmailExistsAsync(userVm.Email))
                throw new InvalidOperationException("Email already exists.");

            User user = _userFactoryProvider.CreateUser(
                userVm.Role,
                userVm.Email,
                userVm.FirstName,
                userVm.LastName,
                userVm.PhoneNumber);

            user.IsActive = userVm.IsActive;

            if (user is Volunteer v)
            {
                await _unitOfWork.Volunteers.AddAsync(v);
                await _unitOfWork.SaveChangesAsync();
                
                // Observer Pattern: Notify observers about new volunteer registration
                await _volunteerEventPublisher.NotifyVolunteerRegisteredAsync(v);
            }
            else if (user is Organization o)
                await _unitOfWork.Organizations.AddAsync(o);
            else if (user is Admin a)
                await _unitOfWork.Admins.AddAsync(a);

            await _unitOfWork.SaveChangesAsync();
            return user;
        }

     
        public async Task<bool> UpdateUserAsync(int id, UserVM userVm)
        {
            var user = await GetUserByIdAsync(id);
            if (user == null) return false;

            user.Email = userVm.Email;
            user.FirstName = userVm.FirstName;
            user.LastName = userVm.LastName;
            user.PhoneNumber = userVm.PhoneNumber;
            user.IsActive = userVm.IsActive;

            UpdateUser(user);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await GetUserByIdAsync(id);
            if (user == null) return false;

            RemoveUser(user);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            var volunteer = await _unitOfWork.Volunteers.GetByIdAsync(id);
            if (volunteer != null)
                return volunteer;

            var organization = await _unitOfWork.Organizations.GetByIdAsync(id);
            if (organization != null)
                return organization;

            var admin = await _unitOfWork.Admins.GetByIdAsync(id);
            return admin;
        }


        public async Task<UserFilterViewModel> GetFilteredUsersAsync(
            string? searchTerm,
            UserRole? roleFilter,
            bool? isActiveFilter,
            int pageNumber,
            int pageSize)
        {
            var users = (await _unitOfWork.Volunteers.GetAllAsync()).Cast<User>()
                .Concat(await _unitOfWork.Organizations.GetAllAsync())
                .Concat(await _unitOfWork.Admins.GetAllAsync())
                .ToList();

            return new UserFilterViewModel
            {
                Users = users,
                TotalUsers = users.Count
            };
        }

        public async Task<bool> ActivateUserAsync(int id)
        {
            var user = await GetUserByIdAsync(id);
            if (user == null) return false;

            user.IsActive = true;
            UpdateUser(user);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateUserAsync(int id)
        {
            var user = await GetUserByIdAsync(id);
            if (user == null) return false;

            user.IsActive = false;
            UpdateUser(user);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _unitOfWork.Volunteers.AnyAsync(u => u.Email == email)
                || await _unitOfWork.Organizations.AnyAsync(u => u.Email == email)
                || await _unitOfWork.Admins.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> CanDeleteUserAsync(int id)
        {
            return await GetUserByIdAsync(id) != null;
        }


        public async Task<Application> SubmitApplicationAsync(int volunteerId, int projectId)
        {
            var application = new Application
            {
                VolunteerId = volunteerId,
                ProjectId = projectId,
                Status = ApplicationStatus.Pending
            };

            await _unitOfWork.Applications.AddAsync(application);
            await _unitOfWork.SaveChangesAsync();
            //Decorator
            await _notificationService.NotifyApplicationSubmittedAsync(application);

            return application;
        }


        public async Task<bool> ApproveApplicationAsync(Application application, string? notes)
        {//State
            var context = _stateContextFactory.CreateContext(application);

            if (!context.CanApprove())
                return false;

            var result = await context.ApproveAsync(notes);
            await _unitOfWork.SaveChangesAsync();

            
            await _notificationService.NotifyApplicationApprovedAsync(application);

            return result;
        }

        public async Task<bool> RejectApplicationAsync(Application application, string? notes)
        {
            var context = _stateContextFactory.CreateContext(application);

            if (!context.CanReject())
                return false;

            var result = await context.RejectAsync(notes);
            await _unitOfWork.SaveChangesAsync();

            await _notificationService.NotifyApplicationRejectedAsync(application);
            return result;
        }

        public async Task<bool> WithdrawApplicationAsync(Application application)
        {
            var context = _stateContextFactory.CreateContext(application);

            if (!context.CanWithdraw())
                return false;

            var result = await context.WithdrawAsync();
            await _unitOfWork.SaveChangesAsync();

            await _notificationService.NotifyApplicationWithdrawnAsync(application);
            return result;
        }

      
        private void UpdateUser(User user)
        {
            if (user is Volunteer v) _unitOfWork.Volunteers.Update(v);
            else if (user is Organization o) _unitOfWork.Organizations.Update(o);
            else if (user is Admin a) _unitOfWork.Admins.Update(a);
        }

        private void RemoveUser(User user)
        {
            if (user is Volunteer v) _unitOfWork.Volunteers.Remove(v);
            else if (user is Organization o) _unitOfWork.Organizations.Remove(o);
            else if (user is Admin a) _unitOfWork.Admins.Remove(a);
        }

        
        public async Task<string> GetEnrichedVolunteerSummaryAsync(int volunteerId)
        {
            var volunteer = await _unitOfWork.Volunteers.GetByIdAsync(volunteerId);
            if (volunteer == null)
                throw new InvalidOperationException($"Volunteer {volunteerId} not found");

            // Uses Decorator pattern 
            return await _volunteerProfileService.FormatVolunteerSummaryAsync(volunteer);
        }

        
        public async Task<bool> UpdateVolunteerSkillsAsync(int volunteerId, List<string> newSkills)
        {
            var volunteer = await _unitOfWork.Volunteers.GetByIdAsync(volunteerId);
            if (volunteer == null)
                return false;

            volunteer.Skills = newSkills;
            _unitOfWork.Volunteers.Update(volunteer);
            await _unitOfWork.SaveChangesAsync();

            // Observer Pattern: Notify all observers about skill update
            await _volunteerEventPublisher.NotifyVolunteerSkillsUpdatedAsync(volunteer, newSkills);

            return true;
        }

       
        public async Task<bool> RecordVolunteerProjectCompletionAsync(int volunteerId, int projectId, int hoursLogged)
        {
            var volunteer = await _unitOfWork.Volunteers.GetByIdAsync(volunteerId);
            if (volunteer == null)
                return false;

            volunteer.VolunteerHours += hoursLogged;
            _unitOfWork.Volunteers.Update(volunteer);
            await _unitOfWork.SaveChangesAsync();

            // Observer Pattern: Notify all observers about project completion
            await _volunteerEventPublisher.NotifyVolunteerProjectCompletedAsync(volunteer, projectId, hoursLogged);

            return true;
        }
    }
}
